using CreditsSim.Application.DTOs;
using CreditsSim.Application.Services;
using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
using CreditsSim.Domain.Query;
using CreditsSim.WebAPI.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace CreditsSim.Tests.Plugins;

/// <summary>
/// Tests del SimulationPlugin — el tool que expone el asistente via Semantic Kernel.
/// Verifica que el string recibido del LLM se parsea al enum InstallmentType y se
/// persiste en la entidad con el valor canonico (FIXED / GERMAN), no el string crudo.
/// </summary>
public class SimulationPluginTests
{
    private static (SimulationPlugin plugin, CapturingRepository repo) BuildPlugin()
    {
        var repo = new CapturingRepository();
        var services = new ServiceCollection();
        services.AddSingleton<ISimulationRepository>(repo);
        services.AddSingleton<IAmortizationCalculator, FrenchAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculator, GermanAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculatorFactory, AmortizationCalculatorFactory>();
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return (new SimulationPlugin(scopeFactory), repo);
    }

    [Fact]
    public async Task CreateSimulation_InstallmentTypeGERMAN_PersisteEnumCorrecto()
    {
        var (plugin, repo) = BuildPlugin();

        var output = await plugin.CreateSimulationAsync(60_000m, 12, 18m, installmentType: "GERMAN");

        var persisted = Assert.Single(repo.Added);
        // El valor persistido es el ToString() del enum, NO el string recibido del LLM.
        Assert.Equal(InstallmentType.GERMAN.ToString(), persisted.InstallmentType);
        // La salida al LLM usa las etiquetas de cuota inicial/final para GERMAN.
        Assert.Contains("Cuota inicial", output);
        Assert.Contains("Cuota final", output);
        Assert.DoesNotContain("Cuota mensual", output);
    }

    [Fact]
    public async Task CreateSimulation_InstallmentTypeFIXED_PersisteEnumCorrecto()
    {
        var (plugin, repo) = BuildPlugin();

        var output = await plugin.CreateSimulationAsync(60_000m, 12, 18m, installmentType: "FIXED");

        var persisted = Assert.Single(repo.Added);
        Assert.Equal(InstallmentType.FIXED.ToString(), persisted.InstallmentType);
        Assert.Contains("Cuota mensual", output);
        Assert.DoesNotContain("Cuota inicial", output);
    }

    [Fact]
    public async Task CreateSimulation_InstallmentTypeLowercase_ParseaCaseInsensitive()
    {
        // El plugin es consumido por el LLM, que puede emitir "german" o "GERMAN".
        // Enum.TryParse(ignoreCase: true) lo acepta — pero la persistencia siempre
        // canonicaliza a MAYUSCULAS via enum.ToString().
        var (plugin, repo) = BuildPlugin();

        await plugin.CreateSimulationAsync(60_000m, 12, 18m, installmentType: "german");

        var persisted = Assert.Single(repo.Added);
        Assert.Equal("GERMAN", persisted.InstallmentType);
    }

    [Fact]
    public async Task CreateSimulation_InstallmentTypeInvalido_DevuelveMensajeErrorSinPersistir()
    {
        var (plugin, repo) = BuildPlugin();

        var output = await plugin.CreateSimulationAsync(60_000m, 12, 18m, installmentType: "AMERICAN");

        Assert.Contains("FIXED", output);
        Assert.Contains("GERMAN", output);
        Assert.Empty(repo.Added); // no debe persistir nada
    }

    [Fact]
    public async Task CreateSimulation_InstallmentTypeDefault_AsumeFIXED()
    {
        // El parametro tiene default `"FIXED"` — si el LLM lo omite, debe asumirse frances.
        var (plugin, repo) = BuildPlugin();

        await plugin.CreateSimulationAsync(60_000m, 12, 18m);

        var persisted = Assert.Single(repo.Added);
        Assert.Equal("FIXED", persisted.InstallmentType);
    }

    // ── Test double ─────────────────────────────────────────────────
    private sealed class CapturingRepository : ISimulationRepository
    {
        public List<SimulationHistory> Added { get; } = new();

        public Task<SimulationHistory> AddAsync(SimulationHistory simulation, CancellationToken ct = default)
        {
            Added.Add(simulation);
            return Task.FromResult(simulation);
        }

        public Task<SimulationHistory?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<SimulationHistory?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<(List<SimulationHistory> Items, bool HasNextPage)> GetCursorPagedAsync(
            int pageSize,
            bool ascending = false,
            string sortBy = "createdAt",
            DateTime? cursorCreatedAt = null,
            Guid? cursorId = null,
            SimulationListFilter? filter = null,
            CancellationToken ct = default) =>
            Task.FromResult((new List<SimulationHistory>(), false));

        public Task<int> CountAsync(SimulationListFilter? filter = null, CancellationToken ct = default) =>
            Task.FromResult(0);
    }
}

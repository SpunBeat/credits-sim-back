using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CreditsSim.Application.DTOs;
using CreditsSim.Application.Services;
using CreditsSim.Domain.Entities;
using CreditsSim.Domain.Interfaces;
using Microsoft.SemanticKernel;

namespace CreditsSim.WebAPI.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes credit simulation tools to the LLM.
/// Each method returns human-readable text (not JSON) so the LLM can
/// relay it naturally to the user.
/// </summary>
public class SimulationPlugin
{
    private readonly ISimulationRepository _repo;

    public SimulationPlugin(ISimulationRepository repo)
    {
        _repo = repo;
    }

    // ── Create ────────────────────────────────────────────────────

    [KernelFunction("create_simulation")]
    [Description("Crea una nueva simulación de crédito personal con cuotas fijas (sistema francés). " +
                 "Recibe monto, plazo en meses y tasa de interés anual en porcentaje. " +
                 "Devuelve el resumen con cuota mensual, total a pagar y total de intereses.")]
    public async Task<string> CreateSimulationAsync(
        [Description("Monto del crédito en USD (entre 1 y 10,000,000)")] decimal amount,
        [Description("Plazo en meses (entre 1 y 360)")] int termMonths,
        [Description("Tasa de interés anual en porcentaje, ej: 18.5 para 18.5%")] decimal annualRate)
    {
        try
        {
            if (amount <= 0 || amount > 10_000_000)
                return $"No pude crear la simulación: el monto debe estar entre $1 y $10,000,000 (recibí ${amount:N2}).";
            if (termMonths < 1 || termMonths > 360)
                return $"No pude crear la simulación: el plazo debe estar entre 1 y 360 meses (recibí {termMonths}).";
            if (annualRate < 0 || annualRate > 100)
                return $"No pude crear la simulación: la tasa anual debe estar entre 0% y 100% (recibí {annualRate}%).";

            var schedule = AmortizationService.CalculateSchedule(amount, termMonths, annualRate);

            var entity = new SimulationHistory
            {
                Id = Guid.NewGuid(),
                Amount = amount,
                TermMonths = termMonths,
                AnnualRate = annualRate,
                InstallmentType = "FIXED",
                ScheduleJson = JsonSerializer.Serialize(schedule),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);

            var totalPaid = schedule.Sum(r => r.Payment);
            var totalInterest = schedule.Sum(r => r.Interest);
            var monthlyPayment = schedule.FirstOrDefault()?.Payment ?? 0;

            return $"""
                Simulación creada exitosamente.
                ID: {entity.Id}
                Monto: ${amount:N2}
                Plazo: {termMonths} meses
                Tasa anual: {annualRate}%
                Cuota mensual: ${monthlyPayment:N2}
                Total a pagar: ${totalPaid:N2}
                Total intereses: ${totalInterest:N2}
                """;
        }
        catch (Exception e)
        {
            return $"No pude crear la simulación: {e.Message}";
        }
    }

    // ── Get by ID ─────────────────────────────────────────────────

    [KernelFunction("get_simulation")]
    [Description("Obtiene el detalle completo de una simulación existente por su ID (GUID). " +
                 "Incluye el cronograma de pagos mes a mes.")]
    public async Task<string> GetSimulationAsync(
        [Description("ID (GUID) de la simulación")] string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
                return $"El ID '{id}' no es un GUID válido.";

            var entity = await _repo.GetByIdAsync(guid);
            if (entity is null)
                return $"No encontré una simulación con ID {id}.";

            var schedule = JsonSerializer.Deserialize<List<ScheduleRow>>(entity.ScheduleJson) ?? [];
            var totalPaid = schedule.Sum(r => r.Payment);
            var totalInterest = schedule.Sum(r => r.Interest);
            var monthlyPayment = schedule.FirstOrDefault()?.Payment ?? 0;

            var sb = new StringBuilder();
            sb.AppendLine($"Simulación {entity.Id}");
            sb.AppendLine($"Monto: ${entity.Amount:N2}");
            sb.AppendLine($"Plazo: {entity.TermMonths} meses");
            sb.AppendLine($"Tasa anual: {entity.AnnualRate}%");
            sb.AppendLine($"Tipo de cuota: {entity.InstallmentType}");
            sb.AppendLine($"Cuota mensual: ${monthlyPayment:N2}");
            sb.AppendLine($"Total a pagar: ${totalPaid:N2}");
            sb.AppendLine($"Total intereses: ${totalInterest:N2}");
            sb.AppendLine($"Creado: {entity.CreatedAt:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();
            sb.AppendLine("Cronograma de pagos:");
            sb.AppendLine("Mes | Cuota | Capital | Interés | Seguro | Saldo");

            foreach (var row in schedule)
            {
                sb.AppendLine($"{row.Month,3} | ${row.Payment:N2} | ${row.Principal:N2} | ${row.Interest:N2} | ${row.Insurance:N2} | ${row.Balance:N2}");
            }

            return sb.ToString();
        }
        catch (Exception e)
        {
            return $"Error al obtener la simulación: {e.Message}";
        }
    }

    // ── List recent ───────────────────────────────────────────────

    [KernelFunction("list_simulations")]
    [Description("Lista las simulaciones más recientes del historial. " +
                 "Devuelve un resumen de cada simulación ordenado por fecha de creación descendente.")]
    public async Task<string> ListSimulationsAsync(
        [Description("Cantidad de simulaciones a listar (máximo 20, por defecto 5)")] int count = 5)
    {
        try
        {
            count = Math.Clamp(count, 1, 20);

            var (items, _) = await _repo.GetCursorPagedAsync(
                pageSize: count,
                ascending: false,
                sortBy: "createdAt");

            if (items.Count == 0)
                return "No hay simulaciones registradas en el historial.";

            var sb = new StringBuilder();
            sb.AppendLine($"Últimas {items.Count} simulaciones:");
            sb.AppendLine();

            foreach (var (item, idx) in items.Select((item, idx) => (item, idx)))
            {
                sb.AppendLine($"{idx + 1}. ID: {item.Id}");
                sb.AppendLine($"   Monto: ${item.Amount:N2} | Plazo: {item.TermMonths}m | Tasa: {item.AnnualRate}%");
                sb.AppendLine($"   Creado: {item.CreatedAt:yyyy-MM-dd HH:mm} UTC");
            }

            return sb.ToString();
        }
        catch (Exception e)
        {
            return $"Error al listar simulaciones: {e.Message}";
        }
    }

    // ── Compare ───────────────────────────────────────────────────

    [KernelFunction("compare_simulations")]
    [Description("Compara dos o más simulaciones mostrando sus KPIs lado a lado. " +
                 "Recibe los IDs separados por coma. Resalta cuál tiene la cuota más baja y el menor costo total.")]
    public async Task<string> CompareSimulationsAsync(
        [Description("IDs de las simulaciones a comparar, separados por coma")] string ids)
    {
        try
        {
            var guidStrings = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (guidStrings.Length < 2)
                return "Necesito al menos 2 IDs para comparar. Separalos con coma.";
            if (guidStrings.Length > 6)
                return "Máximo 6 simulaciones para comparar.";

            var scenarios = new List<(SimulationHistory Entity, List<ScheduleRow> Schedule)>();

            foreach (var idStr in guidStrings)
            {
                if (!Guid.TryParse(idStr, out var guid))
                    return $"El ID '{idStr}' no es un GUID válido.";

                var entity = await _repo.GetByIdAsync(guid);
                if (entity is null)
                    return $"No encontré una simulación con ID {idStr}.";

                var schedule = JsonSerializer.Deserialize<List<ScheduleRow>>(entity.ScheduleJson) ?? [];
                scenarios.Add((entity, schedule));
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Comparación de {scenarios.Count} simulaciones:");
            sb.AppendLine();

            // Header
            sb.Append("Indicador".PadRight(20));
            for (var i = 0; i < scenarios.Count; i++)
                sb.Append($"| Escenario {i + 1}".PadRight(18));
            sb.AppendLine();
            sb.AppendLine(new string('-', 20 + scenarios.Count * 18));

            // Rows
            void AddRow(string label, Func<(SimulationHistory, List<ScheduleRow>), string> getValue)
            {
                sb.Append(label.PadRight(20));
                foreach (var s in scenarios)
                    sb.Append($"| {getValue(s)}".PadRight(18));
                sb.AppendLine();
            }

            AddRow("Monto", s => $"${s.Item1.Amount:N2}");
            AddRow("Plazo", s => $"{s.Item1.TermMonths}m");
            AddRow("Tasa anual", s => $"{s.Item1.AnnualRate}%");
            AddRow("Cuota mensual", s => $"${s.Item2.FirstOrDefault()?.Payment ?? 0:N2}");
            AddRow("Total a pagar", s => $"${s.Item2.Sum(r => r.Payment):N2}");
            AddRow("Total intereses", s => $"${s.Item2.Sum(r => r.Interest):N2}");
            AddRow("Total seguro", s => $"${s.Item2.Sum(r => r.Insurance):N2}");

            sb.AppendLine();

            // Best
            var payments = scenarios.Select((s, i) => (Monthly: s.Schedule.FirstOrDefault()?.Payment ?? 0, Index: i)).ToList();
            var cheapest = payments.MinBy(p => p.Monthly);
            sb.AppendLine($"✓ Cuota más baja: Escenario {cheapest.Index + 1} (${cheapest.Monthly:N2}/mes)");

            var totals = scenarios.Select((s, i) => (Total: s.Schedule.Sum(r => r.Payment), Index: i)).ToList();
            var lowestTotal = totals.MinBy(t => t.Total);
            sb.AppendLine($"✓ Menor costo total: Escenario {lowestTotal.Index + 1} (${lowestTotal.Total:N2})");

            return sb.ToString();
        }
        catch (Exception e)
        {
            return $"Error al comparar simulaciones: {e.Message}";
        }
    }

    // ── Delete ────────────────────────────────────────────────────

    [KernelFunction("delete_simulation")]
    [Description("Elimina una simulación por su ID. Solo ejecutar cuando el usuario lo confirme explícitamente. " +
                 "Esta acción es irreversible.")]
    public async Task<string> DeleteSimulationAsync(
        [Description("ID (GUID) de la simulación a eliminar")] string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var guid))
                return $"El ID '{id}' no es un GUID válido.";

            var deleted = await _repo.DeleteAsync(guid);

            return deleted
                ? $"Simulación {id} eliminada correctamente."
                : $"No encontré una simulación con ID {id}. Puede que ya haya sido eliminada.";
        }
        catch (Exception e)
        {
            return $"Error al eliminar la simulación: {e.Message}";
        }
    }
}

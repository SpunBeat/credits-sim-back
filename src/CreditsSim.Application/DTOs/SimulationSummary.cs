namespace CreditsSim.Application.DTOs;

/// <summary>
/// Resumen de una simulación (sin cronograma). Usado en listados paginados.
/// </summary>
public record SimulationSummary
{
    /// <summary>Identificador único de la simulación.</summary>
    public Guid Id { get; init; }

    /// <summary>Monto del crédito simulado.</summary>
    public decimal Amount { get; init; }

    /// <summary>Plazo en meses.</summary>
    public int TermMonths { get; init; }

    /// <summary>Tasa de interés anual (%).</summary>
    public decimal AnnualRate { get; init; }

    /// <summary>Tipo de cuota aplicado: FIXED (Sistema Francés) o GERMAN (Sistema Alemán).</summary>
    public InstallmentType InstallmentType { get; init; } = InstallmentType.FIXED;

    /// <summary>Fecha y hora de creación (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}

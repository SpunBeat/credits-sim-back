namespace CreditsSim.Application.DTOs;

/// <summary>
/// Resultado completo de una simulación de crédito, incluyendo el cronograma de pagos.
/// </summary>
public record SimulationResponse
{
    /// <summary>Identificador único de la simulación.</summary>
    public Guid Id { get; init; }

    /// <summary>Monto del crédito simulado.</summary>
    public decimal Amount { get; init; }

    /// <summary>Plazo en meses.</summary>
    public int TermMonths { get; init; }

    /// <summary>Tasa de interés anual (%).</summary>
    public decimal AnnualRate { get; init; }

    /// <summary>Tipo de cuota aplicado (ej: FIXED, GERMAN).</summary>
    public string InstallmentType { get; init; } = string.Empty;

    /// <summary>Cronograma de pagos mes a mes.</summary>
    public List<ScheduleRow> Schedule { get; init; } = [];

    /// <summary>Fecha y hora de creación de la simulación (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Detalle de una cuota individual dentro del cronograma de pagos.
/// </summary>
public record ScheduleRow
{
    /// <summary>Número de mes (1-based).</summary>
    public int Month { get; init; }

    /// <summary>Monto total de la cuota (capital + interés + seguro).</summary>
    public decimal Payment { get; init; }

    /// <summary>Porción de capital amortizado en esta cuota.</summary>
    public decimal Principal { get; init; }

    /// <summary>Porción de interés en esta cuota.</summary>
    public decimal Interest { get; init; }

    /// <summary>Prima de seguro de desgravamen en esta cuota.</summary>
    public decimal Insurance { get; init; }

    /// <summary>Saldo pendiente del crédito después de esta cuota.</summary>
    public decimal Balance { get; init; }
}

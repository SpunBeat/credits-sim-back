namespace CreditsSim.Application.DTOs;

/// <summary>
/// Datos de entrada para simular un crédito personal.
/// </summary>
public record SimulationRequest
{
    /// <summary>Monto del crédito a simular. Debe ser mayor a 0 y menor o igual a 10,000,000.</summary>
    public decimal Amount { get; init; }

    /// <summary>Plazo en meses. Rango permitido: 1 a 360.</summary>
    public int TermMonths { get; init; }

    /// <summary>Tasa de interés anual expresada como porcentaje (ej: 18.5 para 18.5%). Rango: 0 a 100.</summary>
    public decimal AnnualRate { get; init; }

    /// <summary>Tipo de cuota. Valores soportados: FIXED (Sistema Francés), GERMAN (Sistema Alemán).</summary>
    public string InstallmentType { get; init; } = "FIXED";
}

namespace CreditsSim.Application.DTOs;

/// <summary>
/// Sistema de amortizacion aplicado a la simulacion.
/// </summary>
public enum InstallmentType
{
    /// <summary>Sistema frances: cuotas constantes, capital creciente e interes decreciente.</summary>
    FIXED,

    /// <summary>Sistema aleman: capital constante, cuotas decrecientes.</summary>
    GERMAN
}

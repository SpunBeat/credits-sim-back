using CreditsSim.Application.Features.Simulations.Queries;
using CreditsSim.Application.Validators;

namespace CreditsSim.Tests.Validators;

/// <summary>
/// A diferencia del comando POST, el query-string llega como `string?` y la
/// convencion HTTP espera case-insensitivity (ej: ?installmentType=german).
/// El validator usa Enum.TryParse(ignoreCase: true).
/// </summary>
public class ListSimulationsValidatorTests
{
    private readonly ListSimulationsValidator _validator = new();

    private static ListSimulationsQuery QueryWith(string? installmentType) =>
        new(InstallmentType: installmentType);

    [Theory]
    [InlineData("FIXED")]
    [InlineData("GERMAN")]
    [InlineData("fixed")]
    [InlineData("german")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void InstallmentType_ValorValidoOAusente_PasaValidacion(string? tipo)
    {
        var result = _validator.Validate(QueryWith(tipo));

        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(ListSimulationsQuery.InstallmentType));
    }

    [Theory]
    [InlineData("AMERICAN")]
    [InlineData("FRENCH")]
    [InlineData("xyz")]
    public void InstallmentType_ValorInvalido_FallaConMensajeQueListaValoresValidos(string tipo)
    {
        var result = _validator.Validate(QueryWith(tipo));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == nameof(ListSimulationsQuery.InstallmentType));
        Assert.Contains("FIXED", error.ErrorMessage);
        Assert.Contains("GERMAN", error.ErrorMessage);
    }
}

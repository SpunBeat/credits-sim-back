using CreditsSim.Application.DTOs;
using CreditsSim.Application.Features.Simulations.Commands;
using CreditsSim.Application.Validators;

namespace CreditsSim.Tests.Validators;

/// <summary>
/// El comando recibe el enum ya tipado (`InstallmentType`); el validator solo
/// puede ver valores enum. Las cadenas invalidas como "fixed", "AMERICAN", null
/// se rechazan antes, en el boundary de deserializacion JSON — ver
/// <see cref="Serialization.StrictEnumConverterTests"/>.
/// </summary>
public class CreateSimulationValidatorTests
{
    private readonly CreateSimulationValidator _validator = new();

    private static CreateSimulationCommand ValidCommand(InstallmentType type) =>
        new(Amount: 50_000m, TermMonths: 12, AnnualRate: 18m, InstallmentType: type);

    [Theory]
    [InlineData(InstallmentType.FIXED)]
    [InlineData(InstallmentType.GERMAN)]
    public void InstallmentType_ValorValido_PasaValidacion(InstallmentType tipo)
    {
        var result = _validator.Validate(ValidCommand(tipo));

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateSimulationCommand.InstallmentType));
    }

    [Fact]
    public void InstallmentType_FueraDeRango_FallaConMensajeQueListaValoresValidos()
    {
        // Un valor numerico fuera del enum llega si alguien deserializa un int invalido
        // (ej: { "installmentType": 99 }). IsInEnum() lo detecta y emite mensaje.
        var cmd = new CreateSimulationCommand(50_000m, 12, 18m, (InstallmentType)999);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == nameof(CreateSimulationCommand.InstallmentType));
        Assert.Contains("FIXED", error.ErrorMessage);
        Assert.Contains("GERMAN", error.ErrorMessage);
    }

    [Fact]
    public void Amount_Invalido_Falla()
    {
        var cmd = new CreateSimulationCommand(Amount: 0m, TermMonths: 12, AnnualRate: 18m, InstallmentType: InstallmentType.FIXED);

        var result = _validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateSimulationCommand.Amount));
    }
}

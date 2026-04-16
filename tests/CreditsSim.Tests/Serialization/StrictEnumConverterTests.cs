using System.Text.Json;
using CreditsSim.Application.DTOs;
using CreditsSim.WebAPI.Swagger;

namespace CreditsSim.Tests.Serialization;

/// <summary>
/// El converter estricto es el boundary que rechaza "german", null, "AMERICAN"
/// antes de que el DTO llegue al validator. Se testea aqui el comportamiento
/// concreto: throw JsonException con mensaje que lista los valores validos.
/// </summary>
public class StrictEnumConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new StrictEnumConverterFactory());
        return opts;
    }

    [Theory]
    [InlineData("FIXED", InstallmentType.FIXED)]
    [InlineData("GERMAN", InstallmentType.GERMAN)]
    public void Deserialize_ValorCanonico_Parsea(string json, InstallmentType expected)
    {
        var parsed = JsonSerializer.Deserialize<InstallmentType>($"\"{json}\"", Options);

        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData("fixed")]
    [InlineData("german")]
    [InlineData("German")]
    [InlineData("AMERICAN")]
    [InlineData("")]
    public void Deserialize_ValorInvalido_ThrowJsonExceptionConMensajeLegible(string value)
    {
        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<InstallmentType>($"\"{value}\"", Options));

        Assert.Contains("InstallmentType", ex.Message);
        Assert.Contains("FIXED", ex.Message);
        Assert.Contains("GERMAN", ex.Message);
    }

    [Fact]
    public void Deserialize_Null_ThrowJsonException()
    {
        // `null` literal en JSON: el converter espera un string y reporta error legible.
        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<InstallmentType>("null", Options));

        Assert.Contains("InstallmentType", ex.Message);
    }

    [Fact]
    public void Deserialize_Numero_ThrowJsonException()
    {
        // El converter rechaza valores enteros — fuerza uso de nombres literales.
        var ex = Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<InstallmentType>("0", Options));

        Assert.Contains("string", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_ValorEnum_EmiteStringCanonico()
    {
        var json = JsonSerializer.Serialize(InstallmentType.GERMAN, Options);

        Assert.Equal("\"GERMAN\"", json);
    }
}

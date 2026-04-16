using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreditsSim.WebAPI.Swagger;

/// <summary>
/// JsonStringEnumConverter estricto: rechaza variantes de case (ej: "german" en vez de "GERMAN").
/// El JsonStringEnumConverter por default es case-insensitive; usamos este para forzar el
/// contrato declarado en OpenAPI ("enum": ["FIXED", "GERMAN"]) con valor exacto.
/// </summary>
public class StrictEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Valor invalido para {typeof(TEnum).Name}. Se esperaba un string.");

        var value = reader.GetString();
        if (!Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            throw new JsonException(
                $"Valor '{value}' invalido para {typeof(TEnum).Name}. Valores permitidos (case-sensitive): {string.Join(", ", Enum.GetNames<TEnum>())}.");

        return parsed;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>Factory que registra StrictEnumConverter para cualquier enum al serializar.</summary>
public class StrictEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrictEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

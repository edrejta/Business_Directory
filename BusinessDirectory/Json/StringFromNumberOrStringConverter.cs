using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusinessDirectory.Json;

/// <summary>
/// Konverteson edhe numra në string kur frontendi dërgon gabimisht numra (p.sh. username si number).
/// Zvogëlon 400 "The JSON value could not be converted to System.String" kur payload-i ka lloje të gabuara.
/// </summary>
public sealed class StringFromNumberOrStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException($"Pritet string ose number për fushën, u gjet: {reader.TokenType}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

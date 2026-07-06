using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Serialization;

public sealed class NullableByteArrayJsonConverter : JsonConverter<byte[]?>
{
    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Convert.FromBase64String(value);
        }

        throw new JsonException($"Cannot convert JSON token '{reader.TokenType}' to byte[].");
    }

    public override void Write(Utf8JsonWriter writer, byte[]? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteBase64StringValue(value);
    }
}

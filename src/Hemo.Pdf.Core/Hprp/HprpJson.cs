using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JsonElementUndefinedConverter(),
        },
    };

    public static void ApplyTo(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Converters.Any(c => c is JsonElementUndefinedConverter))
            return;

        options.Converters.Insert(0, new JsonElementUndefinedConverter());
    }

    /// <summary>
    /// Default STJ cannot write <see cref="JsonValueKind.Undefined"/> (omitted layout title/when/content).
    /// </summary>
    public sealed class JsonElementUndefinedConverter : JsonConverter<JsonElement>
    {
        public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonElement.ParseValue(ref reader);

        public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
        {
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                writer.WriteNullValue();
                return;
            }

            value.WriteTo(writer);
        }
    }
}

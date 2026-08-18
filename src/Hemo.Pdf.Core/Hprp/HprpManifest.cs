using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpManifest
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("engineVersion")]
    public int EngineVersion { get; init; } = HprpEngine.CurrentVersion;

    [JsonPropertyName("dataAdapter")]
    public string DataAdapter { get; init; } = HprpDataAdapterIds.FlattenDto;

    [JsonPropertyName("requiresSignature")]
    public bool RequiresSignature { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }
}

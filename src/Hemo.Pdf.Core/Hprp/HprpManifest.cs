using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpManifest
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    /// <summary>Folder key under <c>reports/{id}/variants/{variant}</c>. Empty for single-package reports.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    /// <summary>Composer path already in C#: <c>DefaultForm</c>, <c>ThaiUrForm</c>, or <c>UniquePlanner</c>.</summary>
    [JsonPropertyName("layoutKind")]
    public string? LayoutKind { get; init; }

    /// <summary>Tenant setting value (<c>Default</c> / <c>Rama</c> / <c>ThaiUr</c>) when this package is a hemosheet layout profile.</summary>
    [JsonPropertyName("layoutProfile")]
    public string? LayoutProfile { get; init; }

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

    /// <summary>
    /// <c>composition</c> (default) or experimental <c>absolute</c> freeform canvas.
    /// Omitted / null means composition.
    /// </summary>
    [JsonPropertyName("layoutMode")]
    public string? LayoutMode { get; init; }

    /// <summary>Optional FE menu / picker / parameter metadata.</summary>
    [JsonPropertyName("ui")]
    public HprpManifestUi? Ui { get; init; }
}

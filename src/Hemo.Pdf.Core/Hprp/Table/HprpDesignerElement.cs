using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

public sealed class HprpDesignerBox
{
    [JsonPropertyName("xMm")]
    public float XMm { get; init; }

    [JsonPropertyName("yMm")]
    public float YMm { get; init; }

    [JsonPropertyName("wMm")]
    public float WMm { get; init; } = 40;

    [JsonPropertyName("hMm")]
    public float HMm { get; init; } = 20;
}

public sealed class HprpTableBinding
{
    /// <summary>JSON path e.g. <c>months[].entries[].hb</c></summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    /// <summary>Column id (<c>hb</c>) or date part (<c>month</c>, <c>day</c>).</summary>
    [JsonPropertyName("column")]
    public string Column { get; init; } = "";

    /// <summary><see cref="HprpTableBindingContexts"/></summary>
    [JsonPropertyName("context")]
    public string Context { get; init; } = HprpTableBindingContexts.Entry;
}

public sealed class HprpDesignerElement
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    /// <summary><see cref="HprpDesignerElementTypes"/></summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = HprpDesignerElementTypes.ConfigTable;

    [JsonPropertyName("box")]
    public HprpDesignerBox Box { get; init; } = new();

    /// <summary>Header preset id (e.g. <c>thaiur-header-v1</c>).</summary>
    [JsonPropertyName("preset")]
    public string? Preset { get; init; }

    /// <summary>Table preset id for <c>config-table</c>.</summary>
    [JsonPropertyName("presetId")]
    public string? PresetId { get; init; }

    /// <summary>Inline preset when not using <see cref="PresetId"/>.</summary>
    [JsonPropertyName("tablePreset")]
    public HprpTablePreset? TablePreset { get; init; }

    [JsonPropertyName("columnOverrides")]
    public IReadOnlyList<HprpTableColumnDef>? ColumnOverrides { get; init; }

    [JsonPropertyName("bindings")]
    public IReadOnlyList<HprpTableBinding> Bindings { get; init; } = [];

    /// <summary>Legacy dense widget id when <see cref="Type"/> is <c>dense</c>.</summary>
    [JsonPropertyName("widget")]
    public string? Widget { get; init; }

    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }
}

using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Hprp.Header;

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

    /// <summary>Inline header preset (Studio column drag / field edits).</summary>
    [JsonPropertyName("headerPreset")]
    public HprpHeaderPreset? HeaderPreset { get; init; }

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

    /// <summary>
    /// Studio flow: <c>below</c> (default) stacks under previous; <c>beside</c> places to the right.
    /// </summary>
    [JsonPropertyName("place")]
    public string? Place { get; init; }

    /// <summary>When true, width is user-resized and reflow will not stretch to full content width.</summary>
    [JsonPropertyName("manualWidth")]
    public bool ManualWidth { get; init; }

    /// <summary>Hardcoded text for <c>box-text</c> (used when <see cref="Bind"/> is empty or unresolved).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Optional JSON path for <c>box-text</c> e.g. <c>$.coPayCriteria.title</c>.</summary>
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    /// <summary><c>left</c> / <c>center</c> / <c>right</c> for <c>box-text</c>.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    public HprpDesignerElement WithBox(HprpDesignerBox box) => new()
    {
        Id = Id,
        Type = Type,
        Box = box,
        Preset = Preset,
        HeaderPreset = HeaderPreset,
        PresetId = PresetId,
        TablePreset = TablePreset,
        ColumnOverrides = ColumnOverrides,
        Bindings = Bindings,
        Widget = Widget,
        Chrome = Chrome,
        Place = Place,
        ManualWidth = ManualWidth,
        Text = Text,
        Bind = Bind,
        Align = Align,
    };
}

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

/// <summary>
/// One horizontal segment inside a multi-value <c>box-text</c>
/// (e.g. clinical-02 meta: month | EPO | needles/week).
/// </summary>
public sealed class HprpBoxTextItem
{
    /// <summary>Leading label (normal weight), e.g. <c>เดือน</c>.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    /// <summary>Hardcoded primary value when <see cref="Bind"/> is empty.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>JSON path for primary value, e.g. <c>$.meta.monthLabel</c>.</summary>
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    /// <summary>Optional second label after the primary value, e.g. <c>พ.ศ.</c>.</summary>
    [JsonPropertyName("label2")]
    public string? Label2 { get; init; }

    [JsonPropertyName("text2")]
    public string? Text2 { get; init; }

    [JsonPropertyName("bind2")]
    public string? Bind2 { get; init; }

    /// <summary><c>left</c> / <c>center</c> / <c>right</c> within this segment.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    /// <summary>Relative row weight (default 1).</summary>
    [JsonPropertyName("flex")]
    public float? Flex { get; init; }
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

    /// <summary>Header preset id (e.g. <c>clinical-header-thaiur</c>).</summary>
    [JsonPropertyName("preset")]
    public string? Preset { get; init; }

    /// <summary>Inline header preset (Studio column drag / field edits).</summary>
    [JsonPropertyName("headerPreset")]
    public HprpHeaderPreset? HeaderPreset { get; init; }

    /// <summary>
    /// Header bottom profile override (<see cref="HprpHeaderBottomModes"/>).
    /// When set, selects <c>bottomFieldSets[mode]</c> on the shared ThaiUR preset
    /// (e.g. <c>checklist-patient</c> for Default progress note).
    /// </summary>
    [JsonPropertyName("bottomMode")]
    public string? BottomMode { get; init; }

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

    /// <summary>
    /// Hardcoded text for <c>box-text</c>, or format for <c>page-of</c>
    /// (tokens <c>{current}</c> / <c>{total}</c>).
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Optional JSON path for <c>box-text</c> e.g. <c>$.coPayCriteria.title</c>.</summary>
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    /// <summary>
    /// Multi-value horizontal segments for <c>box-text</c>.
    /// When non-empty, takes precedence over single <see cref="Text"/> / <see cref="Bind"/>.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<HprpBoxTextItem>? Items { get; init; }

    /// <summary>
    /// Segments for <see cref="HprpDesignerElementTypes.FieldRow"/>
    /// (checkbox options and/or fill-in text).
    /// </summary>
    [JsonPropertyName("segments")]
    public IReadOnlyList<HprpFieldRowSegment>? Segments { get; init; }

    /// <summary>Page band: <see cref="HprpDesignerBands"/> —
    /// <c>super-header</c> / <c>header</c> / <c>content</c> (default) / <c>footer</c> / <c>super-footer</c>.
    /// Chrome bands repeat on each page; content flows and may create extra pages.
    /// </summary>
    [JsonPropertyName("band")]
    public string? Band { get; init; }

    /// <summary><c>left</c> / <c>center</c> / <c>right</c> for single-value <c>box-text</c>.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    /// <summary>
    /// For <see cref="HprpDesignerElementTypes.Group"/>: <c>column</c> (default) stacks children vertically
    /// inside this block’s width (inner section).
    /// </summary>
    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    /// <summary>Child elements when <see cref="Type"/> is <c>group</c> (max <see cref="HprpDesignerGroupLimits.MaxChildren"/>).</summary>
    [JsonPropertyName("children")]
    public IReadOnlyList<HprpDesignerElement>? Children { get; init; }

    /// <summary>
    /// JSON path that must be truthy for this element to join content flow
    /// (e.g. <c>$.textNotes</c>). Empty array / blank / missing → omit so
    /// optional sections do not create a trailing page with only repeating chrome.
    /// </summary>
    [JsonPropertyName("omitWhenEmpty")]
    public string? OmitWhenEmpty { get; init; }

    /// <summary>JSON path to row data for <c>data-grid</c> e.g. <c>$.rows</c>.</summary>
    [JsonPropertyName("bindRows")]
    public string? BindRows { get; init; }

    /// <summary>Static column headers when not using <see cref="ColumnHeadersBind"/>.</summary>
    [JsonPropertyName("columnHeaders")]
    public IReadOnlyList<string>? ColumnHeaders { get; init; }

    /// <summary>JSON path to column headers e.g. <c>$.columnHeaders</c>.</summary>
    [JsonPropertyName("columnHeadersBind")]
    public string? ColumnHeadersBind { get; init; }

    /// <summary>
    /// Pack-owned paragraphs for <c>narrative</c> (Studio Word-lite editor).
    /// Used when <see cref="BindParagraphs"/> is empty or data has no rows.
    /// </summary>
    [JsonPropertyName("paragraphs")]
    public IReadOnlyList<HprpNarrativeParagraph>? Paragraphs { get; init; }

    /// <summary>
    /// Optional JSON path to paragraph array e.g. <c>$.bodyParagraphs</c>
    /// (<c>{ text, sub }</c>). When present and non-empty, overrides pack paragraphs for PDF.
    /// </summary>
    [JsonPropertyName("bindParagraphs")]
    public string? BindParagraphs { get; init; }

    /// <summary>Maps designer <c>data-grid</c> onto a composition layout node for <see cref="HprpBinder"/>.</summary>
    public HprpLayoutNode ToLayoutNode() => new()
    {
        Type = HprpDesignerElementTypes.DataGrid,
        BindRows = BindRows,
        ColumnHeaders = ColumnHeaders,
        ColumnHeadersBind = ColumnHeadersBind,
        Chrome = Chrome,
    };

    public HprpDesignerElement WithBox(HprpDesignerBox box) => Clone(box: box);

    public HprpDesignerElement WithBoxAndChildren(HprpDesignerBox box, IReadOnlyList<HprpDesignerElement> children) =>
        Clone(box: box, children: children);

    private HprpDesignerElement Clone(
        HprpDesignerBox? box = null,
        IReadOnlyList<HprpDesignerElement>? children = null) => new()
    {
        Id = Id,
        Type = Type,
        Box = box ?? Box,
        Preset = Preset,
        HeaderPreset = HeaderPreset,
        BottomMode = BottomMode,
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
        Items = Items,
        Segments = Segments,
        Align = Align,
        Band = Band,
        Direction = Direction,
        Children = children ?? Children,
        OmitWhenEmpty = OmitWhenEmpty,
        BindRows = BindRows,
        ColumnHeaders = ColumnHeaders,
        ColumnHeadersBind = ColumnHeadersBind,
        Paragraphs = Paragraphs,
        BindParagraphs = BindParagraphs,
    };
}

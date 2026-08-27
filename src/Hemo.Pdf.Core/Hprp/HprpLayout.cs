using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpLayout
{
    [JsonPropertyName("page")]
    public HprpPage Page { get; init; } = new();

    [JsonPropertyName("header")]
    public HprpLayoutNode? Header { get; init; }

    [JsonPropertyName("body")]
    public IReadOnlyList<HprpLayoutNode> Body { get; init; } = [];

    /// <summary>Hemosheet (and similar) widget plan — interpreted by the C# planner.</summary>
    [JsonPropertyName("sections")]
    public IReadOnlyList<HprpSectionNode> Sections { get; init; } = [];

    /// <summary>
    /// Freeform widgets for <see cref="HprpLayoutModes.Absolute"/> (legacy).
    /// Ignored by composition / designer composers.
    /// </summary>
    [JsonPropertyName("widgets")]
    public IReadOnlyList<HprpAbsoluteWidget> Widgets { get; init; } = [];

    /// <summary>
    /// Designer canvas elements for <see cref="HprpLayoutModes.Designer"/>.
    /// </summary>
    [JsonPropertyName("elements")]
    public IReadOnlyList<Hprp.Table.HprpDesignerElement> Elements { get; init; } = [];
}

public sealed class HprpPage
{
    [JsonPropertyName("size")]
    public string Size { get; init; } = "A4";

    /// <summary>Uniform margin (mm) when <see cref="Margin"/> sides are omitted.</summary>
    [JsonPropertyName("marginMm")]
    public float? MarginMm { get; init; }

    [JsonPropertyName("margin")]
    public HprpSides? Margin { get; init; }

    [JsonPropertyName("spacingMm")]
    public float? SpacingMm { get; init; }

    [JsonPropertyName("fontSize")]
    public float? FontSize { get; init; }

    /// <summary><c>portrait</c> (default) or <c>landscape</c>.</summary>
    [JsonPropertyName("orientation")]
    public string? Orientation { get; init; }
}

public sealed class HprpLayoutNode
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("widget")]
    public string? Widget { get; init; }

    [JsonPropertyName("when")]
    public JsonElement When { get; init; }

    [JsonPropertyName("title")]
    public JsonElement Title { get; init; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; init; }

    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    [JsonPropertyName("bindRows")]
    public string? BindRows { get; init; }

    [JsonPropertyName("style")]
    public string? Style { get; init; }

    [JsonPropertyName("columns")]
    public int Columns { get; init; } = 2;

    [JsonPropertyName("columnHeaders")]
    public IReadOnlyList<string>? ColumnHeaders { get; init; }

    [JsonPropertyName("columnHeadersBind")]
    public string? ColumnHeadersBind { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyList<HprpFieldNode>? Fields { get; init; }

    [JsonPropertyName("rows")]
    public IReadOnlyList<HprpRowNode>? Rows { get; init; }

    [JsonPropertyName("appendFlatten")]
    public bool AppendFlatten { get; init; }

    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }

    /// <summary>
    /// Ordered entry columns for dense table recipes (e.g. clinical-01 annual table).
    /// Empty / omitted = widget default. Not the same as <see cref="Columns"/> (field-grid count)
    /// or hemosheet <see cref="HprpSectionNode.Columns"/> (string[] field ids).
    /// </summary>
    [JsonPropertyName("columnPlan")]
    public IReadOnlyList<HprpColumnPlanItem>? ColumnPlan { get; init; }

    [JsonPropertyName("box")]
    public HprpNodeBox? Box { get; init; }

    [JsonPropertyName("gapMm")]
    public float? GapMm { get; init; }

    /// <summary>Cells for <c>type: row</c>.</summary>
    [JsonPropertyName("cells")]
    public IReadOnlyList<HprpCellNode>? Cells { get; init; }

    /// <summary>Child nodes for <c>type: column-stack</c> (or implicit stack inside a cell).</summary>
    [JsonPropertyName("nodes")]
    public IReadOnlyList<HprpLayoutNode>? Nodes { get; init; }
}

public sealed class HprpCellNode
{
    [JsonPropertyName("width")]
    public string? Width { get; init; }

    [JsonPropertyName("nodes")]
    public IReadOnlyList<HprpLayoutNode> Nodes { get; init; } = [];
}

public sealed class HprpColumnPlanItem
{
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    [JsonPropertyName("labelKey")]
    public string? LabelKey { get; init; }

    [JsonPropertyName("weight")]
    public float? Weight { get; init; }

    [JsonPropertyName("center")]
    public bool? Center { get; init; }

    [JsonPropertyName("isLab")]
    public bool? IsLab { get; init; }
}

public sealed class HprpFieldNode
{
    [JsonPropertyName("label")]
    public JsonElement Label { get; init; }

    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; init; }

    [JsonPropertyName("columnSpan")]
    public int ColumnSpan { get; init; } = 1;
}

public sealed class HprpRowNode
{
    [JsonPropertyName("label")]
    public JsonElement Label { get; init; }

    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; init; }
}

public sealed class HprpSectionNode
{
    [JsonPropertyName("widget")]
    public string Widget { get; init; } = "";

    [JsonPropertyName("when")]
    public JsonElement When { get; init; }

    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    [JsonPropertyName("columns")]
    public IReadOnlyList<string>? Columns { get; init; }

    [JsonPropertyName("columnsWhen")]
    public Dictionary<string, string[]>? ColumnsWhen { get; init; }

    [JsonPropertyName("fixedLinesFrom")]
    public string? FixedLinesFrom { get; init; }

    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }
}

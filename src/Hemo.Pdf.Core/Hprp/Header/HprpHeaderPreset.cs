using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Hprp.Header;

public static class HprpHeaderColumnKinds
{
    public const string Logo = "logo";
    public const string Title = "title";
    public const string Meta = "meta";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Logo,
        Title,
        Meta,
    };
}

/// <summary>Reusable clinical header structure (band columns + field lines). Bindings on fields.</summary>
public sealed class HprpHeaderPreset
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("titleRowHeightMm")]
    public float TitleRowHeightMm { get; init; } = 21.6f;

    [JsonPropertyName("bottomRowHeightMm")]
    public float BottomRowHeightMm { get; init; } = 5.4f;

    [JsonPropertyName("showDateAndHdNo")]
    public bool ShowDateAndHdNo { get; init; }

    [JsonPropertyName("showHdPerWeek")]
    public bool ShowHdPerWeek { get; init; } = true;

    /// <summary>Top band: logo | title | meta (fixed mm and/or relative weight).</summary>
    [JsonPropertyName("columns")]
    public IReadOnlyList<HprpHeaderBandColumn> Columns { get; init; } = [];

    /// <summary>Lines inside the meta (right) panel.</summary>
    [JsonPropertyName("metaLines")]
    public IReadOnlyList<HprpHeaderFieldLine> MetaLines { get; init; } = [];

    /// <summary>Bottom diagnosis / allergy / HD row.</summary>
    [JsonPropertyName("bottomFields")]
    public IReadOnlyList<HprpHeaderFieldLine> BottomFields { get; init; } = [];

    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }
}

public sealed class HprpHeaderBandColumn
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    /// <summary><see cref="HprpHeaderColumnKinds"/></summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = HprpHeaderColumnKinds.Title;

    /// <summary>Fixed width in mm. When set, preferred over <see cref="Weight"/>.</summary>
    [JsonPropertyName("widthMm")]
    public float? WidthMm { get; init; }

    [JsonPropertyName("weight")]
    public float Weight { get; init; } = 1f;

    [JsonPropertyName("bind")]
    public string? Bind { get; init; }
}

public sealed class HprpHeaderFieldLine
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("label")]
    public string Label { get; init; } = "";

    /// <summary>JSON path e.g. <c>$.header.patient.name</c></summary>
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    [JsonPropertyName("label2")]
    public string? Label2 { get; init; }

    [JsonPropertyName("bind2")]
    public string? Bind2 { get; init; }

    /// <summary>Relative weight on the bottom row (ignored for meta lines).</summary>
    [JsonPropertyName("weight")]
    public float Weight { get; init; } = 1f;

    /// <summary>When true, field is only shown if <c>showHdPerWeek</c> is on.</summary>
    [JsonPropertyName("whenHdPerWeek")]
    public bool WhenHdPerWeek { get; init; }
}

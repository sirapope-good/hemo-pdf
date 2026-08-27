using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>Freeform widget for <see cref="HprpLayoutModes.Absolute"/> packages (experimental).</summary>
public sealed class HprpAbsoluteWidget
{
    public static readonly IReadOnlySet<string> PrimitiveTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "text", "frame", "table",
    };

    public const string TypeDense = "dense";

    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    /// <summary><c>text</c> | <c>frame</c> | <c>table</c> | <c>dense</c></summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    /// <summary>
    /// Dense clinical widget id (e.g. <c>thaiur.header</c>) when <see cref="Type"/> is <c>dense</c>.
    /// Same ids as composition <c>layout.body[].widget</c> — reusable across absolute packs.
    /// </summary>
    [JsonPropertyName("widget")]
    public string? Widget { get; init; }

    [JsonPropertyName("xMm")]
    public float XMm { get; init; }

    [JsonPropertyName("yMm")]
    public float YMm { get; init; }

    [JsonPropertyName("wMm")]
    public float WMm { get; init; } = 40;

    [JsonPropertyName("hMm")]
    public float HMm { get; init; } = 20;

    [JsonPropertyName("zIndex")]
    public int ZIndex { get; init; } = 1;

    [JsonPropertyName("style")]
    public HprpAbsoluteWidgetStyle? Style { get; init; }

    /// <summary>Optional chrome override (same shape as composition layout nodes).</summary>
    [JsonPropertyName("chrome")]
    public HprpChrome? Chrome { get; init; }

    /// <summary>Optional column plan override for dense table widgets (e.g. clinical-01 annual).</summary>
    [JsonPropertyName("columnPlan")]
    public IReadOnlyList<HprpColumnPlanItem>? ColumnPlan { get; init; }

    /// <summary>Type-specific payload (text content, table headers/rows, frame label).</summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }

    public bool IsDense =>
        string.Equals(Type, TypeDense, StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrWhiteSpace(Widget) && !PrimitiveTypes.Contains(Type));

    public string? ResolveDenseWidgetId()
    {
        if (!string.IsNullOrWhiteSpace(Widget))
            return Widget.Trim();

        if (string.Equals(Type, TypeDense, StringComparison.OrdinalIgnoreCase))
            return null;

        // Allow type = widget id for compact authoring (same as composition).
        if (!string.IsNullOrWhiteSpace(Type)
            && !PrimitiveTypes.Contains(Type)
            && HprpWidgetIds.All.Contains(Type))
        {
            return Type.Trim();
        }

        return null;
    }

    /// <summary>Maps absolute dense overrides onto a composition layout node for section composers.</summary>
    public HprpLayoutNode ToLayoutNode() => new()
    {
        Widget = ResolveDenseWidgetId(),
        Chrome = Chrome,
        ColumnPlan = ColumnPlan,
    };
}

public sealed class HprpAbsoluteWidgetStyle
{
    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; init; }

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; init; }

    [JsonPropertyName("borderWidth")]
    public float? BorderWidth { get; init; }

    [JsonPropertyName("borderStyle")]
    public string? BorderStyle { get; init; }
}

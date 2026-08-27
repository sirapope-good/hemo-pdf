using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>Freeform widget for <see cref="HprpLayoutModes.Absolute"/> packages (experimental).</summary>
public sealed class HprpAbsoluteWidget
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    /// <summary><c>text</c> | <c>frame</c> | <c>table</c></summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

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

    /// <summary>Type-specific payload (text content, table headers/rows, frame label).</summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
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

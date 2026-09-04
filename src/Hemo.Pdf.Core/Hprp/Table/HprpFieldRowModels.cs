using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

/// <summary>One selectable choice inside a <c>field-row</c> options segment.</summary>
public sealed class HprpFieldOption
{
    /// <summary>Canonical value stored / compared (e.g. <c>ชาย</c> or <c>M</c>).</summary>
    [JsonPropertyName("value")]
    public string Value { get; init; } = "";

    /// <summary>Printed label next to the checkbox (defaults to <see cref="Value"/>).</summary>
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    /// <summary>Extra strings that also select this option (e.g. <c>M</c>, <c>Male</c>).</summary>
    [JsonPropertyName("match")]
    public IReadOnlyList<string>? Match { get; init; }
}

/// <summary>
/// One horizontal segment in a <c>field-row</c>:
/// <c>options</c> (checkbox list) or <c>text</c> (label + bound value / blank line).
/// </summary>
public sealed class HprpFieldRowSegment
{
    /// <summary><c>options</c> | <c>text</c> (default <c>text</c>).</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = HprpFieldRowSegmentKinds.Text;

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    /// <summary>JSON path for selected value / fill-in text.</summary>
    [JsonPropertyName("bind")]
    public string? Bind { get; init; }

    /// <summary>Hardcoded text when bind is empty (text kind only).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("options")]
    public IReadOnlyList<HprpFieldOption>? Options { get; init; }

    /// <summary>Relative width weight (default 1).</summary>
    [JsonPropertyName("flex")]
    public float? Flex { get; init; }

    /// <summary><c>left</c> / <c>center</c> / <c>right</c>.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    /// <summary>
    /// When true and the bound value is empty, draw a dotted underline for handwriting
    /// (blank-form printing).
    /// </summary>
    [JsonPropertyName("blankLine")]
    public bool BlankLine { get; init; } = true;

    /// <summary>Wrap checkbox options onto additional lines (options kind).</summary>
    [JsonPropertyName("wrap")]
    public bool Wrap { get; init; }

    /// <summary>Max options per line when <see cref="Wrap"/> is true (default 4).</summary>
    [JsonPropertyName("optionsPerLine")]
    public int? OptionsPerLine { get; init; }
}

public static class HprpFieldRowSegmentKinds
{
    public const string Options = "options";
    public const string Text = "text";
}

/// <summary>Pure matching helpers for field-row checkbox selection.</summary>
public static class HprpFieldRowMatching
{
    public static bool IsSelected(string? boundValue, HprpFieldOption option)
    {
        if (option is null || string.IsNullOrWhiteSpace(boundValue))
            return false;

        var raw = boundValue.Trim();
        if (ValuesEqual(raw, option.Value))
            return true;

        if (!string.IsNullOrWhiteSpace(option.Label) && ValuesEqual(raw, option.Label))
            return true;

        if (option.Match is { Count: > 0 })
        {
            foreach (var alias in option.Match)
            {
                if (ValuesEqual(raw, alias))
                    return true;
            }
        }

        return false;
    }

    public static string DisplayLabel(HprpFieldOption option) =>
        string.IsNullOrWhiteSpace(option.Label) ? (option.Value ?? "") : option.Label!.Trim();

    private static bool ValuesEqual(string a, string? b) =>
        !string.IsNullOrWhiteSpace(b)
        && string.Equals(a, b.Trim(), StringComparison.OrdinalIgnoreCase);
}

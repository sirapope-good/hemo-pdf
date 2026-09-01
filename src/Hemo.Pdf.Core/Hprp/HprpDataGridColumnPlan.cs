using System.Globalization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Resolves designer/composition <c>chrome.columnWidths</c> for lab-style grids
/// where DATE column count comes from data (<c>columnHeadersBind</c>) and may differ from saved tokens.
/// </summary>
public static class HprpDataGridColumnPlan
{
    /// <summary>Default lab layout: narrow first column + equal flexible date columns.</summary>
    public static readonly string[] DefaultLabTokens = ["3", "*"];

    /// <summary>Placeholder for unfilled DATE columns (matches clinical-07 matrix).</summary>
    public const string EmptyDateHeaderLabel = "DATE";

    /// <summary>
    /// Lab grid DATE row: column 0 is always blank; empty date slots show <see cref="EmptyDateHeaderLabel"/>.
    /// </summary>
    public static IReadOnlyList<string> NormalizeLabColumnHeaders(IReadOnlyList<string>? headers)
    {
        if (headers is not { Count: > 0 })
            return headers ?? [];

        var result = headers.ToList();
        result[0] = "";
        for (var i = 1; i < result.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(result[i]))
                result[i] = EmptyDateHeaderLabel;
        }

        return result;
    }

    /// <summary>
    /// Resolves <paramref name="columnWidths"/> into <paramref name="columnCount"/> relative weights.
    /// When token count matches column count, uses exact parse (same as <see cref="HprpChrome.ParseColumnWeights"/>).
    /// Otherwise: token[0] = lab column, token[1] (or <c>*</c>) applies to each remaining column.
    /// </summary>
    public static IReadOnlyList<float> Resolve(IReadOnlyList<string>? columnWidths, int columnCount)
    {
        if (columnCount <= 0)
            return [];

        var exact = HprpChrome.ParseColumnWeights(columnWidths, columnCount);
        if (exact.Count == columnCount)
            return exact;

        var tokens = columnWidths is { Count: > 0 } ? columnWidths : DefaultLabTokens;
        var lab = ParseWeightToken(tokens, 0, fallback: 3f);
        var date = tokens.Count > 1
            ? ParseWeightToken(tokens, 1, fallback: 1f)
            : 1f;

        var result = new float[columnCount];
        result[0] = lab;
        for (var i = 1; i < columnCount; i++)
            result[i] = date;

        return result;
    }

    /// <summary>
    /// Normalizes persisted tokens to one entry per column (for Studio save / exact PDF match).
    /// </summary>
    public static IReadOnlyList<string> NormalizeTokens(IReadOnlyList<string>? columnWidths, int columnCount)
    {
        if (columnCount <= 0)
            return [];

        if (columnWidths is { Count: > 0 }
            && columnWidths.Count == columnCount)
        {
            return columnWidths.ToList();
        }

        var weights = Resolve(columnWidths, columnCount);
        var source = columnWidths is { Count: > 0 } ? columnWidths : DefaultLabTokens;
        var result = new string[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var raw = i < source.Count ? source[i] : (i == 0 ? source[0] : (source.Count > 1 ? source[1] : "*"));
            result[i] = FormatWeightToken(weights[i], raw);
        }

        return result;
    }

    internal static float ParseWeightToken(IReadOnlyList<string> tokens, int index, float fallback)
    {
        if (index < 0 || index >= tokens.Count)
            return fallback;

        var token = tokens[index]?.Trim() ?? "";
        if (token.Length == 0 || token == "*")
            return fallback;

        if (token.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            token = token[..^2].Trim();

        return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;
    }

    internal static string FormatWeightToken(float weight, string? previousToken)
    {
        var prev = previousToken?.Trim() ?? "";
        if (prev == "*" && weight is >= 0.999f and <= 1.001f)
            return "*";

        if (prev.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            return string.Create(CultureInfo.InvariantCulture, $"{weight:0.##}mm");

        if (weight is >= 0.999f and <= 1.001f && (prev.Length == 0 || prev == "*"))
            return "*";

        return weight.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

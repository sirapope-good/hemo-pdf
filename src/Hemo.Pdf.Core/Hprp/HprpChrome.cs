using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// File-driven table chrome for primitive blocks and hemosheet section nodes.
/// Omitted fields keep engine / branding defaults.
/// </summary>
public sealed class HprpChrome
{
    public const string BrandingHeaderFill = "$branding.sectionHeaderBackground";

    [JsonPropertyName("headerFill")]
    public string? HeaderFill { get; init; }

    /// <summary><c>none</c>, <c>thin</c> (default), or <c>medium</c>.</summary>
    [JsonPropertyName("border")]
    public string? Border { get; init; }

    [JsonPropertyName("fontSize")]
    public float? FontSize { get; init; }

    [JsonPropertyName("rowHeightMm")]
    public float? RowHeightMm { get; init; }

    /// <summary>Relative weights per column; <c>*</c> = 1. Applied when count matches.</summary>
    [JsonPropertyName("columnWidths")]
    public IReadOnlyList<string>? ColumnWidths { get; init; }

    public static string ResolveHeaderFill(
        HprpChrome? chrome,
        PdfReportContext? context,
        string fallback)
    {
        var raw = chrome?.HeaderFill?.Trim();
        if (!string.IsNullOrWhiteSpace(raw)
            && !string.Equals(raw, BrandingHeaderFill, StringComparison.OrdinalIgnoreCase))
        {
            var fromFile = ReportSectionHeaderChrome.Normalize(raw);
            if (fromFile is not null)
                return fromFile;
        }

        return ReportSectionHeaderChrome.Resolve(context, fallback);
    }

    /// <summary>Hex from the file only — null means keep ambient branding.</summary>
    public static string? FileHeaderFillOrNull(HprpChrome? chrome)
    {
        var raw = chrome?.HeaderFill?.Trim();
        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, BrandingHeaderFill, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return ReportSectionHeaderChrome.Normalize(raw);
    }

    /// <summary>
    /// First file hex on body/section nodes. Skips <see cref="BrandingHeaderFill"/>
    /// and nodes gated by <c>when</c> still count — the color lives in the pack even
    /// when the grid is not bound.
    /// </summary>
    public static string? FirstFileHeaderFillFromLayout(HprpLayout? layout)
    {
        if (layout is null)
            return null;

        foreach (var node in layout.Body)
        {
            var fill = FileHeaderFillOrNull(node.Chrome);
            if (fill is not null)
                return fill;
        }

        foreach (var section in layout.Sections)
        {
            var fill = FileHeaderFillOrNull(section.Chrome);
            if (fill is not null)
                return fill;
        }

        return null;
    }

    public static float ResolveBorderWidth(HprpChrome? chrome) =>
        chrome?.Border?.Trim().ToLowerInvariant() switch
        {
            "none" => 0f,
            "medium" => 1f,
            _ => 0.5f,
        };

    public static float ResolveFontSize(HprpChrome? chrome, float fallback) =>
        chrome?.FontSize is > 0 and < 48 ? chrome.FontSize.Value : fallback;

    public static IReadOnlyList<float> ParseColumnWeights(IReadOnlyList<string>? widths, int columnCount)
    {
        if (widths is null || widths.Count != columnCount || columnCount <= 0)
            return [];

        var result = new float[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            var token = widths[i]?.Trim() ?? "";
            if (token.Length == 0 || token == "*")
            {
                result[i] = 1f;
                continue;
            }

            if (token.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
                token = token[..^2].Trim();

            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || value <= 0)
            {
                return [];
            }

            result[i] = value;
        }

        return result;
    }

    public static void Validate(HprpChrome? chrome, string path, List<string> errors)
    {
        if (chrome is null)
            return;

        var fill = chrome.HeaderFill?.Trim();
        if (!string.IsNullOrWhiteSpace(fill)
            && !string.Equals(fill, BrandingHeaderFill, StringComparison.OrdinalIgnoreCase)
            && ReportSectionHeaderChrome.Normalize(fill) is null)
        {
            errors.Add($"{path}.headerFill must be #RRGGBB or {BrandingHeaderFill}.");
        }

        var border = chrome.Border?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(border)
            && border is not ("none" or "thin" or "medium"))
        {
            errors.Add($"{path}.border must be none, thin, or medium.");
        }

        if (chrome.FontSize is <= 0 or >= 48)
            errors.Add($"{path}.fontSize must be between 0 and 48.");

        if (chrome.RowHeightMm is <= 0 or > 80)
            errors.Add($"{path}.rowHeightMm must be between 0 and 80.");
    }
}

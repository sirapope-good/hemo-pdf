using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp;

public enum HprpHeaderAlign
{
    Middle,
    Top,
    Bottom,
}

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

    /// <summary>Relative weights per column; <c>*</c> = 1. Tokens ending in <c>mm</c> are constant mm.</summary>
    [JsonPropertyName("columnWidths")]
    public IReadOnlyList<string>? ColumnWidths { get; init; }

    /// <summary>
    /// Vertical band weights inside a cell (e.g. clinical-05 SOAP S:O:A:P).
    /// Omitted = widget default.
    /// </summary>
    [JsonPropertyName("bandWeights")]
    public IReadOnlyList<float>? BandWeights { get; init; }

    /// <summary>Table column-header bar height (mm). Omitted = widget default.</summary>
    [JsonPropertyName("headerHeightMm")]
    public float? HeaderHeightMm { get; init; }

    /// <summary><c>top</c>, <c>middle</c> (default), or <c>bottom</c> for header label vertical align.</summary>
    [JsonPropertyName("headerAlign")]
    public string? HeaderAlign { get; init; }

    /// <summary>Uniform inset inside the header cell (mm). Omitted = 0.</summary>
    [JsonPropertyName("headerPaddingMm")]
    public float? HeaderPaddingMm { get; init; }

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

        if (chrome.RowHeightMm is <= 0 or > 200)
            errors.Add($"{path}.rowHeightMm must be between 0 and 200.");

        if (chrome.HeaderHeightMm is <= 0 or > HprpBox.MaxMm)
            errors.Add($"{path}.headerHeightMm must be between 0 and {HprpBox.MaxMm}.");

        var align = chrome.HeaderAlign?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(align)
            && align is not ("top" or "middle" or "bottom"))
        {
            errors.Add($"{path}.headerAlign must be top, middle, or bottom.");
        }

        if (chrome.HeaderPaddingMm is < 0 or > HprpBox.MaxMm)
            errors.Add($"{path}.headerPaddingMm must be between 0 and {HprpBox.MaxMm}.");

        if (chrome.BandWeights is { Count: > 0 } bands)
        {
            if (bands.Any(w => w <= 0 || float.IsNaN(w) || float.IsInfinity(w)))
                errors.Add($"{path}.bandWeights must be positive finite numbers.");
        }
    }

    public static float ResolveHeaderHeightMm(HprpChrome? chrome, float fallback) =>
        chrome?.HeaderHeightMm is > 0 and <= HprpBox.MaxMm
            ? chrome.HeaderHeightMm.Value
            : fallback;

    /// <summary>Omitted / unknown → middle (engine default).</summary>
    public static HprpHeaderAlign ResolveHeaderAlign(HprpChrome? chrome) =>
        chrome?.HeaderAlign?.Trim().ToLowerInvariant() switch
        {
            "top" => HprpHeaderAlign.Top,
            "bottom" => HprpHeaderAlign.Bottom,
            _ => HprpHeaderAlign.Middle,
        };

    public static float ResolveHeaderPaddingMm(HprpChrome? chrome) =>
        chrome?.HeaderPaddingMm is >= 0 and <= HprpBox.MaxMm
            ? chrome.HeaderPaddingMm.Value
            : 0f;

    /// <summary>
    /// Parses mixed constant-mm / relative column tokens.
    /// Empty result = invalid or omitted — caller keeps defaults.
    /// </summary>
    public static IReadOnlyList<(bool ConstantMm, float Value)> ParseMixedColumns(IReadOnlyList<string>? widths)
    {
        if (widths is null || widths.Count == 0)
            return [];

        var parsed = new List<(bool ConstantMm, float Value)>(widths.Count);
        foreach (var raw in widths)
        {
            var token = raw?.Trim() ?? "";
            if (token.Length == 0)
                return [];

            var constant = token.EndsWith("mm", StringComparison.OrdinalIgnoreCase);
            if (constant)
                token = token[..^2].Trim();
            else if (token == "*")
                token = "1";

            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || value <= 0)
            {
                return [];
            }

            parsed.Add((constant, value));
        }

        return parsed;
    }

    /// <summary>
    /// Row cell widths: <c>32mm</c> constant, <c>40%</c> relative (percent weight),
    /// <c>*</c> remaining relative (100 − sum of percents, or 1 when no percents).
    /// </summary>
    public static IReadOnlyList<(bool ConstantMm, float Value)> ParseRowCellWidths(IReadOnlyList<string>? widths)
    {
        if (widths is null || widths.Count == 0)
            return [];

        var parsed = new List<(bool ConstantMm, float Value, bool Percent)>(widths.Count);
        var percentSum = 0f;
        var starCount = 0;
        foreach (var raw in widths)
        {
            var token = raw?.Trim() ?? "";
            if (token.Length == 0)
                return [];

            if (token == "*")
            {
                parsed.Add((false, 1f, false));
                starCount++;
                continue;
            }

            var constant = token.EndsWith("mm", StringComparison.OrdinalIgnoreCase);
            var percent = !constant && token.EndsWith("%", StringComparison.OrdinalIgnoreCase);
            if (constant)
                token = token[..^2].Trim();
            else if (percent)
                token = token[..^1].Trim();

            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || value <= 0)
            {
                return [];
            }

            if (percent)
                percentSum += value;

            parsed.Add((constant, value, percent));
        }

        var starWeight = starCount > 0
            ? Math.Max(100f - percentSum, 1f) / starCount
            : 1f;

        var result = new List<(bool ConstantMm, float Value)>(parsed.Count);
        foreach (var (constant, value, percent) in parsed)
        {
            if (constant)
            {
                result.Add((true, value));
                continue;
            }

            if (percent)
            {
                result.Add((false, value));
                continue;
            }

            result.Add((false, starWeight));
        }

        return result;
    }

    public static IReadOnlyList<float> ResolveBandWeights(
        IReadOnlyList<float>? configured,
        IReadOnlyList<float> defaults)
    {
        if (configured is null || configured.Count == 0)
            return defaults;

        if (configured.Any(w => w <= 0 || float.IsNaN(w) || float.IsInfinity(w)))
            return defaults;

        return configured;
    }

    /// <summary>
    /// Overlay element chrome onto preset chrome; null overlay fields keep the base value.
    /// </summary>
    public static HprpChrome? Merge(HprpChrome? bas, HprpChrome? overlay)
    {
        if (overlay is null)
            return bas;
        if (bas is null)
            return overlay;

        return new HprpChrome
        {
            HeaderFill = overlay.HeaderFill ?? bas.HeaderFill,
            Border = overlay.Border ?? bas.Border,
            FontSize = overlay.FontSize ?? bas.FontSize,
            RowHeightMm = overlay.RowHeightMm ?? bas.RowHeightMm,
            ColumnWidths = overlay.ColumnWidths ?? bas.ColumnWidths,
            BandWeights = overlay.BandWeights ?? bas.BandWeights,
            HeaderHeightMm = overlay.HeaderHeightMm ?? bas.HeaderHeightMm,
            HeaderAlign = overlay.HeaderAlign ?? bas.HeaderAlign,
            HeaderPaddingMm = overlay.HeaderPaddingMm ?? bas.HeaderPaddingMm,
        };
    }
}

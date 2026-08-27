using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Models;

/// <summary>View model for designer (config-table) canvas packages.</summary>
public sealed class DesignerCanvasViewModel
{
    public string Title { get; init; } = "";
    public bool Landscape { get; init; }
    public HprpResolvedPage Page { get; init; }
    /// <summary><c>none</c> / <c>thin</c> — page frame for PDF (optional).</summary>
    public string? PageBorder { get; init; }
    public IReadOnlyList<HprpDesignerElement> Elements { get; init; } = [];
    public JsonElement? Data { get; init; }
    public IReadOnlyDictionary<string, string> Labels { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, HprpTablePreset> Presets { get; init; } =
        new Dictionary<string, HprpTablePreset>(StringComparer.OrdinalIgnoreCase);

    public static DesignerCanvasViewModel FromPackage(
        HprpPackage package,
        JsonElement? data = null,
        IReadOnlyDictionary<string, string>? labels = null,
        IReadOnlyDictionary<string, HprpTablePreset>? presets = null)
    {
        var page = HprpPageLayout.FromPackage(package, HprpPageFallback.Uniform(2f, 0f));
        var landscape = string.Equals(
            package.Layout.Page.Orientation,
            "landscape",
            StringComparison.OrdinalIgnoreCase);

        return new DesignerCanvasViewModel
        {
            Title = package.Manifest.DisplayName,
            Landscape = landscape,
            Page = page,
            PageBorder = package.Layout.Page.Border,
            Elements = package.Layout.Elements,
            Data = data,
            Labels = labels ?? package.GetLabels(package.Manifest.Language),
            Presets = presets ?? new Dictionary<string, HprpTablePreset>(StringComparer.OrdinalIgnoreCase),
        };
    }

    public HemosheetReportViewModel? ReadHeader()
    {
        if (Data is not JsonElement json
            || json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty("header", out var headerEl)
            || headerEl.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonSerializer.Deserialize<HemosheetReportViewModel>(
            headerEl.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public HctEpoReportViewModel? ReadHctEpo()
    {
        if (Data is not JsonElement json || json.ValueKind != JsonValueKind.Object)
            return null;

        return JsonSerializer.Deserialize<HctEpoReportViewModel>(
            json.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

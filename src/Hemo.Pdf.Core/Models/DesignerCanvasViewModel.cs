using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;
using Hemo.Pdf.Core.Hprp.Table;
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
    public IReadOnlyList<HprpDesignerPageSlice> Pages { get; init; } = [];
    public int PageCount { get; init; } = 1;
    public float ContentFlowHeightMm { get; init; }
    public JsonElement? Data { get; init; }
    public IReadOnlyDictionary<string, string> Labels { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, HprpTablePreset> Presets { get; init; } =
        new Dictionary<string, HprpTablePreset>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, HprpHeaderPreset> HeaderPresets { get; init; } =
        new Dictionary<string, HprpHeaderPreset>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Typed report model for <c>type: dense</c> hosts (e.g. clinical-05 SOAP / checklist).
    /// When null, dense fall back to JSON readers such as <see cref="ReadHctEpo"/>.
    /// </summary>
    public object? BoundModel { get; init; }

    public static DesignerCanvasViewModel FromPackage(
        HprpPackage package,
        JsonElement? data = null,
        IReadOnlyDictionary<string, string>? labels = null,
        IReadOnlyDictionary<string, HprpTablePreset>? presets = null,
        IReadOnlyDictionary<string, HprpHeaderPreset>? headerPresets = null,
        object? boundModel = null)
    {
        var page = HprpPageLayout.FromPackage(package, HprpPageFallback.Uniform(2f, 0f));
        var landscape = string.Equals(
            package.Layout.Page.Orientation,
            "landscape",
            StringComparison.OrdinalIgnoreCase);

        const float a4W = 210f;
        const float a4H = 297f;
        var pageW = landscape ? a4H : a4W;
        var pageH = landscape ? a4W : a4H;
        var contentW = Math.Max(10f, pageW - page.Left - page.Right);
        var flow = HprpDesignerFlow.ReflowDetailed(
            package.Layout.Page,
            package.Layout.Elements,
            contentW,
            pageH,
            page.Top,
            page.Bottom,
            page.Left,
            fallbackSpacingMm: 2f);

        return new DesignerCanvasViewModel
        {
            Title = package.Manifest.DisplayName,
            Landscape = landscape,
            Page = page,
            PageBorder = package.Layout.Page.Border,
            Elements = flow.FlatElements,
            Pages = flow.Pages,
            PageCount = flow.PageCount,
            ContentFlowHeightMm = flow.ContentFlowHeightMm,
            Data = data,
            Labels = labels ?? package.GetLabels(package.Manifest.Language),
            Presets = presets ?? new Dictionary<string, HprpTablePreset>(StringComparer.OrdinalIgnoreCase),
            HeaderPresets = headerPresets
                ?? new Dictionary<string, HprpHeaderPreset>(StringComparer.OrdinalIgnoreCase),
            BoundModel = boundModel,
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

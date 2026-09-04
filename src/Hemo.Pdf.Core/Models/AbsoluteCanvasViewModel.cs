using System.Text.Json;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Models;

/// <summary>View model for experimental absolute (mm) canvas packages.</summary>
public sealed class AbsoluteCanvasViewModel
{
    public string Title { get; init; } = "";
    public bool Landscape { get; init; }
    public HprpResolvedPage Page { get; init; }
    public IReadOnlyList<HprpAbsoluteWidget> Widgets { get; init; } = [];

    /// <summary>
    /// Bound report DTO for dense widgets (e.g. <c>HctEpoReportViewModel</c> when
    /// <c>dataAdapter</c> is clinical-01). Primitive text/frame/table ignore this.
    /// </summary>
    public object? BoundModel { get; init; }

    public IReadOnlyDictionary<string, string> Labels { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static AbsoluteCanvasViewModel FromPackage(
        HprpPackage package,
        object? boundModel = null,
        IReadOnlyDictionary<string, string>? labels = null)
    {
        var page = HprpPageLayout.FromPackage(package, HprpPageFallback.Uniform(8f, 0f));
        var landscape = string.Equals(
            package.Layout.Page.Orientation,
            "landscape",
            StringComparison.OrdinalIgnoreCase);

        var resolvedLabels = labels ?? package.GetLabels(package.Manifest.Language);

        return new AbsoluteCanvasViewModel
        {
            Title = package.Manifest.DisplayName,
            Landscape = landscape,
            Page = page,
            Widgets = package.Layout.Widgets
                .OrderBy(w => w.ZIndex)
                .ThenBy(w => w.Id, StringComparer.Ordinal)
                .ToList(),
            BoundModel = boundModel,
            Labels = resolvedLabels,
        };
    }

    public static string DataString(JsonElement data, string name, string fallback = "")
    {
        if (data.ValueKind != JsonValueKind.Object)
            return fallback;
        if (!data.TryGetProperty(name, out var el))
            return fallback;
        return el.ValueKind == JsonValueKind.String ? (el.GetString() ?? fallback) : fallback;
    }
}

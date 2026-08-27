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

    public static AbsoluteCanvasViewModel FromPackage(HprpPackage package)
    {
        var page = HprpPageLayout.FromPackage(package, HprpPageFallback.Uniform(8f, 0f));
        var landscape = string.Equals(
            package.Layout.Page.Orientation,
            "landscape",
            StringComparison.OrdinalIgnoreCase);

        return new AbsoluteCanvasViewModel
        {
            Title = package.Manifest.DisplayName,
            Landscape = landscape,
            Page = page,
            Widgets = package.Layout.Widgets
                .OrderBy(w => w.ZIndex)
                .ThenBy(w => w.Id, StringComparer.Ordinal)
                .ToList(),
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

using System.Text.Json;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Drops designer content blocks that are reserved for optional data
/// (e.g. text notes) when that data is empty — avoids a trailing page that
/// only repeats header chrome.
/// </summary>
public static class HprpDesignerOmit
{
    /// <summary>
    /// Keep elements that should participate in reflow for the given sample/report data.
    /// When <paramref name="data"/> and <paramref name="boundModel"/> are both null,
    /// everything is kept (authoring without a sample).
    /// </summary>
    public static IReadOnlyList<HprpDesignerElement> FilterForFlow(
        IReadOnlyList<HprpDesignerElement> elements,
        JsonElement? data,
        object? boundModel = null)
    {
        if (elements.Count == 0)
            return elements;

        if (data is null && boundModel is null)
            return elements;

        var kept = new List<HprpDesignerElement>(elements.Count);
        foreach (var e in elements)
        {
            if (!ShouldInclude(e, data, boundModel))
                continue;

            if (IsGroup(e) && e.Children is { Count: > 0 })
            {
                var kids = FilterForFlow(e.Children, data, boundModel);
                if (kids.Count == 0)
                    continue;
                if (kids.Count != e.Children.Count)
                    kept.Add(e.WithBoxAndChildren(e.Box, kids));
                else
                    kept.Add(e);
                continue;
            }

            kept.Add(e);
        }

        return kept;
    }

    public static bool ShouldInclude(
        HprpDesignerElement element,
        JsonElement? data,
        object? boundModel = null)
    {
        if (!string.IsNullOrWhiteSpace(element.OmitWhenEmpty))
        {
            if (data is not null)
                return HprpJsonPath.IsTruthy(HprpJsonPath.Select(data, element.OmitWhenEmpty));

            if (IsChecklistTextNotes(element) && boundModel is Clinical05ProgressNoteChecklistReportViewModel vm)
                return vm.TextNotes.Count > 0;

            // Path declared but no data yet — keep the slot for authoring.
            return true;
        }

        // Fallback for packs that predate omitWhenEmpty.
        if (IsChecklistTextNotes(element))
        {
            if (boundModel is Clinical05ProgressNoteChecklistReportViewModel vm)
                return vm.TextNotes.Count > 0;
            if (data is not null)
                return HprpJsonPath.IsTruthy(HprpJsonPath.Select(data, "$.textNotes"));
        }

        return true;
    }

    private static bool IsChecklistTextNotes(HprpDesignerElement e) =>
        string.Equals(e.Type, HprpDesignerElementTypes.Dense, StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            e.Widget,
            HprpWidgetIds.ClinicalChecklistTextNotes,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsGroup(HprpDesignerElement e) =>
        string.Equals(e.Type, HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase);
}

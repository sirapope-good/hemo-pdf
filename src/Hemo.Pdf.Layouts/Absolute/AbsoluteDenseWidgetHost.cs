using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Absolute;

/// <summary>
/// Dispatches absolute <c>type: dense</c> widgets to existing section composers.
/// Keeps dense pixels in one place while allowing freeform mm placement / chrome overrides.
/// </summary>
public static class AbsoluteDenseWidgetHost
{
    private const float MinMonthRowHeightMm = 12f;
    private const float LayoutSafetyMm = 1.5f;

    private static readonly HctEpoAnnualTableSection AnnualTable = new();
    private static readonly HctEpoCoPayCriteriaSection CoPayCriteria = new();

    /// <summary>Clinical-01 dense widgets available on the absolute canvas (extend per template).</summary>
    public static readonly IReadOnlySet<string> Clinical01WidgetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        HprpWidgetIds.ThaiUrHeader,
        HprpWidgetIds.ClinicalHctEpoAnnualTable,
        HprpWidgetIds.ClinicalHctEpoCopay,
    };

    public static bool TryCompose(
        IContainer container,
        HprpAbsoluteWidget widget,
        AbsoluteCanvasViewModel canvas)
    {
        var widgetId = widget.ResolveDenseWidgetId();
        if (string.IsNullOrWhiteSpace(widgetId))
            return false;

        var node = widget.ToLayoutNode();
        var labels = canvas.Labels;

        if (string.Equals(widgetId, HprpWidgetIds.ThaiUrHeader, StringComparison.OrdinalIgnoreCase))
            return TryComposeHeader(container, canvas);

        if (string.Equals(widgetId, HprpWidgetIds.ClinicalHctEpoAnnualTable, StringComparison.OrdinalIgnoreCase))
            return TryComposeAnnualTable(container, canvas, widget, node, labels);

        if (string.Equals(widgetId, HprpWidgetIds.ClinicalHctEpoCopay, StringComparison.OrdinalIgnoreCase))
            return TryComposeCopay(container, canvas, node, labels);

        return false;
    }

    private static bool TryComposeHeader(IContainer container, AbsoluteCanvasViewModel canvas)
    {
        if (canvas.BoundModel is HctEpoReportViewModel hct)
        {
            ThaiUrReportHeader.Compose(container, hct.Header, hct.Title);
            return true;
        }

        if (canvas.BoundModel is HemosheetReportViewModel header)
        {
            ThaiUrReportHeader.Compose(container, header, canvas.Title);
            return true;
        }

        // Placeholder when sample / adapter not bound yet — still placeable for layout work.
        ThaiUrReportHeader.Compose(container, new HemosheetReportViewModel(), canvas.Title);
        return true;
    }

    private static bool TryComposeAnnualTable(
        IContainer container,
        AbsoluteCanvasViewModel canvas,
        HprpAbsoluteWidget widget,
        HprpLayoutNode node,
        IReadOnlyDictionary<string, string> labels)
    {
        var vm = canvas.BoundModel as HctEpoReportViewModel
            ?? new HctEpoReportViewModel { Title = canvas.Title };

        var monthRowHeightMm = BudgetMonthRowHeightFromBoxMm(widget.HMm);
        AnnualTable.Compose(container, vm, monthRowHeightMm, labels, node);
        return true;
    }

    private static bool TryComposeCopay(
        IContainer container,
        AbsoluteCanvasViewModel canvas,
        HprpLayoutNode node,
        IReadOnlyDictionary<string, string> labels)
    {
        var criteria = canvas.BoundModel is HctEpoReportViewModel hct
            ? hct.CoPayCriteria
            : HctEpoCoPayCriteria.CreateDefault();

        CoPayCriteria.Compose(container, criteria, labels, node);
        return true;
    }

    /// <summary>
    /// Fit 12 month rows into the absolute box height (header bar + rows).
    /// Makes the annual table reusable at any placed size.
    /// </summary>
    public static float BudgetMonthRowHeightFromBoxMm(float boxHeightMm)
    {
        var availableForRowsMm = Math.Max(
            0f,
            boxHeightMm - HemosheetThaiUrStyle.HeaderBarHeightMm - LayoutSafetyMm);
        return Math.Max(availableForRowsMm / 12f, MinMonthRowHeightMm);
    }
}

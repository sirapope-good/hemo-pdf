using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Absolute;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Dense QuestPDF layout for clinical-01. Section <b>order</b> comes from the
/// <c>.hprp</c> package (<c>layout.header</c> + <c>layout.body</c> widgets and
/// optional form <c>type</c> blocks). Pixel drawing of dense widgets stays in
/// dedicated section composers (ThaiUr header, annual table, co-pay).
/// </summary>
public sealed class Clinical01HctEpoComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;

    /// <summary>Room for ~3 handwritten / stacked entries per month (reference form uses ~2).</summary>
    private const float MinMonthRowHeightMm = 12f;

    private const float A4HeightMm = 297f;
    private const float PageNumberFooterMm = 7f;
    private const float LayoutSafetyMm = 1.5f;
    private const float SectionSpacingMm = 2f;

    private readonly HctEpoAnnualTableSection _annualTable = new();
    private readonly HctEpoCoPayCriteriaSection _coPayCriteria = new();
    private readonly IHprpTemplateStore? _templates;

    public Clinical01HctEpoComposer(IHprpTemplateStore? templates = null)
    {
        _templates = templates;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (HctEpoReportViewModel)dataModel;
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);

        // Absolute clinical-01 packs reuse the same dense section composers via mm placement.
        if (package is not null && HprpLayoutModes.IsAbsolute(package.Manifest))
        {
            var absolute = AbsoluteCanvasViewModel.FromPackage(
                package,
                vm,
                HprpLabelResolver.Resolve(_templates, context));
            return AbsoluteCanvasComposer.Compose(absolute, context);
        }

        var page = HprpPageLayout.FromPackage(
            package,
            HprpPageFallback.Uniform(HemosheetThaiUrStyle.PageMarginMm, SectionSpacingMm));
        var monthRowHeightMm = BudgetMonthRowHeightMm(vm.CoPayCriteria, page.Vertical);
        var labels = HprpLabelResolver.Resolve(_templates, context);
        var nodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        return HprpQuestPages.Create(
            page,
            header: null,
            content: c => ComposeContent(c, vm, monthRowHeightMm, labels, nodes, context, page.SpacingMm),
            footer: null);
    }

    private void ComposeContent(
        IContainer container,
        HctEpoReportViewModel vm,
        float monthRowHeightMm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> nodes,
        PdfReportContext context,
        float spacingMm)
    {
        var handlers = new Dictionary<string, Action<IContainer, HprpLayoutNode>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ThaiUrHeader] = (c, _) => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title),
            [HprpWidgetIds.ClinicalHctEpoAnnualTable] = (c, node) =>
                _annualTable.Compose(c, vm, monthRowHeightMm, labels, node),
            [HprpWidgetIds.ClinicalHctEpoCopay] = (c, node) =>
                _coPayCriteria.Compose(c, vm.CoPayCriteria, labels, node),
        };

        container.Column(col =>
        {
            col.Spacing(spacingMm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                nodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    /// <summary>
    /// Divide leftover A4 content height across 12 month rows so the co-pay block
    /// sits flush above the page-number footer.
    /// </summary>
    internal static float BudgetMonthRowHeightMm(HctEpoCoPayCriteria criteria, float verticalMarginMm = -1)
    {
        var margin = verticalMarginMm >= 0 ? verticalMarginMm : 2f * HemosheetThaiUrStyle.PageMarginMm;
        var pageContentMm = A4HeightMm
            - margin
            - PageNumberFooterMm;

        // ShowDateAndHdNo = false → title band + single diagnosis/allergy row.
        var headerMm = HemosheetThaiUrStyle.TitleHeightMm
            + HemosheetThaiUrStyle.MetaRowHeightMm;

        var coPayMm = HctEpoCoPayCriteriaSection.EstimateHeightMm(criteria);
        var tableHeaderMm = HemosheetThaiUrStyle.HeaderBarHeightMm;

        // Two gaps: header↔table and table↔co-pay.
        var availableForRowsMm = pageContentMm
            - headerMm
            - SectionSpacingMm * 2f
            - coPayMm
            - tableHeaderMm
            - LayoutSafetyMm;

        var rowH = availableForRowsMm / 12f;
        return Math.Max(rowH, MinMonthRowHeightMm);
    }
}

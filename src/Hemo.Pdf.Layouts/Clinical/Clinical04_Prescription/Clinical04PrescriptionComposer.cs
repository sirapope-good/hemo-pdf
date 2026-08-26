using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical04_Prescription;

/// <summary>
/// Dense QuestPDF layout for clinical-04: ThaiUr header + equal 50/50 prescription columns
/// that fill remaining A4 height (blank-print friendly).
/// </summary>
public sealed class Clinical04PrescriptionComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float A4HeightMm = 297f;
    private const float PageNumberFooterMm = 7f;
    private const float LayoutSafetyMm = 8f;
    private const float SectionSpacingMm = 2f;
    private const float MinBlockHeightMm = 120f;

    private readonly Clinical04PrescriptionColumnsSection _columns = new();
    private readonly IHprpTemplateStore? _templates;

    public Clinical04PrescriptionComposer(IHprpTemplateStore? templates = null)
    {
        _templates = templates;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (Clinical04PrescriptionReportViewModel)dataModel;
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);
        var page = HprpPageLayout.FromPackage(
            package,
            HprpPageFallback.Uniform(HemosheetThaiUrStyle.PageMarginMm, SectionSpacingMm));
        var bodyNodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical04BodyDefault,
            HprpClinicalWidgetSets.Clinical04BodyAllowed,
            includeHeader: false);
        var columnsChrome = bodyNodes
            .FirstOrDefault(n => string.Equals(
                n.Widget,
                HprpWidgetIds.ClinicalPrescriptionColumns,
                StringComparison.OrdinalIgnoreCase))
            ?.Chrome;
        var tableHeaderMm = HprpChrome.ResolveHeaderHeightMm(
            columnsChrome,
            HemosheetThaiUrStyle.HeaderBarHeightMm);
        var blockHeightMm = BudgetBlockHeightMm(page.Vertical, tableHeaderMm, page.SpacingMm);

        var labels = HprpLabelResolver.Resolve(_templates, context);
        var headerWidget = HprpLayoutPlan.ResolveHeaderWidget(
            package,
            HprpWidgetIds.ThaiUrHeader,
            HprpClinicalWidgetSets.Clinical04HeaderAllowed);

        return HprpQuestPages.Create(
            page,
            header: string.Equals(headerWidget, HprpWidgetIds.ThaiUrHeader, StringComparison.OrdinalIgnoreCase)
                ? c => ComposeRepeatingHeader(c, vm, page.SpacingMm)
                : null,
            content: c => ComposeBody(c, vm, blockHeightMm, labels, bodyNodes, context, page.SpacingMm),
            footer: null);
    }

    private void ComposeBody(
        IContainer container,
        Clinical04PrescriptionReportViewModel vm,
        float blockHeightMm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> bodyNodes,
        PdfReportContext context,
        float spacingMm)
    {
        var handlers = new Dictionary<string, Action<IContainer, HprpLayoutNode>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ClinicalPrescriptionColumns] = (c, node) =>
                _columns.Compose(c, vm, blockHeightMm, labels, node, context),
        };

        container.Column(col =>
        {
            col.Spacing(spacingMm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                bodyNodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    private static void ComposeRepeatingHeader(
        IContainer container,
        Clinical04PrescriptionReportViewModel vm,
        float spacingMm)
    {
        var gap = spacingMm >= 0 ? spacingMm : SectionSpacingMm;
        var box = gap > 0 ? container.PaddingBottom(gap, Mm) : container;
        box.Element(c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));
    }

    /// <summary>
    /// Remaining page height for the two equal columns (header already drawn as page header).
    /// </summary>
    internal static float BudgetBlockHeightMm(
        float verticalMarginMm = -1,
        float tableHeaderMm = -1,
        float sectionSpacingMm = -1)
    {
        var margin = verticalMarginMm >= 0 ? verticalMarginMm : 2f * HemosheetThaiUrStyle.PageMarginMm;
        var pageContentMm = A4HeightMm - margin - PageNumberFooterMm;

        var headerMm = HemosheetThaiUrStyle.TitleHeightMm + HemosheetThaiUrStyle.MetaRowHeightMm;
        var gapMm = sectionSpacingMm >= 0 ? sectionSpacingMm : SectionSpacingMm;
        // tableHeaderMm reserved in section itself; budget is full leftover for the block.
        _ = tableHeaderMm;
        var availableMm = pageContentMm - headerMm - gapMm - LayoutSafetyMm;
        return availableMm >= MinBlockHeightMm ? availableMm : Math.Max(availableMm, 80f);
    }
}

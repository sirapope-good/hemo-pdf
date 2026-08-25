using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Dense QuestPDF layout for clinical-05: repeating header from <c>layout.header</c>,
/// body widgets from <c>layout.body</c> (SOAP table).
/// </summary>
public sealed class Clinical05ProgressNoteComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float A4HeightMm = 297f;
    private const float PageNumberFooterMm = 7f;
    private const float LayoutSafetyMm = 1.5f;
    private const float SectionSpacingMm = 2f;
    private const float MinBlockHeightMm = 90f;

    private readonly Clinical05SoapTableSection _table = new();
    private readonly IHprpTemplateStore? _templates;

    public Clinical05ProgressNoteComposer(IHprpTemplateStore? templates = null)
    {
        _templates = templates;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (Clinical05ProgressNoteReportViewModel)dataModel;
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var rowHeightMm = BudgetRowHeightMm(vm);
        var labels = HprpLabelResolver.Resolve(_templates, context);
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);
        var headerWidget = HprpLayoutPlan.ResolveHeaderWidget(
            package,
            HprpWidgetIds.ThaiUrHeader,
            HprpClinicalWidgetSets.Clinical05HeaderAllowed);
        var bodyNodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical05BodyDefault,
            HprpClinicalWidgetSets.Clinical05BodyAllowed,
            includeHeader: false);

        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = string.Equals(headerWidget, HprpWidgetIds.ThaiUrHeader, StringComparison.OrdinalIgnoreCase)
                ? c => ComposeRepeatingHeader(c, vm)
                : null,
            Content = c => ComposeBody(c, vm, rowHeightMm, labels, bodyNodes, context),
            Footer = null,
        };
    }

    private void ComposeBody(
        IContainer container,
        Clinical05ProgressNoteReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> bodyNodes,
        PdfReportContext context)
    {
        var handlers = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ClinicalSoapTable] = c => _table.Compose(c, vm, rowHeightMm, labels),
        };

        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                bodyNodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    private static void ComposeRepeatingHeader(IContainer container, Clinical05ProgressNoteReportViewModel vm)
    {
        container
            .PaddingBottom(SectionSpacingMm, Mm)
            .Element(c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));
    }

    internal static float BudgetRowHeightMm(Clinical05ProgressNoteReportViewModel vm)
    {
        var pageContentMm = A4HeightMm
            - 2f * HemosheetThaiUrStyle.PageMarginMm
            - PageNumberFooterMm;

        var headerMm = HemosheetThaiUrStyle.TitleHeightMm
            + HemosheetThaiUrStyle.MetaRowHeightMm;
        var tableHeaderMm = HemosheetThaiUrStyle.HeaderBarHeightMm;
        var availableForRowsMm = pageContentMm
            - headerMm
            - SectionSpacingMm
            - tableHeaderMm
            - LayoutSafetyMm;

        return Math.Max(availableForRowsMm / Clinical05SoapTableSection.MinEmptyRows, MinBlockHeightMm);
    }
}

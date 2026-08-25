using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;

/// <summary>
/// Dense QuestPDF layout for clinical-02. Widget <b>order</b> from <c>.hprp</c>;
/// meta band stays glued to <c>clinical.epo-drug-table</c> (not a separate reorderable widget yet).
/// </summary>
public sealed class Clinical02EpoDrugComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float A4HeightMm = 297f;
    private const float PageNumberFooterMm = 7f;
    private const float LayoutSafetyMm = 1.5f;
    private const float SectionSpacingMm = 2f;
    private const float MetaBandMm = 8f;
    private const float MinRowHeightMm = 9f;
    private const int MinEmptyRows = 8;

    private readonly EpoDrugInjectionTableSection _table = new();
    private readonly HctEpoCoPayCriteriaSection _coPayCriteria = new();
    private readonly IHprpTemplateStore? _templates;

    public Clinical02EpoDrugComposer(IHprpTemplateStore? templates = null)
    {
        _templates = templates;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (EpoDrugReportViewModel)dataModel;
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var rowHeightMm = BudgetRowHeightMm(vm);
        var labels = HprpLabelResolver.Resolve(_templates, context);
        var package = HprpLayoutPlan.TryGetPackage(_templates, context);
        var nodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical02DefaultOrder,
            HprpClinicalWidgetSets.Clinical02Allowed);

        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => ComposeContent(c, vm, rowHeightMm, labels, nodes, context),
            Footer = null,
        };
    }

    private void ComposeContent(
        IContainer container,
        EpoDrugReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string> labels,
        IReadOnlyList<HprpLayoutNode> nodes,
        PdfReportContext context)
    {
        var handlers = new Dictionary<string, Action<IContainer>>(StringComparer.OrdinalIgnoreCase)
        {
            [HprpWidgetIds.ThaiUrHeader] = c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title),
            [HprpWidgetIds.ClinicalEpoDrugTable] = c => ComposeTableWithMeta(c, vm, rowHeightMm, labels),
            [HprpWidgetIds.ClinicalHctEpoCopay] = c => _coPayCriteria.Compose(c, vm.CoPayCriteria, labels),
        };

        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm);
            HprpWidgetDispatch.ComposeColumn(
                col,
                nodes,
                handlers,
                node => HprpGenericBlockComposer.TryCreateDrawer(node, context.Data, labels, context));
        });
    }

    private void ComposeTableWithMeta(
        IContainer container,
        EpoDrugReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string> labels)
    {
        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm);
            col.Item().Element(c => ComposeMetaBand(c, vm.Meta, labels));
            col.Item().Element(c => _table.Compose(c, vm, rowHeightMm, labels));
        });
    }

    private static void ComposeMetaBand(
        IContainer container,
        EpoDrugMeta meta,
        IReadOnlyDictionary<string, string> labels)
    {
        container
            .Border(HemosheetThaiUrStyle.BorderWidth)
            .Padding(2f, Mm)
            .Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span(HprpLabels.Get(labels, "month", "เดือน ") + " ").Style(ThaiUrText.Base);
                    t.Span(meta.MonthLabel).Style(ThaiUrText.Bold);
                    t.Span("    " + HprpLabels.Get(labels, "yearBe", "พ.ศ.") + " ").Style(ThaiUrText.Base);
                    t.Span(meta.YearBe > 0 ? meta.YearBe.ToString() : string.Empty).Style(ThaiUrText.Bold);
                });

                row.RelativeItem().Text(t =>
                {
                    t.Span(HprpLabels.Get(labels, "epoName", "ยา EPO") + " ").Style(ThaiUrText.Base);
                    t.Span(meta.EpoName ?? string.Empty).Style(ThaiUrText.Bold);
                });

                row.ConstantItem(42, Mm).AlignRight().Text(t =>
                {
                    t.Span(HprpLabels.Get(labels, "needlesPerWeek", "เข็ม/สัปดาห์") + " ").Style(ThaiUrText.Base);
                    t.Span(meta.NeedlesPerWeek ?? string.Empty).Style(ThaiUrText.Bold);
                });
            });
    }

    internal static float BudgetRowHeightMm(EpoDrugReportViewModel vm)
    {
        var pageContentMm = A4HeightMm
            - 2f * HemosheetThaiUrStyle.PageMarginMm
            - PageNumberFooterMm;

        var headerMm = HemosheetThaiUrStyle.TitleHeightMm
            + HemosheetThaiUrStyle.MetaRowHeightMm;
        var coPayMm = HctEpoCoPayCriteriaSection.EstimateHeightMm(vm.CoPayCriteria);
        var tableHeaderMm = HemosheetThaiUrStyle.HeaderBarHeightMm;
        var rowCount = Math.Max(vm.Rows?.Count ?? 0, MinEmptyRows);

        var availableForRowsMm = pageContentMm
            - headerMm
            - MetaBandMm
            - SectionSpacingMm * 3f
            - coPayMm
            - tableHeaderMm
            - LayoutSafetyMm;

        var rowH = availableForRowsMm / rowCount;
        return Math.Max(rowH, MinRowHeightMm);
    }
}

using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Dense QuestPDF layout for clinical-01: shared ThaiUr header (no Date/HD NO.),
/// annual Hct/EPO table with budgeted month-row height so the co-pay block
/// snaps to the bottom of the page (paper-form look even when empty).
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
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var monthRowHeightMm = BudgetMonthRowHeightMm(vm.CoPayCriteria);
        var labels = HprpLabelResolver.Resolve(_templates, context);

        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => ComposeContent(c, vm, monthRowHeightMm, labels),
            Footer = null,
        };
    }

    private void ComposeContent(
        IContainer container,
        HctEpoReportViewModel vm,
        float monthRowHeightMm,
        IReadOnlyDictionary<string, string> labels)
    {
        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm);

            col.Item().Element(c =>
                ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));

            col.Item().Element(c => _annualTable.Compose(c, vm, monthRowHeightMm, labels));

            col.Item().Element(c => _coPayCriteria.Compose(c, vm.CoPayCriteria, labels));
        });
    }

    /// <summary>
    /// Divide leftover A4 content height across 12 month rows so the co-pay block
    /// sits flush above the page-number footer.
    /// </summary>
    internal static float BudgetMonthRowHeightMm(HctEpoCoPayCriteria criteria)
    {
        var pageContentMm = A4HeightMm
            - 2f * HemosheetThaiUrStyle.PageMarginMm
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

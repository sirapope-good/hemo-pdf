using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Dense QuestPDF layout for clinical-05: ThaiUr header + SOAP table (~2 blocks / page).
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

    public object Compose(object dataModel, PdfReportContext context)
    {
        var vm = (Clinical05ProgressNoteReportViewModel)dataModel;
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var rowHeightMm = BudgetRowHeightMm(vm);

        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => ComposeContent(c, vm, rowHeightMm),
            Footer = null,
        };
    }

    private void ComposeContent(
        IContainer container,
        Clinical05ProgressNoteReportViewModel vm,
        float rowHeightMm)
    {
        container.Column(col =>
        {
            col.Spacing(SectionSpacingMm);
            col.Item().Element(c => ThaiUrReportHeader.Compose(c, vm.Header, vm.Title));
            col.Item().Element(c => _table.Compose(c, vm, rowHeightMm));
        });
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

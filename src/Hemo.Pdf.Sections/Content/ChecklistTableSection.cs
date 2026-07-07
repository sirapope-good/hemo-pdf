using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class ChecklistTableSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not IChecklistSource source || source.Checklist is not { } checklist)
        {
            return;
        }

        if (checklist.Items.Count == 0)
        {
            return;
        }

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(16);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
            });

            if (!string.IsNullOrWhiteSpace(checklist.Title))
            {
                table.Cell().ColumnSpan(3)
                    .Background(PdfSectionMetrics.SectionHeaderBackground)
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(checklist.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            table.Cell().Border(0.5f).Background(PdfSectionMetrics.SectionHeaderBackground).Padding(PdfSectionMetrics.CellPadding)
                .Text("")
                .FontSize(PdfStyleDefaults.Body.DataFontSize);
            table.Cell().Border(0.5f).Background(PdfSectionMetrics.SectionHeaderBackground).Padding(PdfSectionMetrics.CellPadding)
                .Text("รายการ")
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.DataFontSize)
                .SemiBold();
            table.Cell().Border(0.5f).Background(PdfSectionMetrics.SectionHeaderBackground).Padding(PdfSectionMetrics.CellPadding)
                .Text("หมายเหตุ")
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.DataFontSize)
                .SemiBold();

            foreach (var item in checklist.Items)
            {
                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Row(row =>
                {
                    PdfComponentHelpers.RenderCheckbox(row, item.IsChecked, 8f);
                });

                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
                    .Text(item.Label)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize);

                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
                    .Text(string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize);
            }
        });
    }
}

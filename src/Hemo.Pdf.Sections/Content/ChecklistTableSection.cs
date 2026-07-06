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

        container.Column(col =>
        {
            col.Spacing(4);

            if (!string.IsNullOrWhiteSpace(checklist.Title))
            {
                col.Item().Text(checklist.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(20);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Cell().Border(0.5f).Background("#f0f0f0").Padding(4)
                    .Text("")
                    .FontSize(PdfStyleDefaults.Body.DataFontSize);
                table.Cell().Border(0.5f).Background("#f0f0f0").Padding(4)
                    .Text("รายการ")
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize)
                    .SemiBold();
                table.Cell().Border(0.5f).Background("#f0f0f0").Padding(4)
                    .Text("หมายเหตุ")
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize)
                    .SemiBold();

                foreach (var item in checklist.Items)
                {
                    table.Cell().Border(0.5f).Padding(4).Row(row =>
                    {
                        PdfComponentHelpers.RenderCheckbox(row, item.IsChecked, 10f);
                    });

                    table.Cell().Border(0.5f).Padding(4)
                        .Text(item.Label)
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(PdfStyleDefaults.Body.DataFontSize);

                    table.Cell().Border(0.5f).Padding(4)
                        .Text(string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes)
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(PdfStyleDefaults.Body.DataFontSize);
                }
            });
        });
    }
}

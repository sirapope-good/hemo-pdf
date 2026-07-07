using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public interface IKeyValueRowsSource
{
    IReadOnlyList<KeyValuePair<string, string?>> Rows { get; }
    string? SectionTitle { get; }
}

public sealed class KeyValueTableSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        IReadOnlyList<KeyValuePair<string, string?>> rows;
        string? title;

        switch (viewModel)
        {
            case IKeyValueRowsSource source:
                rows = source.Rows;
                title = source.SectionTitle;
                break;
            case SimpleReportViewModel simple:
                rows = simple.Rows;
                title = string.IsNullOrWhiteSpace(simple.Title) ? null : simple.Title;
                break;
            default:
                return;
        }

        if (rows.Count == 0)
        {
            return;
        }

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
            });

            if (!string.IsNullOrWhiteSpace(title))
            {
                table.Cell().ColumnSpan(2)
                    .Background(PdfSectionMetrics.SectionHeaderBackground)
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            foreach (var (key, value) in rows)
            {
                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
                    .Text(key)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize)
                    .SemiBold();

                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding)
                    .Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(PdfStyleDefaults.Body.DataFontSize);
            }
        });
    }
}

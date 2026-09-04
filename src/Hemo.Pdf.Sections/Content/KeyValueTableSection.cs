using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public interface IKeyValueRowsSource
{
    IReadOnlyList<KeyValuePair<string, string?>> Rows { get; }
    string? SectionTitle { get; }
    HprpChrome? Chrome { get; }
}

public sealed class KeyValueTableSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        IReadOnlyList<KeyValuePair<string, string?>> rows;
        string? title;
        HprpChrome? chrome = null;

        switch (viewModel)
        {
            case IKeyValueRowsSource source:
                rows = source.Rows;
                title = source.SectionTitle;
                chrome = source.Chrome;
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

        var border = HprpChrome.ResolveBorderWidth(chrome);
        var headerFill = HprpChrome.ResolveHeaderFill(chrome, context, PdfSectionMetrics.SectionHeaderBackground);
        var fontSize = HprpChrome.ResolveFontSize(chrome, context.DefaultFontSize ?? PdfStyleDefaults.Body.DataFontSize);
        var weights = HprpChrome.ParseColumnWeights(chrome?.ColumnWidths, 2);

        container.Border(border).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(weights.Count == 2 ? weights[0] : 2);
                columns.RelativeColumn(weights.Count == 2 ? weights[1] : 3);
            });

            if (!string.IsNullOrWhiteSpace(title))
            {
                table.Cell().ColumnSpan(2)
                    .Background(headerFill)
                    .Border(border)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            foreach (var (key, value) in rows)
            {
                table.Cell().Border(border).Padding(PdfSectionMetrics.CellPadding)
                    .Text(key)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(fontSize)
                    .SemiBold();

                table.Cell().Border(border).Padding(PdfSectionMetrics.CellPadding)
                    .Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(fontSize);
            }
        });
    }
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Footers;

public sealed class PageNumberFooterSection : IReportFooterSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        container.AlignRight().Text(text =>
        {
            text.CurrentPageNumber()
                .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                .FontSize(PdfStyleDefaults.Footer.TextFontSize);
            text.Span(" / ")
                .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                .FontSize(PdfStyleDefaults.Footer.TextFontSize);
            text.TotalPages()
                .FontFamily(PdfStyleDefaults.Footer.TextFontFamily)
                .FontSize(PdfStyleDefaults.Footer.TextFontSize);
        });
    }
}

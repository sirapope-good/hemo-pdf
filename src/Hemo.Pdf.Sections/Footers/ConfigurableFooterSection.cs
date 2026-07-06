using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Footers;

public sealed class ConfigurableFooterSection : IReportFooterSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var disclaimer = context.Branding?.Footer.DisclaimerText;
        var showPageNumber = context.Branding?.Header.ShowPageNumber ?? true;

        container.Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(disclaimer))
            {
                row.RelativeItem()
                    .AlignLeft()
                    .Text(disclaimer)
                    .FontSize(PdfStyleDefaults.Footer.TextFontSize);
            }
            else
            {
                row.RelativeItem();
            }

            if (showPageNumber)
            {
                row.ConstantItem(80)
                    .AlignRight()
                    .DefaultTextStyle(style => style.FontSize(PdfStyleDefaults.Footer.TextFontSize))
                    .Text(text =>
                    {
                        text.Span("หน้า ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
            }
        });
    }
}

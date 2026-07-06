using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Headers;

public sealed class ConfigurableHeaderSection : IReportHeaderSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var branding = context.Branding;
        var title = context.Metadata.Title;
        if (string.IsNullOrWhiteSpace(title) && branding is not null)
            title = branding.DisplayName;

        container.Column(column =>
        {
            if (branding?.Header.CompanyLines is { Count: > 0 } lines)
            {
                foreach (var line in lines)
                {
                    ApplyAlignment(column.Item(), branding.Header.TitleAlignment)
                        .Text(line)
                        .FontSize(PdfStyleDefaults.Body.BaseFontSize);
                }
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                ApplyAlignment(column.Item().PaddingTop(8), branding?.Header.TitleAlignment ?? HeaderAlignment.Center)
                    .Text(title)
                    .Bold()
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize);
            }

            if (!string.IsNullOrWhiteSpace(context.Metadata.Subtitle))
            {
                ApplyAlignment(column.Item().PaddingTop(4), branding?.Header.TitleAlignment ?? HeaderAlignment.Center)
                    .Text(context.Metadata.Subtitle!)
                    .FontSize(PdfStyleDefaults.Body.BaseFontSize);
            }
        });
    }

    private static IContainer ApplyAlignment(IContainer container, HeaderAlignment alignment) =>
        alignment switch
        {
            HeaderAlignment.Left => container.AlignLeft(),
            HeaderAlignment.Right => container.AlignRight(),
            _ => container.AlignCenter(),
        };
}

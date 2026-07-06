using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview;

public static class HeaderPreviewMapper
{
    public static ReportHeaderBlock Map(PdfReportContext context)
    {
        var branding = context.Branding;
        var title = context.Metadata.Title;
        if (string.IsNullOrWhiteSpace(title) && branding is not null)
        {
            title = branding.DisplayName;
        }

        return new ReportHeaderBlock
        {
            Title = title,
            Subtitle = context.Metadata.Subtitle,
            ReportCode = context.Metadata.ReportCode,
        };
    }
}

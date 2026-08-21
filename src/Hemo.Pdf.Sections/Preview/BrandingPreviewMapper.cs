using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview;

public static class BrandingPreviewMapper
{
    public static ReportBranding Map(PdfReportContext context)
    {
        var branding = context.Branding;
        if (branding is null)
        {
            return new ReportBranding();
        }

        return new ReportBranding
        {
            LogoUrl = branding.Header.LogoUrl,
            CompanyLines = branding.Header.CompanyLines,
            Alignment = MapAlignment(branding.Header.TitleAlignment),
            SectionHeaderBackground = branding.Style.SectionHeaderBackground,
        };
    }

    private static string MapAlignment(HeaderAlignment alignment) =>
        alignment switch
        {
            HeaderAlignment.Left => "left",
            HeaderAlignment.Right => "right",
            _ => "center",
        };
}

using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetBrandingPreviewMapper
{
    public static ReportBranding Map(HemosheetReportViewModel vm, PdfReportContext context)
    {
        var branding = BrandingPreviewMapper.Map(context);
        if (string.IsNullOrWhiteSpace(vm.LogoBase64))
        {
            return branding;
        }

        return new ReportBranding
        {
            LogoUrl = vm.LogoBase64,
            CompanyLines = branding.CompanyLines,
            Alignment = branding.Alignment,
        };
    }
}

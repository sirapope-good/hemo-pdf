using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview;

public static class FooterPreviewMapper
{
    public static ReportFooterBlock Map(PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
        var footer = context.Branding?.Footer;
        var showPageNumber = context.Branding?.Header.ShowPageNumber ?? true;

        if (signatures.Count > 0 || footer?.ShowSignatures == true)
        {
            return new ReportFooterBlock
            {
                Type = "signed",
                Lines = string.IsNullOrWhiteSpace(footer?.DisclaimerText)
                    ? []
                    : [footer!.DisclaimerText!],
                Signatures = SignaturePreviewMapper.MapSlots(signatures),
                PageNumber = showPageNumber ? new PageNumberInfo { Current = 1, Total = 1 } : null,
            };
        }

        return new ReportFooterBlock
        {
            Type = string.IsNullOrWhiteSpace(footer?.DisclaimerText) ? "page-number" : "configurable",
            Lines = string.IsNullOrWhiteSpace(footer?.DisclaimerText)
                ? []
                : [footer!.DisclaimerText!],
            PageNumber = showPageNumber ? new PageNumberInfo { Current = 1, Total = 1 } : null,
        };
    }
}

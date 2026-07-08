using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetFooterPreviewMapper
{
    public static ReportFooterBlock Map(HemosheetReportViewModel vm, PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
        var staffSlots = HemosheetPreviewMappers.MapStaffSignatureSlots(vm);
        var footer = context.Branding?.Footer;
        var showPageNumber = context.Branding?.Header.ShowPageNumber ?? true;
        var lines = new List<string>();

        var nursesLine = HemosheetPreviewMappers.BuildNursesInShiftLine(vm, vm.LayoutContext.Features);
        if (!string.IsNullOrWhiteSpace(nursesLine))
        {
            lines.Add($"พยาบาลเวร: {nursesLine}");
        }

        if (!string.IsNullOrWhiteSpace(footer?.DisclaimerText))
        {
            lines.Add(footer.DisclaimerText!);
        }

        var signatureSlots = SignaturePreviewMapper.MapSlots(signatures).ToList();
        signatureSlots.AddRange(staffSlots);

        return new ReportFooterBlock
        {
            Type = signatureSlots.Count > 0 ? "signed" : "configurable",
            Lines = lines,
            Signatures = signatureSlots,
            PageNumber = showPageNumber ? new PageNumberInfo { Current = 1, Total = 1 } : null,
        };
    }
}

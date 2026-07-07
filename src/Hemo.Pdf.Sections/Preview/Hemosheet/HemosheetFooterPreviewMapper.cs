using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetFooterPreviewMapper
{
    public static ReportFooterBlock Map(HemosheetReportViewModel vm, PdfReportContext context)
    {
        var signatures = context.Signatures?.Signatures ?? [];
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

        return new ReportFooterBlock
        {
            Type = signatures.Count > 0 ? "signed" : "configurable",
            Lines = lines,
            Signatures = SignaturePreviewMapper.MapSlots(signatures),
            PageNumber = showPageNumber ? new PageNumberInfo { Current = 1, Total = 1 } : null,
        };
    }
}

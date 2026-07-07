using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Headers;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetHeaderPreviewMapper
{
    public static ReportHeaderBlock Map(HemosheetReportViewModel vm, PdfReportContext context)
    {
        var title = context.Metadata.Title ?? context.Branding?.DisplayName ?? "Hemosheet";

        return new ReportHeaderBlock
        {
            Title = title,
            ReportCode = context.Metadata.ReportCode,
            MetadataLines = HemosheetHeaderLines.BuildPatientMeta(vm, context)
                .Select(line => $"{line.Label}: {line.Value ?? "—"}")
                .ToList(),
        };
    }
}

using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Sections.Headers;

internal static class HemosheetHeaderLines
{
    internal sealed record HeaderLine(string Label, string? Value);

    internal static IReadOnlyList<HeaderLine> BuildPatientMeta(HemosheetReportViewModel vm, PdfReportContext context)
    {
        var lines = new List<HeaderLine>
        {
            new("ชื่อ-สกุล", vm.Patient.Name),
            new("HN", vm.Patient.Hn),
            new("วันเกิด", FormatDate(vm.Patient.BirthDate)),
            new("เพศ", vm.Patient.Sex),
            new("หน่วย", vm.Unit.FullName),
        };

        if (!string.IsNullOrWhiteSpace(context.Metadata.ReportCode))
        {
            lines.Add(new("รหัสเอกสาร", context.Metadata.ReportCode));
        }

        if (vm.TreatmentNo.HasValue)
        {
            lines.Add(new("Treatment No.", vm.TreatmentNo.Value.ToString()));
        }

        return lines;
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd");
}

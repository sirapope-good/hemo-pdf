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
            new("อายุ", vm.Patient.Age?.ToString()),
            new("เพศ", vm.Patient.Sex),
            new("สิทธิ์", vm.Patient.Coverage),
            new("แพ้ยา", FormatAllergies(vm.Patient.Allergies)),
            new("แพทย์", vm.Patient.DoctorName),
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

    private static string? FormatAllergies(IList<string>? allergies)
    {
        if (allergies is null || allergies.Count == 0)
        {
            return "ไม่มีแพ้ยา";
        }

        return string.Join(", ", allergies);
    }
}

using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Constants;

public static class ReportTemplates
{
    public const string DialysisSession = "template-01-dialysis-session";
    public const string LabResult = "template-02-lab-result";
    public const string Prescription = "template-03-prescription";
    public const string Hemosheet = "template-04-hemosheet";
    public const string NurseRecord = "template-05-nurse-record";
    public const string DoctorRecord = "template-06-doctor-record";
    public const string MedHistory = "template-07-med-history";
    public const string Adequacy = "template-08-adequacy";
    public const string Assessment = "template-09-assessment";
    public const string Admission = "template-10-admission";
    public const string ProgressNote = "template-11-progress-note";
    public const string Summary = "template-12-summary";

    private static readonly IReadOnlyDictionary<string, ReportTemplateDefinition> Definitions =
        new Dictionary<string, ReportTemplateDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [DialysisSession] = new() { Id = DialysisSession, DisplayName = "บันทึกการฟอกไตรายครั้ง", RequiresSignature = true },
            [LabResult] = new() { Id = LabResult, DisplayName = "ผล Lab", RequiresSignature = false },
            [Prescription] = new() { Id = Prescription, DisplayName = "ใบสั่งยา/คำสั่งการรักษา", RequiresSignature = true },
            [Hemosheet] = new() { Id = Hemosheet, DisplayName = "Hemosheet สรุปรอบฟอก", RequiresSignature = true },
            [NurseRecord] = new() { Id = NurseRecord, DisplayName = "บันทึกพยาบาล", RequiresSignature = true },
            [DoctorRecord] = new() { Id = DoctorRecord, DisplayName = "บันทึกแพทย์", RequiresSignature = true },
            [MedHistory] = new() { Id = MedHistory, DisplayName = "ประวัติยา", RequiresSignature = false },
            [Adequacy] = new() { Id = Adequacy, DisplayName = "ค่า Adequacy (Kt/V ฯลฯ)", RequiresSignature = false },
            [Assessment] = new() { Id = Assessment, DisplayName = "แบบประเมิน", RequiresSignature = true },
            [Admission] = new() { Id = Admission, DisplayName = "ข้อมูล Admission", RequiresSignature = false },
            [ProgressNote] = new() { Id = ProgressNote, DisplayName = "Progress Note", RequiresSignature = true },
            [Summary] = new() { Id = Summary, DisplayName = "สรุปรายงานรวม", RequiresSignature = false },
        };

    public static IReadOnlyList<ReportTemplateDefinition> All { get; } = Definitions.Values.ToList();

    public static bool IsKnown(string templateId) =>
        Definitions.ContainsKey(templateId);

    public static bool TryGetDefinition(string templateId, out ReportTemplateDefinition? definition) =>
        Definitions.TryGetValue(templateId, out definition);

    public static bool RequiresSignature(string templateId) =>
        TryGetDefinition(templateId, out var definition) && definition!.RequiresSignature;
}

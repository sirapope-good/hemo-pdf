using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;

/// <summary>Labels for clinical-08/09; Treatment intro matches thaiur-report #08 paper.</summary>
internal sealed class ConsentReportLabels
{
    public required string IAm { get; init; }
    public required string TitleMr { get; init; }
    public required string TitleMrs { get; init; }
    public required string TitleMiss { get; init; }
    public required string TitleMaster { get; init; }
    public required string TitleMissChild { get; init; }
    public required string AgePrefix { get; init; }
    public required string AgeUnit { get; init; }
    public required string AsPatient { get; init; }
    public required string AsRepresentative { get; init; }
    public required string RelationshipPrefix { get; init; }
    public required string OfPatientNamed { get; init; }
    public required string RepresentativeReasonIntro { get; init; }
    public required string ReasonMinor { get; init; }
    public required string ReasonUnconscious { get; init; }
    public required string ReasonOther { get; init; }
    public required string PatientPrefix { get; init; }
    public required string SignPrefix { get; init; }
    public required string RoleSigner { get; init; }
    public required string RoleDoctor { get; init; }
    public required string RoleWitness { get; init; }
    public required string RoleNurse { get; init; }
    public required string PlaceholderName { get; init; }

    public static ConsentReportLabels For(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? En : Th;

    public string SignDateLine(ConsentDateParts p) =>
        ReferenceEquals(this, En)
            ? $"Date {Blank(p.Day)} {Blank(p.Month)} {Blank(p.Year)}"
            : $"วันที่ {Blank(p.Day)} เดือน {Blank(p.Month)} พ.ศ. {Blank(p.Year)}";

    public string SkeletonSignDateLine() =>
        ReferenceEquals(this, En)
            ? "Date ... ... ..."
            : "วันที่ ... เดือน ... พ.ศ. ...";

    public string SkeletonValidityRangeLine() =>
        ReferenceEquals(this, En)
            ? "From ... ... ... to ... ... ..."
            : "วันที่ ... เดือน ... พ.ศ. ... ถึง วันที่ ... เดือน ... พ.ศ. ...";

    public string ValidityNote(int months) =>
        ReferenceEquals(this, En)
            ? $"Note: this document is valid for {months} months"
            : $"หมายเหตุ : เอกสารฉบับนี้มีอายุการใช้งาน {months} เดือน";

    public string ValidityRangeLine(ConsentDateParts? from, ConsentDateParts? to)
    {
        var f = from ?? new ConsentDateParts();
        var t = to ?? new ConsentDateParts();
        return ReferenceEquals(this, En)
            ? $"From {Blank(f.Day)} {Blank(f.Month, "..........")} {Blank(f.Year)} to {Blank(t.Day)} {Blank(t.Month, "..........")} {Blank(t.Year)}"
            : $"วันที่ {Blank(f.Day)} เดือน {Blank(f.Month, "....................")} พ.ศ. {Blank(f.Year, "..............")} ถึง วันที่ {Blank(t.Day)} เดือน {Blank(t.Month, "....................")} พ.ศ. {Blank(t.Year, "..............")}";
    }

    private static string Blank(string? value, string placeholder = "......") =>
        string.IsNullOrWhiteSpace(value) ? placeholder : value!;

    private static readonly ConsentReportLabels Th = new()
    {
        IAm = "ข้าพเจ้า",
        TitleMr = "นาย",
        TitleMrs = "นาง",
        TitleMiss = "นางสาว",
        TitleMaster = "ด.ช.",
        TitleMissChild = "ด.ญ.",
        AgePrefix = "อายุ",
        AgeUnit = "ปี",
        AsPatient = "เป็น ผู้ป่วย",
        AsRepresentative = "ผู้มีอำนาจกระทำการแทนผู้ป่วยในฐานะเกี่ยวข้องเป็น",
        RelationshipPrefix = string.Empty,
        OfPatientNamed = "ของผู้ป่วยชื่อ",
        RepresentativeReasonIntro = "ข้าพเจ้าให้ความยินยอมแทนผู้ป่วยเนื่องจากผู้ป่วย",
        ReasonMinor = "ยังไม่บรรลุนิติภาวะ อายุน้อยกว่า 18 ปี",
        ReasonUnconscious = "สติสัมปชัญญะไม่สมบูรณ์",
        ReasonOther = "หรืออื่น ๆ ระบุ",
        PatientPrefix = "ผู้ป่วย",
        SignPrefix = "ลงชื่อ",
        RoleSigner = "ผู้ป่วย / ผู้มีอำนาจกระทำการแทน",
        RoleDoctor = "แพทย์ผู้ให้ข้อมูล",
        RoleWitness = "พยาน",
        RoleNurse = "พยาบาลไตเทียม",
        PlaceholderName = "...",
    };

    private static readonly ConsentReportLabels En = new()
    {
        IAm = "I,",
        TitleMr = "Mr.",
        TitleMrs = "Mrs.",
        TitleMiss = "Ms.",
        TitleMaster = "Master",
        TitleMissChild = "Miss",
        AgePrefix = "age",
        AgeUnit = "years",
        AsPatient = "am the patient",
        AsRepresentative = "am the authorized representative, related as",
        RelationshipPrefix = string.Empty,
        OfPatientNamed = "of the patient named",
        RepresentativeReasonIntro = "I give consent on behalf of the patient because the patient",
        ReasonMinor = "is still a minor under 18 years of age",
        ReasonUnconscious = "is not in a full state of consciousness",
        ReasonOther = "other (specify)",
        PatientPrefix = "Patient",
        SignPrefix = "Sign",
        RoleSigner = "Patient / Authorized representative",
        RoleDoctor = "Doctor providing information",
        RoleWitness = "Witness",
        RoleNurse = "Hemodialysis nurse",
        PlaceholderName = "...",
    };
}

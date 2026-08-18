using Hemo.Pdf.Core.Hprp;
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
    public required string HeaderTitleTreatment { get; init; }
    public required string HeaderTitlePdpa { get; init; }
    public bool IsEnglish { get; init; }

    public static ConsentReportLabels For(string language, IReadOnlyDictionary<string, string>? overlay = null)
    {
        var isEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        var baseLabels = isEn ? En : Th;
        if (overlay is null || overlay.Count == 0)
            return baseLabels;

        return new ConsentReportLabels
        {
            IAm = HprpLabels.Get(overlay, "iAm", baseLabels.IAm),
            TitleMr = HprpLabels.Get(overlay, "titleMr", baseLabels.TitleMr),
            TitleMrs = HprpLabels.Get(overlay, "titleMrs", baseLabels.TitleMrs),
            TitleMiss = HprpLabels.Get(overlay, "titleMiss", baseLabels.TitleMiss),
            TitleMaster = HprpLabels.Get(overlay, "titleMaster", baseLabels.TitleMaster),
            TitleMissChild = HprpLabels.Get(overlay, "titleMissChild", baseLabels.TitleMissChild),
            AgePrefix = HprpLabels.Get(overlay, "agePrefix", baseLabels.AgePrefix),
            AgeUnit = HprpLabels.Get(overlay, "ageUnit", baseLabels.AgeUnit),
            AsPatient = HprpLabels.Get(overlay, "asPatient", baseLabels.AsPatient),
            AsRepresentative = HprpLabels.Get(overlay, "asRepresentative", baseLabels.AsRepresentative),
            RelationshipPrefix = HprpLabels.Get(overlay, "relationshipPrefix", baseLabels.RelationshipPrefix),
            OfPatientNamed = HprpLabels.Get(overlay, "ofPatientNamed", baseLabels.OfPatientNamed),
            RepresentativeReasonIntro = HprpLabels.Get(overlay, "representativeReasonIntro", baseLabels.RepresentativeReasonIntro),
            ReasonMinor = HprpLabels.Get(overlay, "reasonMinor", baseLabels.ReasonMinor),
            ReasonUnconscious = HprpLabels.Get(overlay, "reasonUnconscious", baseLabels.ReasonUnconscious),
            ReasonOther = HprpLabels.Get(overlay, "reasonOther", baseLabels.ReasonOther),
            PatientPrefix = HprpLabels.Get(overlay, "patientPrefix", baseLabels.PatientPrefix),
            SignPrefix = HprpLabels.Get(overlay, "signPrefix", baseLabels.SignPrefix),
            RoleSigner = HprpLabels.Get(overlay, "roleSigner", baseLabels.RoleSigner),
            RoleDoctor = HprpLabels.Get(overlay, "roleDoctor", baseLabels.RoleDoctor),
            RoleWitness = HprpLabels.Get(overlay, "roleWitness", baseLabels.RoleWitness),
            RoleNurse = HprpLabels.Get(overlay, "roleNurse", baseLabels.RoleNurse),
            PlaceholderName = HprpLabels.Get(overlay, "placeholderName", baseLabels.PlaceholderName),
            HeaderTitleTreatment = HprpLabels.Get(overlay, "headerTitleTreatment", baseLabels.HeaderTitleTreatment),
            HeaderTitlePdpa = HprpLabels.Get(overlay, "headerTitlePdpa", baseLabels.HeaderTitlePdpa),
            IsEnglish = isEn,
        };
    }

    public string HeaderTitle(bool isTreatment) =>
        isTreatment ? HeaderTitleTreatment : HeaderTitlePdpa;

    public string SignDateLine(ConsentDateParts p) =>
        ReferenceEquals(this, En) || IsEnglish
            ? $"Date {Blank(p.Day)} {Blank(p.Month)} {Blank(p.Year)}"
            : $"วันที่ {Blank(p.Day)} เดือน {Blank(p.Month)} พ.ศ. {Blank(p.Year)}";

    public string SkeletonSignDateLine() =>
        ReferenceEquals(this, En) || IsEnglish
            ? "Date ... ... ..."
            : "วันที่ ... เดือน ... พ.ศ. ...";

    public string SkeletonValidityRangeLine() =>
        ReferenceEquals(this, En) || IsEnglish
            ? "From ... ... ... to ... ... ..."
            : "วันที่ ... เดือน ... พ.ศ. ... ถึง วันที่ ... เดือน ... พ.ศ. ...";

    public string ValidityNote(int months) =>
        ReferenceEquals(this, En) || IsEnglish
            ? $"Note: this document is valid for {months} months"
            : $"หมายเหตุ : เอกสารฉบับนี้มีอายุการใช้งาน {months} เดือน";

    public string ValidityRangeLine(ConsentDateParts? from, ConsentDateParts? to)
    {
        var f = from ?? new ConsentDateParts();
        var t = to ?? new ConsentDateParts();
        return IsEnglish || ReferenceEquals(this, En)
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
        HeaderTitleTreatment = "หนังสือแสดงความยินยอม",
        HeaderTitlePdpa = "หนังสือรับทราบ",
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
        HeaderTitleTreatment = "Consent Form",
        HeaderTitlePdpa = "Acknowledgement",
        IsEnglish = true,
    };
}

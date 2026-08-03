namespace Hemo.Pdf.Core.Models.Hemosheet;

public sealed class HemosheetReportViewModel
{
    public Guid Id { get; init; }
    public string? PatientId { get; init; }
    public string? Ward { get; init; }
    public string? Bed { get; init; }
    public int? TreatmentNo { get; init; }
    public DateTime? CycleStartTime { get; init; }
    public DateTime? CycleEndTime { get; init; }
    public DateTime? CompletedTime { get; init; }
    public float? Ktv { get; init; }
    public float? Urr { get; init; }
    public float? Prr { get; init; }
    public float? Recir { get; init; }
    public bool IsAcNotUsed { get; init; }
    public bool IsConsent { get; init; }
    public string? CreatorName { get; init; }
    public string? NursesInShift { get; init; }
    public string? NursesInShiftNonPn { get; init; }
    public string? LogoBase64 { get; init; }
    public string? DoctorSignatureBase64 { get; init; }
    public string? DoctorName { get; init; }

    public HemosheetPatientViewModel Patient { get; init; } = new();
    public HemosheetUnitViewModel Unit { get; init; } = new();
    public HemosheetDehydrationViewModel Dehydration { get; init; } = new();
    public HemosheetPrescriptionViewModel DialysisPrescription { get; init; } = new();
    public HemosheetAvShuntViewModel AvShunt { get; init; } = new();
    public HemosheetAssessmentsViewModel Assessments { get; init; } = new();
    public IList<HemosheetDialysisRecordViewModel> DialysisRecords { get; init; } = [];
    public IList<HemosheetNurseRecordViewModel> NurseRecords { get; init; } = [];
    public IList<HemosheetDoctorRecordViewModel> DoctorRecords { get; init; } = [];
    public IList<HemosheetProgressNoteViewModel> ProgressNotes { get; init; } = [];
    public IList<HemosheetMedicineRecordViewModel> MedicineRecords { get; init; } = [];
    public HemosheetLabsViewModel Labs { get; init; } = new();
    public HemosheetVitalSignViewModel? PreVital { get; init; }
    public HemosheetVitalSignViewModel? PostVital { get; init; }
    public Dictionary<string, string> SignatureNames { get; init; } = new();
    public HemosheetLayoutContextViewModel LayoutContext { get; set; } = new();
}

public sealed class HemosheetPatientViewModel
{
    public string? Name { get; init; }
    public string? Hn { get; init; }
    public string? IdentityNumber { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Sex { get; init; }
    public int? Age { get; init; }
    public string? DoctorName { get; init; }
    public IList<string> Allergies { get; init; } = [];
    public string? Coverage { get; init; }
    public string? Diagnosis { get; init; }
    public string? Underlying { get; init; }
}

public sealed class HemosheetUnitViewModel
{
    public int Id { get; init; }
    public string? FullName { get; init; }
}

public sealed class HemosheetDehydrationViewModel
{
    public float? PreWeight { get; init; }
    public float? PostWeight { get; init; }
    public float? LastPostWeight { get; init; }
    public float? FoodIntakeWeight { get; init; }
    public float? ExtraFluid { get; init; }
    public float? BloodTransfusion { get; init; }
    public float? UfNet { get; init; }
    public float? TotalUf { get; init; }
    public float? UfEstimate { get; init; }
    public float? UfGoal { get; init; }
    public float? FlushNss { get; init; }
    public float? FlushNssTotal { get; init; }
}

public sealed class HemosheetPrescriptionViewModel
{
    public string? Mode { get; init; }
    public string? BloodAccessRoute { get; init; }
    public string? HdfType { get; init; }
    public float? DurationHours { get; init; }
    public float? DurationMinutes { get; init; }
    public string? Anticoagulant { get; init; }
    public float? DryWeight { get; init; }
    public float? BloodFlow { get; init; }
    public string? Dialyzer { get; init; }
    public float? DialyzerSurfaceArea { get; init; }
    public float? DialysateK { get; init; }
    public float? DialysateCa { get; init; }
    public float? DialysateNa { get; init; }
    public float? DialysateHco3 { get; init; }
    public float? DialysateTemperature { get; init; }
    public float? DialysateFlowRate { get; init; }
    public float? InitialAmount { get; init; }
    public float? InitialAmountMl { get; init; }
    public float? MaintainAmount { get; init; }
    public float? MaintainAmountMl { get; init; }
    public float? AcPerSession { get; init; }
    public float? AcPerSessionMl { get; init; }
    public string? ReasonForRefraining { get; init; }
    public string? Note { get; init; }
}

public sealed class HemosheetVitalSignViewModel
{
    public DateTime? Timestamp { get; init; }
    public int? Bps { get; init; }
    public int? Bpd { get; init; }
    public int? Hr { get; init; }
    public int? Rr { get; init; }
    public float? Temp { get; init; }
    public float? SpO2 { get; init; }
    public string? Posture { get; init; }
}

public sealed class HemosheetAvShuntViewModel
{
    public Guid? AvShuntId { get; init; }
    public int? CatheterType { get; init; }
    public string? ShuntSite { get; init; }
    public float? ANeedleSize { get; init; }
    public float? VNeedleSize { get; init; }
    public float? CatheterLength { get; init; }
}

public sealed class HemosheetAssessmentItemViewModel
{
    public string? Name { get; init; }
    public bool Checked { get; init; }
    public string? Text { get; init; }
    public IList<string> SelectedOptions { get; init; } = [];
}

public sealed class HemosheetAssessmentsViewModel
{
    public IList<HemosheetAssessmentItemViewModel> Pre { get; init; } = [];
    public IList<HemosheetAssessmentItemViewModel> Re { get; init; } = [];
    public IList<HemosheetAssessmentItemViewModel> Post { get; init; } = [];
    public IList<HemosheetAssessmentItemViewModel> Other { get; init; } = [];
}

public sealed class HemosheetDialysisRecordViewModel
{
    public DateTime? Timestamp { get; init; }
    public int? Bps { get; init; }
    public int? Bpd { get; init; }
    public int? Hr { get; init; }
    public int? Rr { get; init; }
    public float? Bfr { get; init; }
    public float? Vp { get; init; }
    public float? Tmp { get; init; }
    public float? Dc { get; init; }
    public float? Nss { get; init; }
    public float? UfRate { get; init; }
    public float? HdfVolume { get; init; }
    public float? UfTotal { get; init; }
    public string? Note { get; init; }
}

public sealed class HemosheetNurseRecordViewModel
{
    public DateTime? Timestamp { get; init; }
    public string? Content { get; init; }
}

public sealed class HemosheetDoctorRecordViewModel
{
    public DateTime? Timestamp { get; init; }
    public string? Content { get; init; }
}

public sealed class HemosheetProgressNoteViewModel
{
    public string? A { get; init; }
    public string? I { get; init; }
    public string? E { get; init; }
}

public sealed class HemosheetMedicineRecordViewModel
{
    public DateTime? Timestamp { get; init; }
    public string? MedicineName { get; init; }
    public string? Route { get; init; }
    public float? Quantity { get; init; }
}

public sealed class HemosheetLabsViewModel
{
    public string? Hct { get; init; }
    public string? Hb { get; init; }
    public string? Plt { get; init; }
    public string? Wbc { get; init; }
    public string? Na { get; init; }
    public string? K { get; init; }
    public string? Cl { get; init; }
    public string? Co2 { get; init; }
    public string? Bun { get; init; }
    public string? Cr { get; init; }
    public string? Alb { get; init; }
    public string? Ca { get; init; }
    public string? P { get; init; }
    public string? Mg { get; init; }
    public string? Hbsag { get; init; }
    public string? AntiHcv { get; init; }
    public string? AntiHiv { get; init; }
}

public enum HemosheetLayoutProfile
{
    Default = 0,
    Rama = 1,
    ThaiUr = 2,
}

public enum VascularAccessKind
{
    Unknown = 0,
    AvFistula = 1,
    PermCath = 2,
}

public sealed class HemosheetLayoutContextViewModel
{
    public HemosheetLayoutProfile LayoutProfile { get; init; } = HemosheetLayoutProfile.Default;
    public string DialysisMode { get; init; } = "HD";
    public VascularAccessKind VascularAccess { get; init; } = VascularAccessKind.Unknown;
    public HemosheetReportSettingsViewModel ReportSettings { get; init; } = new();
    public Dictionary<string, bool> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class HemosheetReportSettingsViewModel
{
    public string? HemosheetTemplate { get; init; }
    public bool NurseInShiftEnabled { get; init; }
    public HemosheetFixedLinesViewModel FixedLines { get; init; } = new();
}

public sealed class HemosheetFixedLinesViewModel
{
    public int Dialysis { get; init; } = 8;
    public int Nurse { get; init; } = 4;
    public int Medicine { get; init; } = 4;
    public int Doctor { get; init; } = 2;
    public int ProgressNote { get; init; } = 2;
}

public enum HemosheetSectionId
{
    Patient,
    SubHeaderBar,
    SessionMeta,
    Predialysis,
    Dehydration,
    Prescription,
    VascularAccess,
    AssessmentPre,
    AssessmentRe,
    /// <summary>Default-profile Topic | Pre Y/N | Re Y/N table (Telerik AssessmentTable).</summary>
    AssessmentPreRe,
    AssessmentPost,
    AssessmentOther,
    NursingCarePlan,
    DialysisRecords,
    UfSummary,
    NurseRecords,
    DoctorRecords,
    MedicineRecords,
    ProgressNotes,
    FooterChecklists,
    PrePostHdNotes,
    PostVitals,
    AvfAssessment,
    NursesInShift,
    Consent,
    Labs,
    Signatures,
}

public sealed class HemosheetSectionPlan
{
    public HemosheetSectionId SectionId { get; init; }
    public string? Variant { get; init; }
    public IReadOnlyList<string> VisibleColumns { get; init; } = [];
    public int FixedLineCount { get; init; }
}

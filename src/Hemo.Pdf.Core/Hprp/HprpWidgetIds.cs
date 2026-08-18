using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Named engine widgets. New visual primitives still require a C# rebuild;
/// <c>.hprp</c> files may only reference ids in this catalog.
/// </summary>
public static class HprpWidgetIds
{
    public const string ThaiUrHeader = "thaiur.header";
    public const string ClinicalHctEpoAnnualTable = "clinical.hct-epo-annual-table";
    public const string ClinicalHctEpoCopay = "clinical.hct-epo-copay";
    public const string ClinicalEpoDrugTable = "clinical.epo-drug-table";
    public const string ClinicalSoapTable = "clinical.soap-table";
    public const string ClinicalConsentNarrative = "clinical.consent-narrative";

    public const string HemosheetSubHeaderBar = "hemosheet.sub-header-bar";
    public const string HemosheetSessionMeta = "hemosheet.session-meta";
    public const string HemosheetPredialysis = "hemosheet.predialysis";
    public const string HemosheetVascularAccess = "hemosheet.vascular-access";
    public const string HemosheetAssessmentPreRe = "hemosheet.assessment-pre-re";
    public const string HemosheetAssessmentRe = "hemosheet.assessment-re";
    public const string HemosheetAssessmentPost = "hemosheet.assessment-post";
    public const string HemosheetNursingCarePlan = "hemosheet.nursing-care-plan";
    public const string HemosheetAssessmentOther = "hemosheet.assessment-other";
    public const string HemosheetLabs = "hemosheet.labs";
    public const string HemosheetDialysisRecords = "hemosheet.dialysis-records";
    public const string HemosheetUfSummary = "hemosheet.uf-summary";
    public const string HemosheetNurseRecords = "hemosheet.nurse-records";
    public const string HemosheetDoctorRecords = "hemosheet.doctor-records";
    public const string HemosheetMedicineRecords = "hemosheet.medicine-records";
    public const string HemosheetProgressNotes = "hemosheet.progress-notes";
    public const string HemosheetFooterChecklists = "hemosheet.footer-checklists";
    public const string HemosheetPrePostHdNotes = "hemosheet.pre-post-hd-notes";
    public const string HemosheetPostVitals = "hemosheet.post-vitals";
    public const string HemosheetAvfAssessment = "hemosheet.avf-assessment";
    public const string HemosheetConsent = "hemosheet.consent";

    public static readonly IReadOnlySet<string> BlockTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "field-grid",
        "key-value-table",
        "data-grid",
        "text",
        "signature",
        "patient-info",
    };

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ThaiUrHeader,
        ClinicalHctEpoAnnualTable,
        ClinicalHctEpoCopay,
        ClinicalEpoDrugTable,
        ClinicalSoapTable,
        ClinicalConsentNarrative,
        HemosheetSubHeaderBar,
        HemosheetSessionMeta,
        HemosheetPredialysis,
        HemosheetVascularAccess,
        HemosheetAssessmentPreRe,
        HemosheetAssessmentRe,
        HemosheetAssessmentPost,
        HemosheetNursingCarePlan,
        HemosheetAssessmentOther,
        HemosheetLabs,
        HemosheetDialysisRecords,
        HemosheetUfSummary,
        HemosheetNurseRecords,
        HemosheetDoctorRecords,
        HemosheetMedicineRecords,
        HemosheetProgressNotes,
        HemosheetFooterChecklists,
        HemosheetPrePostHdNotes,
        HemosheetPostVitals,
        HemosheetAvfAssessment,
        HemosheetConsent,
    };

    public static bool TryMapHemosheetSection(string widgetId, out HemosheetSectionId sectionId)
    {
        sectionId = widgetId.ToLowerInvariant() switch
        {
            HemosheetSubHeaderBar => HemosheetSectionId.SubHeaderBar,
            HemosheetSessionMeta => HemosheetSectionId.SessionMeta,
            HemosheetPredialysis => HemosheetSectionId.Predialysis,
            HemosheetVascularAccess => HemosheetSectionId.VascularAccess,
            HemosheetAssessmentPreRe => HemosheetSectionId.AssessmentPreRe,
            HemosheetAssessmentRe => HemosheetSectionId.AssessmentRe,
            HemosheetAssessmentPost => HemosheetSectionId.AssessmentPost,
            HemosheetNursingCarePlan => HemosheetSectionId.NursingCarePlan,
            HemosheetAssessmentOther => HemosheetSectionId.AssessmentOther,
            HemosheetLabs => HemosheetSectionId.Labs,
            HemosheetDialysisRecords => HemosheetSectionId.DialysisRecords,
            HemosheetUfSummary => HemosheetSectionId.UfSummary,
            HemosheetNurseRecords => HemosheetSectionId.NurseRecords,
            HemosheetDoctorRecords => HemosheetSectionId.DoctorRecords,
            HemosheetMedicineRecords => HemosheetSectionId.MedicineRecords,
            HemosheetProgressNotes => HemosheetSectionId.ProgressNotes,
            HemosheetFooterChecklists => HemosheetSectionId.FooterChecklists,
            HemosheetPrePostHdNotes => HemosheetSectionId.PrePostHdNotes,
            HemosheetPostVitals => HemosheetSectionId.PostVitals,
            HemosheetAvfAssessment => HemosheetSectionId.AvfAssessment,
            HemosheetConsent => HemosheetSectionId.Consent,
            _ => (HemosheetSectionId)(-1),
        };

        return (int)sectionId >= 0;
    }
}

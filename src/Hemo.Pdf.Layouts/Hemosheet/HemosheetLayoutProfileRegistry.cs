using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public sealed class HemosheetLayoutProfileRegistry
{
    private static readonly HemosheetSectionId[] CoreSections =
    [
        HemosheetSectionId.Patient,
        HemosheetSectionId.SessionMeta,
        HemosheetSectionId.Dehydration,
        HemosheetSectionId.Prescription,
        HemosheetSectionId.VascularAccess,
        HemosheetSectionId.AssessmentPre,
        HemosheetSectionId.AssessmentRe,
        HemosheetSectionId.AssessmentPost,
        HemosheetSectionId.AssessmentOther,
        HemosheetSectionId.Labs,
        HemosheetSectionId.DialysisRecords,
        HemosheetSectionId.NurseRecords,
        HemosheetSectionId.DoctorRecords,
        HemosheetSectionId.MedicineRecords,
        HemosheetSectionId.ProgressNotes,
        HemosheetSectionId.NursesInShift,
        HemosheetSectionId.Consent,
        HemosheetSectionId.Signatures,
    ];

    public IReadOnlyList<HemosheetSectionId> GetSectionOrder(HemosheetLayoutProfile profile) =>
        profile switch
        {
            HemosheetLayoutProfile.ThaiUr => CoreSections,
            HemosheetLayoutProfile.Rama => CoreSections,
            _ => CoreSections,
        };

    public bool IsProfileSection(HemosheetSectionId sectionId, HemosheetLayoutProfile profile) =>
        sectionId switch
        {
            HemosheetSectionId.Consent => profile == HemosheetLayoutProfile.Rama,
            _ => true,
        };
}

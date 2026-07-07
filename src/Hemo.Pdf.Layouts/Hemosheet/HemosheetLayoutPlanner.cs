using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public interface IHemosheetLayoutPlanner
{
    IReadOnlyList<HemosheetSectionPlan> Plan(HemosheetReportViewModel viewModel);
}

public sealed class HemosheetLayoutPlanner : IHemosheetLayoutPlanner
{
    private static readonly string[] BaseDialysisColumns =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP", "TMP", "DC", "NSS", "UF Rate", "หมายเหตุ",
    ];

    private static readonly string[] HdfDialysisColumns =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP", "TMP", "DC", "NSS", "UF Rate", "HDF Vol.", "หมายเหตุ",
    ];

    public IReadOnlyList<HemosheetSectionPlan> Plan(HemosheetReportViewModel viewModel)
    {
        var features = viewModel.LayoutContext.Features;
        var settings = viewModel.LayoutContext.ReportSettings;
        var profile = viewModel.LayoutContext.LayoutProfile;
        var plans = new List<HemosheetSectionPlan>
        {
            new() { SectionId = HemosheetSectionId.Patient },
            new() { SectionId = HemosheetSectionId.SessionMeta },
            new() { SectionId = HemosheetSectionId.Dehydration },
            new() { SectionId = HemosheetSectionId.Prescription },
        };

        if (IsEnabled(features, "showAvPanel"))
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.VascularAccess,
                Variant = "av-fistula",
            });
        }
        else if (IsEnabled(features, "showCathPanel"))
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.VascularAccess,
                Variant = "perm-cath",
            });
        }

        if (viewModel.Assessments.Pre.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentPre });
        }

        if (viewModel.Assessments.Re.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentRe });
        }

        if (viewModel.Assessments.Post.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentPost });
        }

        if (viewModel.Assessments.Other.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentOther });
        }

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.DialysisRecords,
            VisibleColumns = IsEnabled(features, "showHdfColumns") ? HdfDialysisColumns : BaseDialysisColumns,
            FixedLineCount = settings.FixedLines.Dialysis,
        });

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.NurseRecords,
            FixedLineCount = settings.FixedLines.Nurse,
        });

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.DoctorRecords,
            FixedLineCount = settings.FixedLines.Doctor,
        });

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.MedicineRecords,
            FixedLineCount = settings.FixedLines.Medicine,
        });

        if (IsEnabled(features, "showProgressNote") || viewModel.ProgressNotes.Count > 0)
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.ProgressNotes,
                FixedLineCount = settings.FixedLines.ProgressNote,
            });
        }

        if (IsEnabled(features, "showNurseInShift"))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.NursesInShift });
        }

        if (profile == HemosheetLayoutProfile.Rama
            && IsEnabled(features, "showConsentBlock"))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.Consent });
        }

        plans.Add(new() { SectionId = HemosheetSectionId.Signatures });
        return plans;
    }

    private static bool IsEnabled(IReadOnlyDictionary<string, bool> features, string key) =>
        features.TryGetValue(key, out var enabled) && enabled;
}

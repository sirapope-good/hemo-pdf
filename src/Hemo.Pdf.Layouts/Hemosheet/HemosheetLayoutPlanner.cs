using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public sealed class HemosheetLayoutPlanner : IHemosheetLayoutPlanner
{
    private static readonly string[] BaseDialysisColumns =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP", "TMP", "DC", "NSS", "UF Rate", "Total", "หมายเหตุ",
    ];

    private static readonly string[] HdfDialysisColumns =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP", "TMP", "DC", "NSS", "UF Rate", "HDF Vol.", "Total", "หมายเหตุ",
    ];

    private readonly HemosheetLayoutProfileRegistry _profileRegistry;

    public HemosheetLayoutPlanner(HemosheetLayoutProfileRegistry profileRegistry)
    {
        _profileRegistry = profileRegistry;
    }

    public IReadOnlyList<HemosheetSectionPlan> Plan(HemosheetReportViewModel viewModel)
    {
        var features = viewModel.LayoutContext.Features;
        var settings = viewModel.LayoutContext.ReportSettings;
        var profile = viewModel.LayoutContext.LayoutProfile;
        var plans = new List<HemosheetSectionPlan>();

        if (HasSubHeader(viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.SubHeaderBar });
        }

        plans.Add(new() { SectionId = HemosheetSectionId.SessionMeta });
        plans.Add(new() { SectionId = HemosheetSectionId.Predialysis });

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

        if (viewModel.Assessments.Re.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentRe });
        }

        if (viewModel.Assessments.Post.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentPost });
        }

        if (HasNursingCarePlan(viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.NursingCarePlan });
        }

        if (viewModel.Assessments.Other.Count > 0)
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentOther });
        }

        if (HasLabData(viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.Labs });
        }

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.DialysisRecords,
            VisibleColumns = IsEnabled(features, "showHdfColumns") ? HdfDialysisColumns : BaseDialysisColumns,
            FixedLineCount = settings.FixedLines.Dialysis,
        });

        plans.Add(new() { SectionId = HemosheetSectionId.UfSummary });

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

        if (HasFooterChecklists(viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.FooterChecklists });
        }

        plans.Add(new() { SectionId = HemosheetSectionId.PrePostHdNotes });
        plans.Add(new() { SectionId = HemosheetSectionId.PostVitals });
        plans.Add(new() { SectionId = HemosheetSectionId.AvfAssessment });

        if (profile == HemosheetLayoutProfile.Rama
            && IsEnabled(features, "showConsentBlock")
            && _profileRegistry.IsProfileSection(HemosheetSectionId.Consent, profile))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.Consent });
        }

        return plans;
    }

    private static bool HasSubHeader(HemosheetReportViewModel viewModel) =>
        !string.IsNullOrWhiteSpace(viewModel.Patient.Diagnosis)
        || viewModel.Patient.Allergies.Count > 0;

    private static bool HasNursingCarePlan(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Other.Any(i =>
            string.Equals(i.Name, "nursing_diagnosis", StringComparison.OrdinalIgnoreCase)
            || string.Equals(i.Name, "nursing_intervention", StringComparison.OrdinalIgnoreCase)
            || string.Equals(i.Name, "expected_outcomes", StringComparison.OrdinalIgnoreCase));

    private static bool HasFooterChecklists(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Post.Any(i => i.Name?.StartsWith("complication.", StringComparison.OrdinalIgnoreCase) == true
            || i.Name?.StartsWith("nursing.", StringComparison.OrdinalIgnoreCase) == true
            || i.Name?.StartsWith("health.", StringComparison.OrdinalIgnoreCase) == true)
        || viewModel.Assessments.Other.Any(i => i.Name?.StartsWith("medication.", StringComparison.OrdinalIgnoreCase) == true);

    private static bool HasLabData(HemosheetReportViewModel viewModel)
    {
        var labs = viewModel.Labs;
        return !string.IsNullOrWhiteSpace(labs.Hct)
            || !string.IsNullOrWhiteSpace(labs.Hb)
            || !string.IsNullOrWhiteSpace(labs.Plt)
            || !string.IsNullOrWhiteSpace(labs.Wbc)
            || !string.IsNullOrWhiteSpace(labs.Na)
            || !string.IsNullOrWhiteSpace(labs.K)
            || !string.IsNullOrWhiteSpace(labs.Cl)
            || !string.IsNullOrWhiteSpace(labs.Co2)
            || !string.IsNullOrWhiteSpace(labs.Bun)
            || !string.IsNullOrWhiteSpace(labs.Cr)
            || !string.IsNullOrWhiteSpace(labs.Alb)
            || !string.IsNullOrWhiteSpace(labs.Ca)
            || !string.IsNullOrWhiteSpace(labs.P)
            || !string.IsNullOrWhiteSpace(labs.Mg)
            || !string.IsNullOrWhiteSpace(labs.Hbsag)
            || !string.IsNullOrWhiteSpace(labs.AntiHcv)
            || !string.IsNullOrWhiteSpace(labs.AntiHiv);
    }

    private static bool IsEnabled(IReadOnlyDictionary<string, bool> features, string key) =>
        features.TryGetValue(key, out var enabled) && enabled;
}

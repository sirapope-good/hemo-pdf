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
        var plans = new List<HemosheetSectionPlan>
        {
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

        if (profile == HemosheetLayoutProfile.Rama
            && IsEnabled(features, "showConsentBlock")
            && _profileRegistry.IsProfileSection(HemosheetSectionId.Consent, profile))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.Consent });
        }

        return plans;
    }

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

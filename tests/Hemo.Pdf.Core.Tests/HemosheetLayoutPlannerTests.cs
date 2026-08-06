using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetLayoutPlannerTests
{
    private readonly HemosheetLayoutPlanner _planner = new(new HemosheetLayoutProfileRegistry());

    [Fact]
    public void Plan_HdAv_IncludesAvPanel_And_BaseDialysisColumns()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = true,
                ["showCathPanel"] = false,
                ["showHdfColumns"] = false,
            });

        var plans = _planner.Plan(vm);

        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.VascularAccess && p.Variant == "av-fistula");
        var dialysis = plans.Single(p => p.SectionId == HemosheetSectionId.DialysisRecords);
        Assert.DoesNotContain("Substitute total", dialysis.VisibleColumns);
        Assert.DoesNotContain("Substitute rate", dialysis.VisibleColumns);
    }

    [Fact]
    public void Plan_Default_IncludesParitySections()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = true,
                ["showNurseInShift"] = true,
            },
            patient: new HemosheetPatientViewModel { Diagnosis = "CKD stage 5" },
            preVital: new HemosheetVitalSignViewModel { Bps = 130, Bpd = 80 });

        var plans = _planner.Plan(vm);

        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.SubHeaderBar);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.Predialysis);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.UfSummary);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.PrePostHdNotes);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.PostVitals);
    }

    [Fact]
    public void Plan_Hdf_IncludesHdfColumnsAndTotal()
    {
        var vm = CreateViewModel(
            mode: "HDF",
            catheterType: 0,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = true,
                ["showHdfColumns"] = true,
            });

        var dialysis = _planner.Plan(vm).Single(p => p.SectionId == HemosheetSectionId.DialysisRecords);
        Assert.Contains("Substitute total", dialysis.VisibleColumns);
        Assert.Contains("Substitute rate", dialysis.VisibleColumns);
        Assert.Contains("Total", dialysis.VisibleColumns);
    }

    [Fact]
    public void Plan_PermCath_UsesCathVariant()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 3,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = false,
                ["showCathPanel"] = true,
            });

        var vascular = _planner.Plan(vm).Single(p => p.SectionId == HemosheetSectionId.VascularAccess);
        Assert.Equal("perm-cath", vascular.Variant);
    }

    [Fact]
    public void Plan_Rama_IncludesConsent_WhenEnabled()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = true,
                ["showConsentBlock"] = true,
            },
            layoutProfile: HemosheetLayoutProfile.Rama,
            isConsent: true);

        var plans = _planner.Plan(vm);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.Consent);
    }

    [Fact]
    public void Plan_Default_ExcludesConsent()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool>
            {
                ["showAvPanel"] = true,
                ["showConsentBlock"] = true,
            },
            isConsent: true);

        var plans = _planner.Plan(vm);
        Assert.DoesNotContain(plans, p => p.SectionId == HemosheetSectionId.Consent);
    }

    [Fact]
    public void Plan_IncludesLabs_WhenLabDataPresent()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool> { ["showAvPanel"] = true },
            labs: new HemosheetLabsViewModel { Hb = "12.5" });

        var plans = _planner.Plan(vm);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.Labs);
    }

    [Fact]
    public void Plan_Default_UsesPreReMatrix_NotSeparateRe()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool> { ["showAvPanel"] = true },
            assessments: new HemosheetAssessmentsViewModel
            {
                Pre = [new() { Name = "pain", Checked = true }],
                Re = [new() { Name = "pain", Checked = false }],
            });

        var plans = _planner.Plan(vm);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.AssessmentPreRe);
        Assert.DoesNotContain(plans, p => p.SectionId == HemosheetSectionId.AssessmentRe);
    }

    [Fact]
    public void Plan_Default_FooterFromParentSelectedOptions()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool> { ["showAvPanel"] = true },
            assessments: new HemosheetAssessmentsViewModel
            {
                Post =
                [
                    new()
                    {
                        Name = "complication",
                        Checked = true,
                        SelectedOptions = ["Hypo-tension"],
                    },
                ],
            });

        var plans = _planner.Plan(vm);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.FooterChecklists);
        Assert.DoesNotContain(plans, p => p.SectionId == HemosheetSectionId.AssessmentPost);
    }

    [Fact]
    public void Plan_ThaiUr_SkipsPreReMatrix()
    {
        var vm = CreateViewModel(
            mode: "HD",
            catheterType: 0,
            features: new Dictionary<string, bool> { ["showAvPanel"] = true },
            layoutProfile: HemosheetLayoutProfile.ThaiUr,
            assessments: new HemosheetAssessmentsViewModel
            {
                Pre = [new() { Name = "pain", Checked = true }],
                Re = [new() { Name = "pain", Checked = false }],
            });

        var plans = _planner.Plan(vm);
        Assert.DoesNotContain(plans, p => p.SectionId == HemosheetSectionId.AssessmentPreRe);
        Assert.Contains(plans, p => p.SectionId == HemosheetSectionId.AssessmentRe);
    }

    private static HemosheetReportViewModel CreateViewModel(
        string mode,
        int catheterType,
        IReadOnlyDictionary<string, bool> features,
        HemosheetLayoutProfile layoutProfile = HemosheetLayoutProfile.Default,
        bool isConsent = false,
        HemosheetLabsViewModel? labs = null,
        HemosheetPatientViewModel? patient = null,
        HemosheetVitalSignViewModel? preVital = null,
        HemosheetAssessmentsViewModel? assessments = null)
    {
        return new HemosheetReportViewModel
        {
            IsConsent = isConsent,
            Patient = patient ?? new HemosheetPatientViewModel(),
            PreVital = preVital,
            Labs = labs ?? new HemosheetLabsViewModel(),
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = mode },
            AvShunt = new HemosheetAvShuntViewModel { CatheterType = catheterType },
            Assessments = assessments ?? new HemosheetAssessmentsViewModel
            {
                Pre = [new HemosheetAssessmentItemViewModel { Name = "pain", Checked = true }],
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = layoutProfile,
                DialysisMode = mode,
                Features = new Dictionary<string, bool>(features),
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel(),
                },
            },
        };
    }
}

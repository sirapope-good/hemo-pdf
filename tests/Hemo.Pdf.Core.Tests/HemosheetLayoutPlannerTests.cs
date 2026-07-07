using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetLayoutPlannerTests
{
    private readonly HemosheetLayoutPlanner _planner = new();

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
        Assert.DoesNotContain("HDF Vol.", dialysis.VisibleColumns);
    }

    [Fact]
    public void Plan_Hdf_IncludesHdfColumns()
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
        Assert.Contains("HDF Vol.", dialysis.VisibleColumns);
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

    private static HemosheetReportViewModel CreateViewModel(
        string mode,
        int catheterType,
        IReadOnlyDictionary<string, bool> features)
    {
        return new HemosheetReportViewModel
        {
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = mode },
            AvShunt = new HemosheetAvShuntViewModel { CatheterType = catheterType },
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new HemosheetAssessmentItemViewModel { Name = "pain", Checked = true }],
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
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

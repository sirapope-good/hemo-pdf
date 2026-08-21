using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public sealed class HemosheetLayoutPlanner : IHemosheetLayoutPlanner
{
    private readonly HemosheetLayoutProfileRegistry _profileRegistry;
    private readonly IHprpTemplateStore? _templates;
    private readonly ITenantContextAccessor? _tenant;

    public HemosheetLayoutPlanner(HemosheetLayoutProfileRegistry profileRegistry)
        : this(profileRegistry, null, null)
    {
    }

    public HemosheetLayoutPlanner(
        HemosheetLayoutProfileRegistry profileRegistry,
        IHprpTemplateStore? templates,
        ITenantContextAccessor? tenant)
    {
        _profileRegistry = profileRegistry;
        _templates = templates;
        _tenant = tenant;
    }

    public IReadOnlyList<HemosheetSectionPlan> Plan(HemosheetReportViewModel viewModel)
    {
        var tenantCode = _tenant?.TenantCode ?? "";
        var variant = HprpTemplatePaths.FromLayoutProfile(viewModel.LayoutContext.LayoutProfile);
        var package = _templates?.TryGetCached(
            tenantCode,
            ClinicalReportCatalog.HemodialysisRecord,
            variant);
        if (package is not null && package.Layout.Sections.Count > 0)
        {
            var interpreted = Hprp.HprpHemosheetPlanInterpreter.Interpret(package.Layout, viewModel);
            return FilterProfileSections(interpreted, viewModel.LayoutContext.LayoutProfile);
        }

        return PlanBuiltin(viewModel);
    }

    private IReadOnlyList<HemosheetSectionPlan> FilterProfileSections(
        IReadOnlyList<HemosheetSectionPlan> plans,
        HemosheetLayoutProfile profile)
    {
        return plans
            .Where(p => _profileRegistry.IsProfileSection(p.SectionId, profile))
            .ToList();
    }

    internal IReadOnlyList<HemosheetSectionPlan> PlanBuiltin(HemosheetReportViewModel viewModel)
    {
        var features = viewModel.LayoutContext.Features;
        var settings = viewModel.LayoutContext.ReportSettings;
        var profile = viewModel.LayoutContext.LayoutProfile;
        var plans = new List<HemosheetSectionPlan>();

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasSubHeader", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.SubHeaderBar });

        plans.Add(new() { SectionId = HemosheetSectionId.Patient });
        plans.Add(new() { SectionId = HemosheetSectionId.SessionMeta });
        plans.Add(new() { SectionId = HemosheetSectionId.Predialysis });

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("feature:showAvPanel", viewModel))
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.VascularAccess,
                Variant = "av-fistula",
            });
        }
        else if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("feature:showCathPanel", viewModel))
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.VascularAccess,
                Variant = "perm-cath",
            });
        }

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("not-profile:ThaiUr", viewModel)
            && Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasAssessmentPreOrRe", viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentPreRe });
        }
        else if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("profile:ThaiUr", viewModel)
            && Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasAssessmentRe", viewModel))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentRe });
        }

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasPostAssessmentBody", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentPost });

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasNursingCarePlan", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.NursingCarePlan });

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasOtherAssessmentBody", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.AssessmentOther });

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasLabData", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.Labs });

        plans.Add(new HemosheetSectionPlan
        {
            SectionId = HemosheetSectionId.DialysisRecords,
            VisibleColumns = Hprp.HprpHemosheetPlanInterpreter.Evaluate("feature:showHdfColumns", viewModel)
                ? HemosheetDialysisColumnSets.Hdf
                : HemosheetDialysisColumnSets.Base,
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

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("feature:showProgressNote", viewModel)
            || Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasProgressNotes", viewModel))
        {
            plans.Add(new HemosheetSectionPlan
            {
                SectionId = HemosheetSectionId.ProgressNotes,
                FixedLineCount = settings.FixedLines.ProgressNote,
            });
        }

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("data:hasFooterChecklists", viewModel))
            plans.Add(new() { SectionId = HemosheetSectionId.FooterChecklists });

        plans.Add(new() { SectionId = HemosheetSectionId.PrePostHdNotes });
        plans.Add(new() { SectionId = HemosheetSectionId.PostVitals });
        plans.Add(new() { SectionId = HemosheetSectionId.AvfAssessment });

        if (Hprp.HprpHemosheetPlanInterpreter.Evaluate("profile:Rama", viewModel)
            && Hprp.HprpHemosheetPlanInterpreter.Evaluate("feature:showConsentBlock", viewModel)
            && _profileRegistry.IsProfileSection(HemosheetSectionId.Consent, profile))
        {
            plans.Add(new() { SectionId = HemosheetSectionId.Consent });
        }

        return plans;
    }
}

internal static class HemosheetDialysisColumnSets
{
    public static readonly string[] Base =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP", "TMP", "DC", "NSS", "UF Rate", "Total", "หมายเหตุ",
    ];

    public static readonly string[] Hdf =
    [
        "เวลา", "BP", "HR", "RR", "BFR", "VP",
        "Substitute total", "Substitute rate",
        "TMP", "DC", "NSS", "UF Rate", "Total", "หมายเหตุ",
    ];
}

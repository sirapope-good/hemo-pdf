using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Layouts.Hprp;

public static class HprpHemosheetPlanInterpreter
{
    public static IReadOnlyList<HemosheetSectionPlan> Interpret(
        HprpLayout layout,
        HemosheetReportViewModel viewModel)
    {
        if (layout.Sections.Count == 0)
            return [];

        var plans = new List<HemosheetSectionPlan>();
        foreach (var section in layout.Sections)
        {
            if (!HprpWhen.Matches(section.When, token => Evaluate(token, viewModel)))
                continue;

            if (!HprpWidgetIds.TryMapHemosheetSection(section.Widget, out var sectionId))
                continue;

            plans.Add(new HemosheetSectionPlan
            {
                SectionId = sectionId,
                Variant = section.Variant,
                VisibleColumns = ResolveColumns(section, viewModel),
                FixedLineCount = ResolveFixedLines(section.FixedLinesFrom, viewModel),
            });
        }

        return plans;
    }

    public static bool Evaluate(string token, HemosheetReportViewModel viewModel)
    {
        var trimmed = token.Trim();
        var negated = trimmed.StartsWith("not-", StringComparison.OrdinalIgnoreCase);
        if (negated)
            trimmed = trimmed["not-".Length..];

        var matched = EvaluatePositive(trimmed, viewModel);
        return negated ? !matched : matched;
    }

    private static bool EvaluatePositive(string token, HemosheetReportViewModel viewModel)
    {
        var features = viewModel.LayoutContext.Features;
        var profile = viewModel.LayoutContext.LayoutProfile;

        if (token.StartsWith("or:", StringComparison.OrdinalIgnoreCase))
        {
            return token["or:".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(part => Evaluate(part, viewModel));
        }

        if (token.StartsWith("feature:", StringComparison.OrdinalIgnoreCase))
        {
            var key = token["feature:".Length..];
            return features.TryGetValue(key, out var enabled) && enabled;
        }

        if (token.StartsWith("profile:", StringComparison.OrdinalIgnoreCase))
        {
            var name = token["profile:".Length..];
            return string.Equals(profile.ToString(), name, StringComparison.OrdinalIgnoreCase);
        }

        if (token.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return token["data:".Length..].ToLowerInvariant() switch
            {
                "hassubheader" => HasSubHeader(viewModel),
                "hasassessmentpreorre" => viewModel.Assessments.Pre.Count > 0 || viewModel.Assessments.Re.Count > 0,
                "hasassessmentre" => viewModel.Assessments.Re.Count > 0,
                "haspostassessmentbody" => HasPostAssessmentBody(viewModel),
                "hasnursingcareplan" => HasNursingCarePlan(viewModel),
                "hasotherassessmentbody" => HasOtherAssessmentBody(viewModel),
                "haslabdata" => HasLabData(viewModel),
                "hasprogressnotes" => viewModel.ProgressNotes.Count > 0,
                "hasfooterchecklists" => HasFooterChecklists(viewModel),
                _ => false,
            };
        }

        return false;
    }

    private static IReadOnlyList<string> ResolveColumns(HprpSectionNode section, HemosheetReportViewModel viewModel)
    {
        if (section.ColumnsWhen is not null)
        {
            foreach (var (when, columns) in section.ColumnsWhen)
            {
                if (Evaluate(when, viewModel))
                    return columns;
            }
        }

        return section.Columns ?? [];
    }

    private static int ResolveFixedLines(string? key, HemosheetReportViewModel viewModel)
    {
        var lines = viewModel.LayoutContext.ReportSettings.FixedLines;
        return key?.ToLowerInvariant() switch
        {
            "dialysis" => lines.Dialysis,
            "nurse" => lines.Nurse,
            "doctor" => lines.Doctor,
            "medicine" => lines.Medicine,
            "progressnote" => lines.ProgressNote,
            _ => 0,
        };
    }

    private static bool HasSubHeader(HemosheetReportViewModel viewModel) =>
        !string.IsNullOrWhiteSpace(viewModel.Patient.Diagnosis)
        || viewModel.Patient.Allergies.Count > 0;

    private static bool HasFooterChecklists(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Post.Any(HemosheetAssessmentFilters.IsFooterPostItem)
        || viewModel.Assessments.Other.Any(HemosheetAssessmentFilters.IsFooterMedicationItem);

    private static bool HasPostAssessmentBody(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Post.Any(i =>
            !HemosheetAssessmentFilters.IsFooterPostItem(i)
            && !HemosheetAssessmentFilters.IsAvfItem(i));

    private static bool HasOtherAssessmentBody(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Other.Any(i =>
            !HemosheetAssessmentFilters.IsFooterMedicationItem(i)
            && !HemosheetAssessmentFilters.IsNursingCarePlanItem(i));

    private static bool HasNursingCarePlan(HemosheetReportViewModel viewModel) =>
        viewModel.Assessments.Other.Any(HemosheetAssessmentFilters.IsNursingCarePlanItem);

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
}

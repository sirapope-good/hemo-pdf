using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

/// <summary>
/// Shared assessment classification for planner gating and preview body/footer splits.
/// Keep a single copy — planner and mappers used to drift when filters were duplicated.
/// </summary>
public static class HemosheetAssessmentFilters
{
    public static bool IsFooterPostItem(HemosheetAssessmentItemViewModel item)
    {
        var name = item.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.StartsWith("complication.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("nursing.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("health.", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "complication", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "nursing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "health", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "technical", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFooterMedicationItem(HemosheetAssessmentItemViewModel item)
    {
        var name = item.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.StartsWith("medication.", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "medication", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAvfItem(HemosheetAssessmentItemViewModel item) =>
        item.Name?.Contains("thrill", StringComparison.OrdinalIgnoreCase) == true
        || item.Name?.Contains("bruit", StringComparison.OrdinalIgnoreCase) == true
        || item.Name?.Contains("hematoma", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsNursingCarePlanItem(HemosheetAssessmentItemViewModel item) =>
        string.Equals(item.Name, "nursing_diagnosis", StringComparison.OrdinalIgnoreCase)
        || string.Equals(item.Name, "nursing_intervention", StringComparison.OrdinalIgnoreCase)
        || string.Equals(item.Name, "expected_outcomes", StringComparison.OrdinalIgnoreCase);

    public static IList<HemosheetAssessmentItemViewModel> SelectPostBodyItems(
        IList<HemosheetAssessmentItemViewModel> post) =>
        post.Where(i => !IsFooterPostItem(i) && !IsAvfItem(i)).ToList();

    public static IList<HemosheetAssessmentItemViewModel> SelectOtherBodyItems(
        IList<HemosheetAssessmentItemViewModel> other) =>
        other.Where(i => !IsFooterMedicationItem(i) && !IsNursingCarePlanItem(i)).ToList();

    public static IList<HemosheetAssessmentItemViewModel> SelectAvfItems(
        IList<HemosheetAssessmentItemViewModel> items) =>
        items.Where(IsAvfItem).ToList();
}

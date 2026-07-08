using System.Globalization;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Read helpers that turn the <see cref="HemosheetReportViewModel"/> into the display strings /
/// checkbox states the ThaiUR form expects. Kept separate from layout so binding rules are testable.
/// </summary>
internal static class ThaiUrData
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Kg(float? v) => v is > 0 ? $"{Round(v.Value)} Kg" : "-";

    public static string Ml(float? v) => v is not null ? $"{Round(v.Value)} ml" : "-";

    public static string Num(float? v) => v is not null ? Round(v.Value) : "-";

    public static string Num(int? v) => v?.ToString(Culture) ?? "-";

    private static string Round(float v) => Math.Round(v, 2).ToString("0.##", Culture);

    public static string Time(DateTime? dt) => dt?.ToString("HH:mm", Culture) ?? "";

    public static string Date(DateTime? dt) => dt?.ToString("dd/MM/yyyy", Culture) ?? "-";

    public static string Bp(int? sys, int? dia) =>
        $"{(sys?.ToString(Culture) ?? "-")}/{(dia?.ToString(Culture) ?? "-")}";

    public static string WeightGain(HemosheetReportViewModel vm)
    {
        var pre = vm.Dehydration.PreWeight;
        var dry = vm.DialysisPrescription.DryWeight;
        return pre is > 0 && dry is > 0 ? Kg(pre - dry) : "N/A";
    }

    public static string WeightLoss(HemosheetReportViewModel vm)
    {
        var pre = vm.Dehydration.PreWeight;
        var post = vm.Dehydration.PostWeight;
        if (pre is not > 0 || post is not > 0) return "N/A";
        var diff = pre.Value - post.Value;
        return (diff < 0 ? "+" : "") + Kg(Math.Abs(diff));
    }

    public static string Idwg(HemosheetReportViewModel vm)
    {
        var pre = vm.Dehydration.PreWeight;
        var last = vm.Dehydration.LastPostWeight;
        return pre is > 0 && last is > 0 ? Kg(pre - last) : "N/A";
    }

    public static string Allergies(HemosheetReportViewModel vm) =>
        vm.Patient.Allergies.Count == 0 ? "ไม่มีแพ้ยา" : string.Join(", ", vm.Patient.Allergies);

    /// <summary>null = item absent, true/false = present and (un)checked.</summary>
    public static bool? PreState(HemosheetReportViewModel vm, params string[] keys) =>
        State(vm.Assessments.Pre, keys);

    public static bool? PostState(HemosheetReportViewModel vm, params string[] keys) =>
        State(vm.Assessments.Post, keys);

    private static bool? State(IEnumerable<HemosheetAssessmentItemViewModel> items, string[] keys)
    {
        var item = items.FirstOrDefault(i =>
            keys.Any(k => string.Equals(i.Name, k, StringComparison.OrdinalIgnoreCase)));
        return item is null ? null : item.Checked;
    }

    /// <summary>true when any assessment item whose name/text matches the label is checked.</summary>
    public static bool Checked(HemosheetReportViewModel vm, string label)
    {
        bool Match(IEnumerable<HemosheetAssessmentItemViewModel> src) => src.Any(i =>
            i.Checked && (
                string.Equals(i.Text, label, StringComparison.OrdinalIgnoreCase) ||
                (i.Name?.EndsWith(Slug(label), StringComparison.OrdinalIgnoreCase) ?? false)));

        return Match(vm.Assessments.Post) || Match(vm.Assessments.Other);
    }

    public static string? OtherText(HemosheetReportViewModel vm, string key) =>
        vm.Assessments.Other
            .FirstOrDefault(i => string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase))?.Text;

    private static string Slug(string label) =>
        label.Replace(" ", "").Replace("/", "").ToLowerInvariant();
}

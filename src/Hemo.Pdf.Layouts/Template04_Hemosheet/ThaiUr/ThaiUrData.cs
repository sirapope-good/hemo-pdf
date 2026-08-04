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

    /// <summary>
    /// Footer UI label → tokens that may appear as option DisplayName, option Name, or dotted suffix.
    /// Covers default seed + ThaiUR/Telerik short keys + common spelling variants.
    /// </summary>
    private static readonly Dictionary<string, string[]> FooterAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Hypotension"] = ["Hypotension", "Hypo-tension", "hypo"],
            ["Hypertension"] = ["Hypertension", "hyper"],
            ["Muscle cramp"] = ["Muscle cramp", "muscle"],
            ["Headache"] = ["Headache", "head"],
            ["Nausea / Vomitting"] = ["Nausea / Vomitting", "Nausea/Vomitting", "Nausea/Vomit", "vomit", "nau"],
            ["Fever"] = ["Fever", "fever"],
            ["Chest pain"] = ["Chest pain", "chest"],
            ["Arrhythmia"] = ["Arrhythmia", "arr"],
            ["Access problem"] = ["Access problem", "Vascular access problem", "access", "vascular"],
            ["Hypoglycemia"] = ["Hypoglycemia", "hypogly"],
            ["Dizziness"] = ["Dizziness", "dizz"],
            ["No complication"] = ["No complication", "no"],
            ["Blood leak"] = ["Blood leak", "bloodleak", "blood"],
            ["Clotted dialyzer"] = ["Clotted dialyzer", "dialyzer"],
            ["Clotted blood line"] = ["Clotted blood line", "bloodline"],
            ["Machine problem"] = ["Machine problem", "machine"],
            ["Phycho support"] = ["Phycho support", "Psychological support", "psycho", "phycho"],
            ["Trenderlenburg position"] = ["Trenderlenburg position", "Trendelenburg position", "tren", "trender"],
            ["Monitor V/S"] = ["Monitor V/S", "Monitor vital signs", "vital", "monitor"],
            ["Pause UF"] = ["Pause UF", "Pause ultrafiltration", "uf", "pause"],
            ["Hypertonic solution"] = ["Hypertonic solution"],
            ["Oxygen therapy"] = ["Oxygen therapy", "oxygen", "oxegen"],
            ["Decrease Dialysate T."] = ["Decrease Dialysate T.", "Decrease dialysate temperature", "temp", "ddt"],
            ["Hot compress"] = ["Hot compress", "Hot compression", "hot", "hcompress"],
            ["Strength exercise"] = ["Strength exercise", "exercise", "strexe"],
            ["Cold compress"] = ["Cold compress", "Cold compression", "cold", "ccompress"],
            ["Aware aspirate"] = ["Aware aspirate", "Aspirate precaution", "aspirate", "aware"],
            ["Monitor EKG"] = ["Monitor EKG", "ekg"],
            ["Decrease BFR"] = ["Decrease BFR", "bfr"],
            ["Monitor access flow"] = ["Monitor access flow", "flow", "maf"],
            ["Change dialyzer"] = ["Change dialyzer", "dchange"],
            ["Change blood line"] = ["Change blood line", "bchange"],
            ["Notified doctor"] = ["Notified doctor", "Notified physician", "notify", "noti"],
            ["Post HD nursing care"] = ["Post HD nursing care", "ncare"],
            ["Nutrition"] = ["Nutrition", "nutrition", "nutri"],
            ["Vascular Access"] = ["Vascular Access", "Vascular access", "vascular", "vas"],
            ["Exercise"] = ["Exercise", "exercise", "exe"],
            ["Personal hygine"] = ["Personal hygine", "Personal hygiene", "hygiene", "hyg"],
            ["Medication"] = ["Medication", "medication", "med"],
            ["Fluid control"] = ["Fluid control", "fluid"],
            ["KT"] = ["KT", "KT preparation", "kt"],
        };

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

    public static string? Map(int? sys, int? dia)
    {
        if (sys is null || dia is null) return null;
        return Round((sys.Value + 2 * dia.Value) / 3f);
    }

    /// <summary>NSS for fluid summary: session totals, else sum of dialysis-row NSS.</summary>
    public static float? NssMl(HemosheetReportViewModel vm)
    {
        if (vm.Dehydration.FlushNssTotal is not null) return vm.Dehydration.FlushNssTotal;
        if (vm.Dehydration.FlushNss is not null) return vm.Dehydration.FlushNss;
        var sum = vm.DialysisRecords.Sum(r => r.Nss ?? 0f);
        return sum > 0 ? sum : null;
    }

    /// <summary>Extra-fluid as ml (DTO may already be ml; values &lt; 20 treated as liters).</summary>
    public static float? ExtraFluidMl(HemosheetReportViewModel vm)
    {
        var v = vm.Dehydration.ExtraFluid;
        if (v is null) return null;
        return v is > 0 and < 20 ? v * 1000f : v;
    }

    public static float? TotalUfMl(HemosheetReportViewModel vm)
    {
        var uf = vm.Dehydration.TotalUf ?? vm.Dehydration.UfNet;
        return uf is null ? null : uf * 1000f;
    }

    public static float? NetFluidBalanceMl(HemosheetReportViewModel vm)
    {
        var totalUf = TotalUfMl(vm);
        if (totalUf is null) return null;
        return totalUf - (NssMl(vm) ?? 0f) - (ExtraFluidMl(vm) ?? 0f);
    }

    /// <summary>null = item absent, true/false = present and (un)checked.</summary>
    public static bool? PreState(HemosheetReportViewModel vm, params string[] keys) =>
        State(vm.Assessments.Pre, keys);

    public static bool? PostState(HemosheetReportViewModel vm, params string[] keys) =>
        State(vm.Assessments.Post, keys);

    public static bool? OtherState(HemosheetReportViewModel vm, params string[] keys) =>
        State(vm.Assessments.Other, keys);

    /// <summary>Pre first, then Other (Telerik binds Urine under Other).</summary>
    public static bool? PreOrOtherState(HemosheetReportViewModel vm, params string[] keys) =>
        PreState(vm, keys) ?? OtherState(vm, keys);

    /// <summary>
    /// Free-text / number from assessment item. Pre first, then Other
    /// (Pain score is seed Pre <c>pain</c>; Urine ml/day is typically Other <c>urine</c>).
    /// </summary>
    public static string? PreOrOtherText(HemosheetReportViewModel vm, params string[] keys) =>
        FindText(vm.Assessments.Pre, keys) ?? FindText(vm.Assessments.Other, keys);

    private static bool? State(IEnumerable<HemosheetAssessmentItemViewModel> items, string[] keys)
    {
        // Prefer keys in caller order so ThaiUR shorts (inf) win over seed aliases (inflame)
        // when both exist in the payload.
        foreach (var key in keys)
        {
            var item = items.FirstOrDefault(i => NameMatches(i.Name, key));
            if (item is not null)
                return item.Checked;
        }

        return null;
    }

    private static string? FindText(IEnumerable<HemosheetAssessmentItemViewModel> items, string[] keys)
    {
        foreach (var key in keys)
        {
            var item = items.FirstOrDefault(i => NameMatches(i.Name, key));
            if (item is not null)
                return string.IsNullOrWhiteSpace(item.Text) ? null : item.Text.Trim();
        }

        return null;
    }

    /// <summary>
    /// Match seed names (<c>pale</c>), Telerik shorts (<c>crep</c>), aliases (<c>vas:edema</c>),
    /// and dotted keys (<c>complication.hypo</c>).
    /// </summary>
    internal static bool NameMatches(string? name, string key)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
            return false;

        if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.EndsWith(":" + key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.EndsWith("." + key, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Footer checkbox: match UI label against BE multi-select <c>SelectedOptions</c> (DisplayNames),
    /// item Text, or Name / dotted suffix — using alias table for seed ↔ ThaiUR spelling gaps.
    /// </summary>
    public static bool Checked(HemosheetReportViewModel vm, string label)
    {
        var tokens = FooterAliases.TryGetValue(label, out var aliases)
            ? aliases
            : [label];

        return MatchFooter(vm.Assessments.Post, tokens)
            || MatchFooter(vm.Assessments.Other, tokens);
    }

    private static bool MatchFooter(IEnumerable<HemosheetAssessmentItemViewModel> src, string[] tokens)
    {
        foreach (var item in src)
        {
            foreach (var option in item.SelectedOptions)
            {
                if (tokens.Any(t => TokenEquals(option, t)))
                    return true;
            }

            if (item.Checked || item.SelectedOptions.Count > 0)
            {
                if (tokens.Any(t => TokenEquals(item.Text, t)))
                    return true;

                if (tokens.Any(t => NameMatches(item.Name, t) || NameMatches(item.Name, Slug(t))))
                    return true;
            }
        }

        return false;
    }

    internal static bool TokenEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;
        return string.Equals(NormalizeToken(a), NormalizeToken(b), StringComparison.Ordinal);
    }

    private static string NormalizeToken(string value) =>
        value.Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("/", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .ToLowerInvariant();

    public static string? OtherText(HemosheetReportViewModel vm, string key) =>
        vm.Assessments.Other
            .FirstOrDefault(i => string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase))?.Text;

    private static string Slug(string label) =>
        label.Replace(" ", "").Replace("/", "").Replace(".", "").Replace("-", "").ToLowerInvariant();

    /// <summary>
    /// ThaiUR Nursing Diagnosis / Intervention / Outcomes from Nurse Processing (Progress Notes).
    /// Focus → Diagnosis; I → Intervention; E → Evaluation.
    /// Newlines inside I/E (UI "+" lines) expand to extra table rows; Focus stays on the first row of each note.
    /// Always keeps exactly one blank trailing row (or a single blank when empty). Do not pad to
    /// FixedLines.ProgressNote — Hemopro defaults that to 6 and floods empty rows.
    /// </summary>
    public static IReadOnlyList<(string Diagnosis, string Intervention, string Outcome)> NursingPlanRows(
        HemosheetReportViewModel vm)
    {
        var rows = new List<(string Diagnosis, string Intervention, string Outcome)>();

        foreach (var note in vm.ProgressNotes)
        {
            var focusLines = SplitNoteLines(note.Focus);
            var interventionLines = SplitNoteLines(note.I);
            var outcomeLines = SplitNoteLines(note.E);

            if (focusLines.Length == 0 && interventionLines.Length == 0 && outcomeLines.Length == 0)
                continue;

            var lineCount = Math.Max(1, Math.Max(focusLines.Length, Math.Max(interventionLines.Length, outcomeLines.Length)));
            for (var i = 0; i < lineCount; i++)
            {
                // Each newline from UI "+" (or Focus wrap) becomes its own table row.
                var diagnosis = i < focusLines.Length ? focusLines[i] : "";
                var intervention = i < interventionLines.Length ? interventionLines[i] : "";
                var outcome = i < outcomeLines.Length ? outcomeLines[i] : "";
                rows.Add((diagnosis, intervention, outcome));
            }
        }

        // One blank slot only (empty sheet → single blank; with data → one trailing blank).
        rows.Add(("", "", ""));

        return rows;
    }

    internal static string[] SplitNoteLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();
    }
}

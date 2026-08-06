using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Bottom band of ThaiUR hemosheet: complication/nursing + health/med/notes/vitals.
/// Kept separate from <see cref="ThaiUrHemosheetForm"/> so footer layout edits stay localized.
/// </summary>
internal static class ThaiUrHemosheetFooter
{
    private const Unit Mm = Unit.Millimetre;
    private const float Rh = HemosheetThaiUrStyle.RowHeightMm;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    /// <summary>Base height for one nurse-note row (one text line + padding).</summary>
    private const float NoteRowHeightMm = 5.5f;

    /// <summary>
    /// Extra wrapped lines are tighter than the first row box (padding already in
    /// <see cref="NoteRowHeightMm"/>). Used for spacer / page-budget estimates.
    /// </summary>
    private const float NoteWrappedLineMm = 3.4f;

    /// <summary>
    /// Approx. characters per nurse-note content cell (right column minus signer).
    /// Tuned to QuestPDF wrap at ThaiUR content width — too low overestimates height
    /// and desyncs the left spacer (strips look “floated”).
    /// </summary>
    private const int NoteCharsPerLine = 56;

    public const int MinNurseNoteSlots = 2;
    public const int MaxNurseNoteSlots = 4;

    /// <summary>Minimum combined nurse-note height for the default 2 slots.</summary>
    public static float PrePostFloorHeightMm => MinNurseNoteSlots * NoteRowHeightMm;

    /// <summary>Shared label column for Post Vital and AVF/AVG so the first vertical rule aligns.</summary>
    private const float PostStripLabelMm = 18f;

    private const float MedColHeaderMm = 5.5f;
    public const float FluidSummaryHeightMm = 6.5f;

    /// <summary>Approx. height of the default page-number strip under page content.</summary>
    public const float PageNumberFooterMm = 7f;

    /// <summary>Headroom so slight wrap / border error does not push the footer band to page 2.</summary>
    public const float LayoutSafetyMm = 2f;

    /// <summary>Post Vital + AVF + Dialysis Nurse + Dialysis NA (right column only).</summary>
    public static float RightStripBandHeightMm => 4f * HemosheetThaiUrStyle.PostStripRowHeightMm;

    /// <summary>Legacy alias — right strips only (Nephrologist lives on the left now).</summary>
    public static float LowerStripBandHeightMm => RightStripBandHeightMm;

    public static float NephrologistRowHeightMm => HemosheetThaiUrStyle.PostStripRowHeightMm;

    private static readonly string[] ComplicationItems =
    [
        "Hypotension", "Hypertension", "Muscle cramp", "Headache", "Nausea / Vomitting", "Fever",
        "Chest pain", "Arrhythmia", "Access problem", "Hypoglycemia", "Dizziness", "No complication",
    ];

    private static readonly string[] TechnicalItems =
    [
        "Blood leak", "Clotted dialyzer", "Clotted blood line", "Machine problem", "No complication",
    ];

    private static readonly string[] NursingItems =
    [
        "Phycho support", "Trenderlenburg position", "Monitor V/S", "Pause UF", "Hypertonic solution",
        "Oxygen therapy", "Decrease Dialysate T.", "Hot compress", "Strength exercise", "Cold compress",
        "Aware aspirate", "Monitor EKG", "Decrease BFR", "Monitor access flow", "Change dialyzer",
        "Change blood line", "Notified doctor", "Post HD nursing care",
    ];

    private static readonly string[] HealthItems =
    [
        "Nutrition", "Vascular Access", "Exercise", "Personal hygine", "Medication", "Fluid control", "KT",
    ];

    public static void Compose(IContainer c, HemosheetReportViewModel vm, float nurseNotesHeightMm = 0f)
    {
        // nurseNotesHeightMm ignored — notes are content-sized; param kept for older call sites.
        ComposeBand(c, vm);
    }

    /// <summary>
    /// Single footer band (side-by-side): left = checks + spacer + Nephrologist;
    /// right = health/med + notes + spacer + Post Vital…Dialysis NA.
    /// Strips sit beside the assessment column — not stacked under it (stacking made
    /// footer taller than page 1 leftover and shoved AVF/Nurse/NA to page 2).
    /// Spacers use <see cref="NurseNotesFloorHeightMm"/> so both columns share one bottom edge.
    /// <see cref="IContainer.ShowEntire"/> keeps the band from splitting across pages.
    /// </summary>
    public static void ComposeBand(IContainer c, HemosheetReportViewModel vm)
    {
        var panelMm = AssessmentPanelHeightMm();
        var healthMedMm = HealthMedRowHeightMm(vm);
        var notesMm = NurseNotesFloorHeightMm(vm);
        var nephMm = NephrologistRowHeightMm;
        var stripsMm = RightStripBandHeightMm;

        var leftNaturalMm = panelMm + nephMm;
        var rightNaturalMm = healthMedMm + notesMm + stripsMm;
        var targetMm = Math.Max(leftNaturalMm, rightNaturalMm);
        var leftSpacerMm = Math.Max(0f, targetMm - leftNaturalMm);
        var rightSpacerMm = Math.Max(0f, targetMm - rightNaturalMm);

        // Keep checks + notes + Neph + Post Vital…NA on one page (no orphan AVF strip).
        c.ShowEntire().Row(row =>
        {
            row.ConstantItem(76, Mm).Column(left =>
            {
                left.Item().Element(a => AssessmentChecksOnly(a, vm, panelMm));
                if (leftSpacerMm > 0.05f)
                    left.Item().Height(leftSpacerMm, Mm);
                left.Item().Element(n => NephrologistCell(n, vm, nephMm));
            });
            row.RelativeItem().Column(right =>
            {
                right.Item().Height(healthMedMm, Mm).Row(r =>
                {
                    r.ConstantItem(42, Mm).Border(Bw).Element(he => HealthEducation(he, vm));
                    r.RelativeItem().Border(Bw).Element(med => MedicationTable(med, vm, healthMedMm));
                });
                right.Item().Element(n => NurseNoteRows(n, vm));
                if (rightSpacerMm > 0.05f)
                    right.Item().Height(rightSpacerMm, Mm);
                right.Item().Element(s => RightLowerStrips(s, vm));
            });
        });
    }

    /// <summary>Deprecated entry — use <see cref="ComposeBand"/>.</summary>
    public static void ComposeUpper(IContainer c, HemosheetReportViewModel vm) => ComposeBand(c, vm);

    /// <summary>Deprecated — strips are composed inside <see cref="ComposeBand"/>.</summary>
    public static void ComposeLowerStrips(IContainer c, HemosheetReportViewModel vm) =>
        RightLowerStrips(c, vm);

    private static void NephrologistCell(IContainer c, HemosheetReportViewModel vm, float heightMm)
    {
        c.Border(Bw).Height(heightMm, Mm).AlignMiddle().AlignCenter()
            .Text(text =>
            {
                text.Span("Nephrologist ").Style(ThaiUrText.Bold);
                var doctor = vm.DoctorName ?? vm.Patient.DoctorName;
                text.Span(string.IsNullOrWhiteSpace(doctor) ? "-" : doctor).Style(ThaiUrText.Base);
            });
    }

    private static void RightLowerStrips(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(right =>
        {
            right.Item().Element(pv => PostVital(pv, vm));
            right.Item().Element(av => AvfRow(av, vm));
            right.Item().Border(Bw).Height(HemosheetThaiUrStyle.PostStripRowHeightMm, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis Nurse");
                r.RelativeItem().Value(vm.NursesInShiftNonPn);
            });
            right.Item().Border(Bw).Height(HemosheetThaiUrStyle.PostStripRowHeightMm, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis NA");
                r.RelativeItem().Value(vm.NursesInShift);
            });
        });
    }

    /// <summary>
    /// How many nurse-note rows to paint: always at least 2 (Pre/Post placeholders),
    /// at most <see cref="MaxNurseNoteSlots"/>. Extra records beyond max are omitted.
    /// </summary>
    public static int NurseNoteSlotCount(HemosheetReportViewModel vm) =>
        Math.Clamp(Math.Max(MinNurseNoteSlots, vm.NurseRecords.Count), MinNurseNoteSlots, MaxNurseNoteSlots);

    public static float NurseNotesFloorHeightMm(HemosheetReportViewModel vm)
    {
        var total = 0f;
        foreach (var (label, content, _) in BuildNurseNoteSlots(vm))
            total += EstimateNoteRowHeightMm(FormatNoteDisplay(label, content));
        return total;
    }

    /// <summary>Estimated rendered height for one nurse-note slot (matches MinHeight + wrap).</summary>
    internal static float EstimateNoteRowHeightMm(string display)
    {
        var lines = EstimateNoteLines(display);
        if (lines <= 1)
            return NoteRowHeightMm;
        return NoteRowHeightMm + (lines - 1) * NoteWrappedLineMm;
    }

    /// <summary>How many wrapped lines a nurse-note display string needs (1..<see cref="HemosheetThaiUrStyle.NurseNoteMaxLines"/>).</summary>
    internal static int EstimateNoteLines(string display)
    {
        if (string.IsNullOrWhiteSpace(display) || display == "-")
            return 1;

        var length = display.Trim().Length;
        var lines = (int)Math.Ceiling(length / (double)NoteCharsPerLine);
        return Math.Clamp(lines, 1, HemosheetThaiUrStyle.NurseNoteMaxLines);
    }

    internal static string FormatNoteDisplay(string label, string? content)
    {
        var body = string.IsNullOrWhiteSpace(content) ? "" : content.Trim();
        // Avoid "Pre HD Pre HD …" when the record already starts with the slot label.
        if (!string.IsNullOrEmpty(label)
            && body.StartsWith(label, StringComparison.OrdinalIgnoreCase))
        {
            body = body[label.Length..].TrimStart(' ', '-', ':');
        }

        if (string.IsNullOrEmpty(body))
            return string.IsNullOrEmpty(label) ? "-" : label;

        return string.IsNullOrEmpty(label) ? body : $"{label} {body}";
    }

    /// <summary>
    /// Fluid + balanced footer band (checks/Nephrologist | health/notes/strips).
    /// </summary>
    public static float BottomBlockHeightMm(HemosheetReportViewModel vm, float nurseNotesHeightMm)
    {
        var leftMm = AssessmentPanelHeightMm() + NephrologistRowHeightMm;
        var rightMm = HealthMedRowHeightMm(vm) + nurseNotesHeightMm + RightStripBandHeightMm;
        return FluidSummaryHeightMm + Math.Max(leftMm, rightMm);
    }

    /// <summary>
    /// Legacy leftover budget for nurse-note height. Prefer dialysis row budgeting in the form composer.
    /// </summary>
    public static float ComputePrePostTotalHeightMm(HemosheetReportViewModel vm, float mainBandHeightMm)
    {
        var pageContentMm = 297f
            - 2f * HemosheetThaiUrStyle.PageMarginMm
            - PageNumberFooterMm;
        var notesFloor = NurseNotesFloorHeightMm(vm);
        var bottomFloorMm = BottomBlockHeightMm(vm, notesFloor);
        var maxOnSamePageMm = pageContentMm
            - LayoutSafetyMm
            - mainBandHeightMm
            - bottomFloorMm
            + notesFloor;

        if (maxOnSamePageMm < notesFloor)
            return notesFloor;

        return notesFloor;
    }

    public static float AssessmentPanelHeightMm() => Math.Max(
        CheckPanelHeightMm(ComplicationItems, TechnicalItems),
        CheckPanelHeightMm(NursingItems, null));

    public static float HealthEducationHeightMm() =>
        HemosheetThaiUrStyle.HeaderBarHeightMm
        + HealthItems.Length * HemosheetThaiUrStyle.PostCheckRowHeightMm;

    public static float MedicationNaturalHeightMm(HemosheetReportViewModel vm)
    {
        var lines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Medicine, vm.MedicineRecords.Count);
        if (lines <= 0) lines = 2;
        return HemosheetThaiUrStyle.HeaderBarHeightMm + MedColHeaderMm + lines * Rh + Rh;
    }

    public static float HealthMedRowHeightMm(HemosheetReportViewModel vm) =>
        Math.Max(HealthEducationHeightMm(), MedicationNaturalHeightMm(vm));

    private static float CheckPanelHeightMm(string[] items, string[]? items2)
    {
        var rh = HemosheetThaiUrStyle.PostCheckRowHeightMm;
        var h = HemosheetThaiUrStyle.HeaderBarHeightMm + items.Length * rh;
        if (items2 is not null)
            h += HemosheetThaiUrStyle.HeaderBarHeightMm + items2.Length * rh;
        return h;
    }

    private static void AssessmentChecksOnly(IContainer c, HemosheetReportViewModel vm, float panelMm)
    {
        c.MinHeight(panelMm, Mm).Row(r =>
        {
            r.RelativeItem().Border(Bw).MinHeight(panelMm, Mm).Element(cp =>
                CheckGroup(cp, vm, "Complication", ComplicationItems, "Technical complication", TechnicalItems));
            r.RelativeItem().Border(Bw).MinHeight(panelMm, Mm).Element(nm =>
                CheckGroup(nm, vm, "Nursing management", NursingItems, null, null));
        });
    }

    private static void CheckGroup(IContainer c, HemosheetReportViewModel vm, string title, string[] items, string? title2, string[]? items2)
    {
        var rh = HemosheetThaiUrStyle.PostCheckRowHeightMm;
        c.Column(col =>
        {
            col.Item().HeaderBar(title);
            foreach (var item in items)
            {
                col.Item().Height(rh, Mm).PaddingLeft(1f)
                    .CheckLine(item, ThaiUrData.Checked(vm, item));
            }

            if (title2 is not null && items2 is not null)
            {
                col.Item().HeaderBar(title2);
                foreach (var item in items2)
                {
                    col.Item().Height(rh, Mm).PaddingLeft(1f)
                        .CheckLine(item, ThaiUrData.Checked(vm, item));
                }
            }
        });
    }

    private static void HealthEducation(IContainer c, HemosheetReportViewModel vm)
    {
        var minRh = HemosheetThaiUrStyle.PostCheckRowHeightMm;
        c.Column(col =>
        {
            col.Item().HeaderBar("Health education");
            foreach (var item in HealthItems)
            {
                col.Item().Height(minRh, Mm).PaddingLeft(1f)
                    .CheckLine(item, ThaiUrData.Checked(vm, item));
            }
        });
    }

    private static void MedicationTable(IContainer c, HemosheetReportViewModel vm, float targetHeightMm)
    {
        var lines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Medicine, vm.MedicineRecords.Count);
        if (lines <= 0) lines = 2;
        var aboveHctMm = HemosheetThaiUrStyle.HeaderBarHeightMm + MedColHeaderMm + lines * Rh;
        var spacerMm = Math.Max(0f, targetHeightMm - aboveHctMm - Rh);

        c.Column(col =>
        {
            col.Item().HeaderBar("Medication duration HD");
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.ConstantColumn(16, Mm);
                    cols.ConstantColumn(14, Mm);
                });
                t.Cell().Border(Bw).Height(MedColHeaderMm, Mm).AlignMiddle().AlignCenter()
                    .Text("Name/Dose/Route").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).Height(MedColHeaderMm, Mm).AlignMiddle().AlignCenter()
                    .Text("Time").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).Height(MedColHeaderMm, Mm).AlignMiddle().AlignCenter()
                    .Text("Sign").Style(ThaiUrText.Bold);

                for (var i = 0; i < lines; i++)
                {
                    var m = i < vm.MedicineRecords.Count ? vm.MedicineRecords[i] : null;
                    var name = m is null ? "" : $"{m.MedicineName} {ThaiUrData.Num(m.Quantity)} {m.Route}".Trim();
                    t.Cell().BorderLeft(Bw).BorderRight(Bw).MinHeight(Rh, Mm).PaddingLeft(1f).AlignMiddle()
                        .Text(name).Style(ThaiUrText.Base);
                    t.Cell().BorderRight(Bw).MinHeight(Rh, Mm).ValueCentered(ThaiUrData.Time(m?.Timestamp));
                    t.Cell().BorderRight(Bw).MinHeight(Rh, Mm);
                }
            });
            // Continue Name|Time|Sign vertical rules through leftover space down to Hct/Hb.
            if (spacerMm > 0.05f)
            {
                col.Item().Height(spacerMm, Mm).Row(r =>
                {
                    r.RelativeItem(3).BorderLeft(Bw).BorderRight(Bw);
                    r.ConstantItem(16, Mm).BorderRight(Bw);
                    r.ConstantItem(14, Mm).BorderRight(Bw);
                });
            }
            col.Item().Border(Bw).Height(Rh, Mm).Row(lab =>
            {
                lab.RelativeItem().Label($"Hct: {vm.Labs.Hct ?? "-"}");
                lab.RelativeItem().Label($"Hb: {vm.Labs.Hb ?? "-"}");
            });
        });
    }

    private static void NurseNoteRows(IContainer c, HemosheetReportViewModel vm)
    {
        var slots = BuildNurseNoteSlots(vm);
        // MinHeight grows with wrapped text (up to NurseNoteMaxLines). ComposeBand spacers
        // use EstimateNoteRowHeightMm so left/right bottoms stay aligned.
        c.Column(col =>
        {
            foreach (var (label, content, signer) in slots)
                col.Item().MinHeight(NoteRowHeightMm, Mm).Element(n => NoteRow(n, label, content, signer));
        });

        static void NoteRow(IContainer container, string label, string? content, string? signer)
        {
            var display = FormatNoteDisplay(label, content);
            container.Row(r =>
            {
                r.RelativeItem().Border(Bw).PaddingHorizontal(1f).PaddingVertical(0.4f).AlignMiddle()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(ThaiUrText.Base);
                        text.ClampLines(HemosheetThaiUrStyle.NurseNoteMaxLines, "\u2026");
                        text.Span(display);
                    });
                r.ConstantItem(28, Mm).Border(Bw).PaddingHorizontal(1f).PaddingVertical(0.4f).AlignMiddle().AlignRight()
                    .Text(signer ?? "").Style(ThaiUrText.Base);
            });
        }
    }

    /// <summary>
    /// Map nurse records into display slots. First = Pre HD, last = Post HD, middle unlabeled.
    /// Caps at <see cref="MaxNurseNoteSlots"/>; always yields at least <see cref="MinNurseNoteSlots"/>.
    /// </summary>
    internal static IReadOnlyList<(string Label, string? Content, string? Signer)> BuildNurseNoteSlots(
        HemosheetReportViewModel vm)
    {
        var records = vm.NurseRecords;
        var slots = NurseNoteSlotCount(vm);
        var preSigner = SignatureName(vm, "pre_hd");
        var postSigner = SignatureName(vm, "post_hd");

        var rows = new List<(string Label, string? Content, string? Signer)>(slots);
        for (var i = 0; i < slots; i++)
        {
            var label = i == 0 ? "Pre HD" : i == slots - 1 ? "Post HD" : "";
            string? content = i < records.Count ? records[i].Content : null;
            if (i == slots - 1 && records.Count < 2)
                content ??= vm.DoctorRecords.FirstOrDefault()?.Content;

            // Prefer the note author; fall back to dedicated pre_hd / post_hd signature slots.
            var author = i < records.Count ? records[i].CreatorName : null;
            string? slotSignature = i == 0 ? preSigner : i == slots - 1 ? postSigner : null;
            var signer = FirstNonEmpty(author, slotSignature);
            rows.Add((label, content, signer));
        }

        return rows;
    }

    private static string? SignatureName(HemosheetReportViewModel vm, string key)
    {
        if (vm.SignatureNames.TryGetValue(key, out var exact) && !string.IsNullOrWhiteSpace(exact))
            return exact.Trim();

        foreach (var pair in vm.SignatureNames)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pair.Value))
                return pair.Value.Trim();
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static void PostVital(IContainer c, HemosheetReportViewModel vm)
    {
        var p = vm.PostVital;
        var h = HemosheetThaiUrStyle.PostStripRowHeightMm;
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(PostStripLabelMm, Mm);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
            });
            t.Cell().Border(Bw).Height(h, Mm).AlignMiddle().PaddingLeft(1f)
                .Text("Post Vital").Style(ThaiUrText.Bold);
            PostVitalCell(t, h, "BP", ThaiUrData.Bp(p?.Bps, p?.Bpd));
            PostVitalCell(t, h, "PR", ThaiUrData.Num(p?.Hr));
            PostVitalCell(t, h, "RR", ThaiUrData.Num(p?.Rr));
            PostVitalCell(t, h, "BT", $"{ThaiUrData.Num(p?.Temp)} \u00B0C");
            PostVitalCell(t, h, "Sat", $"{ThaiUrData.Num(p?.SpO2)} %");
        });
    }

    private static void PostVitalCell(TableDescriptor t, float heightMm, string label, string value)
    {
        t.Cell().Border(Bw).Height(heightMm, Mm).AlignMiddle().PaddingHorizontal(1f).Row(inner =>
        {
            inner.AutoItem().AlignMiddle().Text($"{label} ").Style(ThaiUrText.Bold);
            inner.RelativeItem().AlignMiddle()
                .Text(string.IsNullOrWhiteSpace(value) ? "-" : value).Style(ThaiUrText.Base);
        });
    }

    private static void AvfRow(IContainer c, HemosheetReportViewModel vm)
    {
        // Needle A/V sizes are already shown under Vascular Access — omit here.
        var h = HemosheetThaiUrStyle.PostStripRowHeightMm;
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(PostStripLabelMm, Mm);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn(1.5f);
            });
            t.Cell().Border(Bw).Height(h, Mm).AlignMiddle().PaddingLeft(1f)
                .Text("AVF/AVG").Style(ThaiUrText.Bold);
            AvfCell(t, h, "Thrill", ThaiUrData.PostState(vm, "thrill", "vas:av:thrill", "post:thrill"));
            AvfCell(t, h, "Bruit", ThaiUrData.PostState(vm, "bruit", "vas:av:bruit", "post:bruit"));
            AvfCell(t, h, "Hematoma", ThaiUrData.PostState(vm, "hematoma", "hema", "vas:hematoma"));
            AvfCell(t, h, "Stop Bleeding > 20 min",
                ThaiUrData.PostState(vm, "sb", "stop bleeding", "stopbleeding", "vas:sb"));
        });
    }

    private static void AvfCell(TableDescriptor t, float heightMm, string label, bool? yes)
    {
        t.Cell().Border(Bw).Height(heightMm, Mm).AlignMiddle().PaddingHorizontal(1.5f).Row(r =>
        {
            r.RelativeItem().AlignMiddle().Text(label).Style(ThaiUrText.Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == true, sizePt: 6.5f);
        });
    }
}

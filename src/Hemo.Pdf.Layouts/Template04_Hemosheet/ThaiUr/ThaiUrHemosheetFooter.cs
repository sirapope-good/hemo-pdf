using Hemo.Pdf.Core.Models.Hemosheet;
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

    /// <summary>Reserved height for Pre/Post HD note body (~2.5 text lines at ThaiUR base size).</summary>
    private const float NoteBodyMinHeightMm = 11.5f;

    /// <summary>Minimum combined Pre+Post HD height when the page has little leftover.</summary>
    public static float PrePostFloorHeightMm => 2f * NoteBodyMinHeightMm;

    /// <summary>Shared label column for Post Vital and AVF/AVG so the first vertical rule aligns.</summary>
    private const float PostStripLabelMm = 18f;

    private const float MedColHeaderMm = 5.5f;
    public const float FluidSummaryHeightMm = 6.5f;

    /// <summary>Approx. height of the default page-number strip under page content.</summary>
    public const float PageNumberFooterMm = 7f;

    /// <summary>Headroom so slight wrap / border error does not push ShowEntire(footer) to page 2.</summary>
    public const float LayoutSafetyMm = 4f;

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

    public static void Compose(IContainer c, HemosheetReportViewModel vm, float prePostTotalHeightMm)
    {
        var panelMm = AssessmentPanelHeightMm();
        var healthMedMm = HealthMedRowHeightMm(vm);
        var stripMm = HemosheetThaiUrStyle.PostStripRowHeightMm;
        var rightBelowMm = 4f * stripMm;
        var rightMm = healthMedMm + prePostTotalHeightMm + rightBelowMm;
        var leftMm = Math.Max(panelMm + stripMm, rightMm);
        var leftSpacerMm = Math.Max(0f, leftMm - panelMm - stripMm);

        c.Row(row =>
        {
            row.ConstantItem(76, Mm).Border(Bw)
                .Element(left => AssessmentAndNephrologistColumn(left, vm, panelMm, leftSpacerMm));
            row.RelativeItem().Border(Bw)
                .Element(right => HealthMedAndNotesColumn(right, vm, healthMedMm, prePostTotalHeightMm));
        });
    }

    /// <summary>
    /// Fluid summary + footer band height with the given Pre/Post HD total.
    /// Used to reserve space on page 1 before sizing dialysis rows (no ExtendVertical —
    /// that steals the whole page and ShowEntire then jumps the footer to page 2).
    /// </summary>
    public static float BottomBlockHeightMm(HemosheetReportViewModel vm, float prePostTotalHeightMm)
    {
        var panelMm = AssessmentPanelHeightMm();
        var healthMedMm = HealthMedRowHeightMm(vm);
        var stripMm = HemosheetThaiUrStyle.PostStripRowHeightMm;
        var rightMm = healthMedMm + prePostTotalHeightMm + 4f * stripMm;
        var footerMm = Math.Max(panelMm + stripMm, rightMm);
        return FluidSummaryHeightMm + footerMm;
    }

    /// <summary>
    /// Legacy leftover budget for Pre+Post HD. Prefer dialysis row budgeting in the form composer.
    /// </summary>
    public static float ComputePrePostTotalHeightMm(HemosheetReportViewModel vm, float mainBandHeightMm)
    {
        var pageContentMm = 297f
            - 2f * HemosheetThaiUrStyle.PageMarginMm
            - PageNumberFooterMm;
        var bottomFloorMm = BottomBlockHeightMm(vm, PrePostFloorHeightMm);
        // mainBand already includes dialysis; this helper is only for older call sites.
        var maxOnSamePageMm = pageContentMm
            - LayoutSafetyMm
            - mainBandHeightMm
            - bottomFloorMm
            + PrePostFloorHeightMm;

        if (maxOnSamePageMm < PrePostFloorHeightMm)
            return PrePostFloorHeightMm;

        return maxOnSamePageMm;
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

    private static void AssessmentAndNephrologistColumn(
        IContainer c,
        HemosheetReportViewModel vm,
        float panelMm,
        float spacerMm)
    {
        c.Column(col =>
        {
            col.Item().MinHeight(panelMm, Mm).Row(r =>
            {
                r.RelativeItem().Border(Bw).MinHeight(panelMm, Mm).Element(cp =>
                    CheckGroup(cp, vm, "Complication", ComplicationItems, "Technical complication", TechnicalItems));
                r.RelativeItem().Border(Bw).MinHeight(panelMm, Mm).Element(nm =>
                    CheckGroup(nm, vm, "Nursing management", NursingItems, null, null));
            });
            if (spacerMm > 0.05f)
                col.Item().Height(spacerMm, Mm);
            col.Item().Border(Bw).Height(HemosheetThaiUrStyle.PostStripRowHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Text(text =>
                {
                    text.Span("Nephrologist ").Style(ThaiUrText.Bold);
                    var doctor = vm.DoctorName ?? vm.Patient.DoctorName;
                    text.Span(string.IsNullOrWhiteSpace(doctor) ? "-" : doctor).Style(ThaiUrText.Base);
                });
        });
    }

    private static void HealthMedAndNotesColumn(
        IContainer c,
        HemosheetReportViewModel vm,
        float healthMedMm,
        float prePostTotalHeightMm)
    {
        var eachNoteMm = prePostTotalHeightMm / 2f;
        c.Column(col =>
        {
            col.Item().Height(healthMedMm, Mm).Row(r =>
            {
                r.ConstantItem(42, Mm).Border(Bw).Element(he => HealthEducation(he, vm));
                r.RelativeItem().Border(Bw).Element(med => MedicationTable(med, vm, healthMedMm));
            });
            col.Item().Height(prePostTotalHeightMm, Mm).Element(n => PrePostHdNotes(n, vm, eachNoteMm));
            col.Item().Element(pv => PostVital(pv, vm));
            col.Item().Element(av => AvfRow(av, vm));
            col.Item().Border(Bw).Height(HemosheetThaiUrStyle.PostStripRowHeightMm, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis Nurse");
                r.RelativeItem().Value(vm.NursesInShiftNonPn);
            });
            col.Item().Border(Bw).Height(HemosheetThaiUrStyle.PostStripRowHeightMm, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis NA");
                r.RelativeItem().Value(vm.NursesInShift);
            });
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

    private static void PrePostHdNotes(IContainer c, HemosheetReportViewModel vm, float eachNoteMm)
    {
        var pre = vm.NurseRecords.FirstOrDefault()?.Content;
        var post = vm.NurseRecords.Skip(1).FirstOrDefault()?.Content
            ?? vm.DoctorRecords.FirstOrDefault()?.Content;
        vm.SignatureNames.TryGetValue("pre_hd", out var preSigner);
        vm.SignatureNames.TryGetValue("post_hd", out var postSigner);

        c.Column(col =>
        {
            col.Item().Height(eachNoteMm, Mm).Element(n => NoteRow(n, "Pre HD", pre, preSigner));
            col.Item().Height(eachNoteMm, Mm).Element(n => NoteRow(n, "Post HD", post, postSigner));
        });

        static void NoteRow(IContainer container, string label, string? content, string? signer)
        {
            var body = string.IsNullOrWhiteSpace(content) ? "" : content.Trim();
            container.Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.ConstantColumn(28, Mm);
                });
                t.Cell().Border(Bw).ExtendVertical().Padding(1f).AlignTop()
                    .Text(string.IsNullOrEmpty(body) ? label : $"{label} {body}")
                    .Style(ThaiUrText.Base);
                t.Cell().Border(Bw).ExtendVertical().Padding(1f).AlignBottom().AlignRight()
                    .Text(signer ?? "").Style(ThaiUrText.Base);
            });
        }
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

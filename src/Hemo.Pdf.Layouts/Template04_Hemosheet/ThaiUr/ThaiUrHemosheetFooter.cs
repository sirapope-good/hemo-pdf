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

    public static void Compose(IContainer c, HemosheetReportViewModel vm)
    {
        // Left block = Complication | Nursing, with Nephrologist spanning both (long doctor names).
        // Right block = Health | Med, then Pre/Post (reserved 2–3 lines), then vitals/signatures
        // snapped to the bottom of the band so the page fills evenly.
        c.Row(row =>
        {
            row.ConstantItem(76, Mm).Border(Bw).Element(left => AssessmentAndNephrologistColumn(left, vm));
            row.RelativeItem().Border(Bw).Element(right => HealthMedAndNotesColumn(right, vm));
        });
    }

    private static void AssessmentAndNephrologistColumn(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignTop().Border(Bw).Element(cp =>
                    CheckGroup(cp, vm, "Complication", ComplicationItems, "Technical complication", TechnicalItems));
                r.RelativeItem().AlignTop().Border(Bw).Element(nm =>
                    CheckGroup(nm, vm, "Nursing management", NursingItems, null, null));
            });
            col.Item().ExtendVertical().AlignBottom().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(24, Mm).LabelBold("Nephrologist");
                r.RelativeItem().Value(vm.DoctorName ?? vm.Patient.DoctorName);
            });
        });
    }

    private static void HealthMedAndNotesColumn(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(42, Mm).AlignTop().Border(Bw).Element(he => HealthEducation(he, vm));
                r.RelativeItem().AlignTop().Border(Bw).Element(med => MedicationTable(med, vm));
            });
            col.Item().Element(n => PrePostHdNotes(n, vm));
            // Push vitals/signatures to the bottom of this column (aligns with Nephrologist).
            col.Item().ExtendVertical().AlignBottom().Element(bottom =>
            {
                bottom.Column(stack =>
                {
                    stack.Item().Element(pv => PostVital(pv, vm));
                    stack.Item().Element(av => AvfRow(av, vm));
                    stack.Item().Border(Bw).Height(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(28, Mm).LabelBold("Dialysis Nurse");
                        r.RelativeItem().Value(vm.NursesInShiftNonPn);
                    });
                    stack.Item().Border(Bw).Height(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(28, Mm).LabelBold("Dialysis NA");
                        r.RelativeItem().Value(vm.NursesInShift);
                    });
                });
            });
        });
    }

    private static void CheckGroup(IContainer c, HemosheetReportViewModel vm, string title, string[] items, string? title2, string[]? items2)
    {
        c.Column(col =>
        {
            col.Item().HeaderBar(title);
            foreach (var item in items)
                col.Item().Border(Bw).Height(HemosheetThaiUrStyle.CheckRowHeightMm, Mm).PaddingLeft(1f)
                    .CheckLine(item, ThaiUrData.Checked(vm, item));

            if (title2 is not null && items2 is not null)
            {
                col.Item().HeaderBar(title2);
                foreach (var item in items2)
                    col.Item().Border(Bw).Height(HemosheetThaiUrStyle.CheckRowHeightMm, Mm).PaddingLeft(1f)
                        .CheckLine(item, ThaiUrData.Checked(vm, item));
            }
        });
    }

    private static void HealthEducation(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().HeaderBar("Health education");
            foreach (var item in HealthItems)
                col.Item().Border(Bw).Height(HemosheetThaiUrStyle.CheckRowHeightMm, Mm).PaddingLeft(1f)
                    .CheckLine(item, ThaiUrData.Checked(vm, item));
        });
    }

    private static void MedicationTable(IContainer c, HemosheetReportViewModel vm)
    {
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
                t.Cell().Border(Bw).AlignCenter().Text("Name/Dose/Route").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).AlignCenter().Text("Time").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).AlignCenter().Text("Sign").Style(ThaiUrText.Bold);

                var lines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Medicine, vm.MedicineRecords.Count);
                if (lines <= 0) lines = 2;
                for (var i = 0; i < lines; i++)
                {
                    var m = i < vm.MedicineRecords.Count ? vm.MedicineRecords[i] : null;
                    var name = m is null ? "" : $"{m.MedicineName} {ThaiUrData.Num(m.Quantity)} {m.Route}".Trim();
                    t.Cell().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).Text(name).Style(ThaiUrText.Base);
                    t.Cell().Border(Bw).Height(Rh, Mm).ValueCentered(ThaiUrData.Time(m?.Timestamp));
                    t.Cell().Border(Bw).Height(Rh, Mm);
                }
            });
            // Telerik places Hct/Hb under the medication column, not Health education.
            col.Item().Border(Bw).Height(Rh, Mm).Row(lab =>
            {
                lab.RelativeItem().Label($"Hct: {vm.Labs.Hct ?? "-"}");
                lab.RelativeItem().Label($"Hb: {vm.Labs.Hb ?? "-"}");
            });
        });
    }

    private static void PrePostHdNotes(IContainer c, HemosheetReportViewModel vm)
    {
        var pre = vm.NurseRecords.FirstOrDefault()?.Content;
        var post = vm.NurseRecords.Skip(1).FirstOrDefault()?.Content
            ?? vm.DoctorRecords.FirstOrDefault()?.Content;
        vm.SignatureNames.TryGetValue("pre_hd", out var preSigner);
        vm.SignatureNames.TryGetValue("post_hd", out var postSigner);

        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.ConstantColumn(28, Mm);
            });

            NoteRow(t, "Pre HD", pre, preSigner);
            NoteRow(t, "Post HD", post, postSigner);
        });

        static void NoteRow(TableDescriptor t, string label, string? content, string? signer)
        {
            var body = string.IsNullOrWhiteSpace(content) ? "" : content.Trim();
            // Reserve ~2–3 lines even when empty; grow with real text via MinHeight.
            t.Cell().Border(Bw).MinHeight(NoteBodyMinHeightMm, Mm).Padding(1f).AlignTop()
                .Text(string.IsNullOrEmpty(body) ? label : $"{label} {body}")
                .Style(ThaiUrText.Base);
            t.Cell().Border(Bw).MinHeight(NoteBodyMinHeightMm, Mm).Padding(1f).AlignBottom().AlignRight()
                .Text(signer ?? "").Style(ThaiUrText.Base);
        }
    }

    private static void PostVital(IContainer c, HemosheetReportViewModel vm)
    {
        var p = vm.PostVital;
        c.Border(Bw).Height(Rh, Mm).PaddingHorizontal(1f).Row(r =>
        {
            r.ConstantItem(18, Mm).LabelBold("Post Vital");
            PostVitalItem(r, "BP", ThaiUrData.Bp(p?.Bps, p?.Bpd));
            PostVitalItem(r, "PR", ThaiUrData.Num(p?.Hr));
            PostVitalItem(r, "RR", ThaiUrData.Num(p?.Rr));
            PostVitalItem(r, "BT", $"{ThaiUrData.Num(p?.Temp)} \u00B0C");
            PostVitalItem(r, "Sat", $"{ThaiUrData.Num(p?.SpO2)} %");
        });
    }

    private static void AvfRow(IContainer c, HemosheetReportViewModel vm)
    {
        c.Border(Bw).Height(Rh, Mm).PaddingLeft(1f).Row(r =>
        {
            r.ConstantItem(14, Mm).LabelBold("AVF/AVG");
            AvfCheck(r, "Thrill", ThaiUrData.PostState(vm, "thrill", "vas:av:thrill", "post:thrill"));
            AvfCheck(r, "Bruit", ThaiUrData.PostState(vm, "bruit", "vas:av:bruit", "post:bruit"));
            AvfCheck(r, "Hematoma", ThaiUrData.PostState(vm, "hematoma", "hema", "vas:hematoma"));
            AvfCheck(r, "Stop Bleeding > 20 min", ThaiUrData.PostState(vm, "sb", "stop bleeding", "stopbleeding", "vas:sb"));
            r.RelativeItem().AlignMiddle().AlignRight().PaddingRight(2f)
                .Text($"A{ThaiUrData.Num(vm.AvShunt.ANeedleSize)}/V{ThaiUrData.Num(vm.AvShunt.VNeedleSize)}")
                .Style(ThaiUrText.Base);
        });
    }

    private static void AvfCheck(RowDescriptor r, string label, bool? yes)
    {
        r.AutoItem().AlignMiddle().Text(label).Style(ThaiUrText.UnitText);
        r.ConstantItem(1.5f);
        r.Checkbox(yes == true, sizePt: 6.5f);
        r.ConstantItem(3f);
    }

    private static void PostVitalItem(RowDescriptor r, string label, string value)
    {
        r.RelativeItem().PaddingHorizontal(1f).Row(inner =>
        {
            inner.AutoItem().AlignMiddle().Text($"{label} ").Style(ThaiUrText.Bold);
            inner.RelativeItem().AlignMiddle().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).Style(ThaiUrText.Base);
        });
    }
}

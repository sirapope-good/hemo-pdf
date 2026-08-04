using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Pixel-parity reproduction of the Telerik "Hemodialysis Record" (Hemosheet-ThaiUR.trdp).
/// </summary>
internal sealed class ThaiUrHemosheetForm
{
    private const Unit Mm = Unit.Millimetre;
    private const float Rh = HemosheetThaiUrStyle.RowHeightMm;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    public void Compose(IContainer container, HemosheetReportViewModel vm, PdfReportContext context)
    {
        var mainBandMm = EstimateMainBandHeightMm(vm);
        var prePostMm = ThaiUrHemosheetFooter.ComputePrePostTotalHeightMm(vm, mainBandMm);

        // No single outer Border around the whole column — that blocks paging when a Note
        // grows and fluid+footer must move to page 2 as one block (ShowEntire).
        container
            .DefaultTextStyle(ThaiUrText.Base)
            .Column(page =>
            {
                page.Item().Border(Bw).Column(main =>
                {
                    main.Item().Element(c => Header(c, vm));
                    main.Item().Element(c => TopBand(c, vm));
                    main.Item().Element(c => NursingPlan(c, vm));
                    main.Item().Element(c => DialysisTable(c, vm));
                });
                // Fluid + footer stay together; Pre/Post HD height is pre-budgeted so bottom
                // strips snap to the page bottom without page-level ExtendVertical.
                page.Item().ShowEntire().Border(Bw).Column(bottom =>
                {
                    bottom.Item().Element(c => FluidSummaryRow(c, vm));
                    bottom.Item().Element(c => ThaiUrHemosheetFooter.Compose(c, vm, prePostMm));
                });
            });
    }

    /// <summary>
    /// Content-sized estimate of Header+TopBand+Nursing+Dialysis (mm). Used only to budget
    /// Pre/Post HD expansion; wrapped dialysis notes may add a little extra height in practice.
    /// </summary>
    private static float EstimateMainBandHeightMm(HemosheetReportViewModel vm)
    {
        // Patient meta RowSpan(2) drives header height (5 meta lines).
        const float headerMm = 5f * Rh;
        var topMm = Math.Max(PredialysisTotalHeightMm(), PrescriptionTotalHeightMm());
        var planRows = Math.Max(1, ThaiUrData.NursingPlanRows(vm).Count);
        var nursingMm = HemosheetThaiUrStyle.HeaderBarHeightMm + planRows * Rh;
        var fixedLines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Dialysis, vm.DialysisRecords.Count);
        if (fixedLines <= 0) fixedLines = 8;
        const float unitRowMm = 3.2f;
        var dialysisMm = Rh + unitRowMm + fixedLines * Rh;
        return headerMm + topMm + nursingMm + dialysisMm;
    }

    /// <summary>
    /// Row 1: Logo | Title (spans 2), both confined to this row only with the same explicit height
    /// (TitleHeightMm), so the logo box matches the title box exactly with no leftover empty strip.
    /// Patient info (col 4) uses RowSpan(2) so its ONE box extends down through row 2 as well — it
    /// must never end with a separate bordered-but-empty gap underneath before Predialysis starts.
    /// Row 2: Diagnosis + Drug Allergy (ColumnSpan 3) span Logo + Title's columns only — flush from
    /// the far left edge (under the logo), stopping before the patient-info column so it never eats
    /// that space (patient info's own RowSpan already covers column 4 here).
    /// </summary>
    private static void Header(IContainer c, HemosheetReportViewModel vm)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(48, Mm);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.ConstantColumn(70, Mm);
            });

            t.Cell().Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Element(logo => Logo(logo, vm));

            t.Cell().ColumnSpan(2).Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Text("Hemodialysis Record").Style(ThaiUrText.Title);

            t.Cell().RowSpan(2).Border(Bw).Padding(1f).AlignTop().Column(meta =>
            {
                MetaLine(meta, "Name", vm.Patient.Name, null, null);
                MetaLine(meta, "CN", vm.Patient.Hn, "Age", ThaiUrData.Num(vm.Patient.Age));
                MetaLine(meta, "Coverage", vm.Patient.Coverage, null, null);
                MetaLine(meta, "ID Card NO.", vm.Patient.IdentityNumber, null, null);
                MetaLine(meta, "Date", ThaiUrData.Date(vm.CycleStartTime), "HD NO.", ThaiUrData.Num(vm.TreatmentNo));
            });

            t.Cell().ColumnSpan(3).Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(16, Mm).Label("Diagnosis");
                r.RelativeItem(2).Value(vm.Patient.Diagnosis ?? vm.Patient.Underlying);
                r.ConstantItem(20, Mm).Label("Drug Allergy");
                r.RelativeItem(1).Value(ThaiUrData.Allergies(vm));
            });
        });
    }

    private static void Logo(IContainer c, HemosheetReportViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.LogoBase64))
        {
            try
            {
                var raw = vm.LogoBase64;
                var comma = raw.IndexOf(',');
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    raw = raw[(comma + 1)..];
                c.Image(Convert.FromBase64String(raw)).FitArea();
                return;
            }
            catch
            {
                // fall through
            }
        }

        c.Text(vm.Unit.FullName ?? "").Style(ThaiUrText.Base);
    }

    private static void MetaLine(ColumnDescriptor col, string label, string? value, string? label2, string? value2)
    {
        // No BorderBottom — patient meta sits in one bordered cell without internal row rules.
        col.Item().Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(22, Mm).Text(label).Style(ThaiUrText.Bold);
            r.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).Style(ThaiUrText.Base);
            if (label2 is not null)
            {
                r.ConstantItem(12, Mm).Text(label2).Style(ThaiUrText.Bold);
                r.ConstantItem(14, Mm).Text(string.IsNullOrWhiteSpace(value2) ? "-" : value2).Style(ThaiUrText.Base);
            }
        });
    }

    // Every row in this band is a fixed-length checklist (same count for every patient), so each
    // side's total height is deterministic and can be computed up front from Rh/row counts —
    // no ExtendVertical needed (that fills *all* available space up to the page boundary, which
    // is why the dividers previously ballooned across two extra pages instead of just closing the
    // gap). MinHeight computed from real content gives an exact match with no wasted space, while
    // still letting QuestPDF's normal Table growth take over if any row organically wraps taller.
    private static float PredialysisInnerSplitMm()
    {
        const int vitalsRows = 6; // BP, PR, RR, BT, Sat, Urine
        const int weightsRows = 9;
        const int vascularAccessRows = 8; // shunt site, needle no., A, V, thrill, bruit, edema, inflamation
        var leftColMm = (vitalsRows + SymptomRows.Length) * Rh;
        var rightColMm = weightsRows * Rh + HemosheetThaiUrStyle.HeaderBarHeightMm
            + vascularAccessRows * Rh + HemosheetThaiUrStyle.SectionBreathingMm;
        return Math.Max(leftColMm, rightColMm);
    }

    private static float PredialysisTotalHeightMm() =>
        HemosheetThaiUrStyle.HeaderBarHeightMm + PredialysisInnerSplitMm();

    private static float PrescriptionMachineSplitMm()
    {
        const int leftFixedRows = 6; // Machine, Dialyzer, Surface area, Use No., Last TCV, Grade
        const int leftMinHeightRows = 4; // Test Leak, Disinfectant, Disinfectant test, reason
        const int rightRows = 1 + 3 + 6; // HD/Online + 3 YesNoRow + 6 ValueUnit
        var leftMm = (leftFixedRows + leftMinHeightRows) * Rh;
        var rightMm = rightRows * Rh;
        return Math.Max(leftMm, rightMm);
    }

    private static float AnticoagulantSplitMm()
    {
        const int acRows = 3; // checkbox, loading, maintenance
        const int timeRows = 3; // Time start, Duration, Time off
        var acMm = HemosheetThaiUrStyle.HeaderBarHeightMm + acRows * Rh + HemosheetThaiUrStyle.SectionBreathingMm;
        var timeMm = HemosheetThaiUrStyle.HeaderBarHeightMm
            + timeRows * HemosheetThaiUrStyle.TimeDialysisRowHeightMm + HemosheetThaiUrStyle.SectionBreathingMm;
        return Math.Max(acMm, timeMm);
    }

    /// <summary>
    /// Anticoagulant row must absorb TopBand slack when Predialysis is taller than Prescription.
    /// Otherwise the vertical divider stops at content height and leaves an undivided gap above Nursing Plan.
    /// </summary>
    private static float AnticoagulantFillMm()
    {
        var baseMm = AnticoagulantSplitMm();
        var slackMm = Math.Max(0f, PredialysisTotalHeightMm() - PrescriptionTotalHeightMm());
        return baseMm + slackMm;
    }

    private static float PrescriptionTotalHeightMm() =>
        HemosheetThaiUrStyle.HeaderBarHeightMm + PrescriptionMachineSplitMm() + AnticoagulantSplitMm();

    private static void TopBand(IContainer c, HemosheetReportViewModel vm)
    {
        var splitMm = Math.Max(PredialysisTotalHeightMm(), PrescriptionTotalHeightMm());
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(HemosheetThaiUrStyle.AssessmentColumnWidthMm, Mm);
                cols.RelativeColumn();
            });
            t.Cell().AlignTop().MinHeight(splitMm, Mm).Element(left => Predialysis(left, vm));
            t.Cell().AlignTop().BorderLeft(Bw).MinHeight(splitMm, Mm).Element(right => Prescription(right, vm));
        });
    }

    private static void Predialysis(IContainer c, HemosheetReportViewModel vm)
    {
        var splitMm = PredialysisInnerSplitMm();
        c.Column(col =>
        {
            col.Item().HeaderBar("Predialysys Assessment");
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(58, Mm);
                    cols.RelativeColumn();
                });
                t.Cell().AlignTop().MinHeight(splitMm, Mm).Column(leftCol =>
                {
                    leftCol.Item().Element(v => Vitals(v, vm));
                    leftCol.Item().Element(s => Symptoms(s, vm));
                });
                // Vertical divider only — no per-row horizontal grid inside the section.
                // MinHeight (computed from real row counts) closes this exactly at the taller
                // Vitals+Symptoms column instead of stopping short at its own shorter content.
                t.Cell().AlignTop().BorderLeft(Bw).MinHeight(splitMm, Mm).Column(rightCol =>
                {
                    rightCol.Item().Element(w => Weights(w, vm));
                    rightCol.Item().Element(va => VascularAccess(va, vm));
                });
            });
        });
    }

    private static void Vitals(IContainer c, HemosheetReportViewModel vm)
    {
        var p = vm.PreVital;
        c.Column(col =>
        {
            LabeledValueUnit(col, "BP", ThaiUrData.Bp(p?.Bps, p?.Bpd), "mmHg");
            LabeledValueUnit(col, "PR", ThaiUrData.Num(p?.Hr), "bpm");
            LabeledValueUnit(col, "RR", ThaiUrData.Num(p?.Rr), "bpm");
            LabeledValueUnit(col, "BT", ThaiUrData.Num(p?.Temp), "\u00B0C");
            LabeledValueUnit(col, "Sat", ThaiUrData.Num(p?.SpO2), "%");
            col.Item().Height(Rh, Mm).YesNo("Urine", ThaiUrData.PreOrOtherState(vm, "urine"), labelMm: 14f, yColMm: 14f, nColMm: 14f);
        });
    }

    private static void LabeledValueUnit(ColumnDescriptor col, string label, string value, string unit)
    {
        col.Item().Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(12, Mm).Label(label);
            r.RelativeItem().Value(value);
            r.ConstantItem(10, Mm).AlignMiddle().Text(unit).Style(ThaiUrText.UnitText);
        });
    }

    // Keys: default seed Name + ThaiUR/Telerik short keys (from Hemosheet-ThaiUR.trdp).
    private static readonly (string Label, string[] Keys)[] SymptomRows =
    [
        ("Pale", ["pale"]),
        ("Edema", ["edema"]),
        ("Dyspnea", ["dyspnea", "dys"]),
        ("Fever", ["fever"]),
        ("Crepitatic", ["crep", "crepitatic", "crepitation"]),
        ("Headache", ["head", "headache"]),
        ("Nausea/Vomitting", ["vomit", "nausea", "vomitting"]),
        ("Anorexia", ["ano", "anorexia", "oral"]),
        ("Itching", ["itch", "itching"]),
        ("Engorged neck vein", ["neck", "engorged", "neckvein"]),
        ("Anxiety", ["anxiety", "psycho"]),
        ("Sleep disturbance", ["sleep"]),
        ("Constipation", ["cons", "constipation"]),
        ("Prolong bleeding", ["pbleed", "bleeding", "prolong"]),
    ];

    private static void Symptoms(IContainer c, HemosheetReportViewModel vm)
    {
        // Fixed-height column rows (not a stretching table) so items stay dense like Telerik.
        c.Column(col =>
        {
            foreach (var (label, keys) in SymptomRows)
            {
                col.Item().Height(Rh, Mm).AlignMiddle()
                    .YesNo(label, ThaiUrData.PreState(vm, keys));
            }
        });
    }

    private static void Weights(IContainer c, HemosheetReportViewModel vm)
    {
        var d = vm.Dehydration;
        c.Column(col =>
        {
            LabelValue(col, "Pre BW", ThaiUrData.Kg(d.PreWeight));
            LabelValue(col, "Last BW", ThaiUrData.Kg(d.LastPostWeight));
            LabelValue(col, "Dry weight", ThaiUrData.Kg(vm.DialysisPrescription.DryWeight));
            LabelValue(col, "Meal/Drink", ThaiUrData.Kg(d.FoodIntakeWeight));
            LabelValue(col, "Weight gain (DW)", ThaiUrData.WeightGain(vm));
            LabelValue(col, "Target UF", ThaiUrData.Ml(ThaiUrData.TotalUfMl(vm)));
            LabelValue(col, "Post BW", ThaiUrData.Kg(d.PostWeight));
            LabelValue(col, "Weight loss", ThaiUrData.WeightLoss(vm));
            LabelValue(col, "IDWG", ThaiUrData.Idwg(vm));
        });
    }

    private static void LabelValue(ColumnDescriptor col, string label, string? value, float labelMm = 22f, float? rowHeightMm = null)
    {
        col.Item().Height(rowHeightMm ?? Rh, Mm).Row(r =>
        {
            r.ConstantItem(labelMm, Mm).Label(label);
            r.RelativeItem().Value(value);
        });
    }

    private static void VascularAccess(IContainer c, HemosheetReportViewModel vm)
    {
        // Telerik: header cell only + vertical column splits — no bottom closing box around content.
        c.Column(col =>
        {
            col.Item().HeaderBar("Vascular Access");
            col.Item().Height(Rh, Mm).Value(vm.AvShunt.ShuntSite ?? vm.DialysisPrescription.BloodAccessRoute);
            col.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle()
                .Text("Needle No.").Style(ThaiUrText.Base);
            col.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle()
                .Text($"A {ThaiUrData.Num(vm.AvShunt.ANeedleSize)}").Style(ThaiUrText.Base);
            col.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle()
                .Text($"V {ThaiUrData.Num(vm.AvShunt.VNeedleSize)}").Style(ThaiUrText.Base);
            // Narrow Vascular Access column (~42mm): tighter Y/N columns than symptom panel.
            col.Item().Height(Rh, Mm).YesNo("Thrill", ThaiUrData.PreState(vm, "thrill", "vas:av:thrill"), 14f, 13f, 13f);
            col.Item().Height(Rh, Mm).YesNo("Bruit", ThaiUrData.PreState(vm, "bruit", "vas:av:bruit"), 14f, 13f, 13f);
            col.Item().Height(Rh, Mm).YesNo("Edema", ThaiUrData.PreState(vm, "edema", "vas:edema"), 14f, 13f, 13f);
            col.Item().Height(Rh, Mm).YesNo("Inflamation", ThaiUrData.PreState(vm, "inf", "inflame", "inflamation", "vas:inflammation"), 14f, 13f, 13f);
            col.Item().Height(HemosheetThaiUrStyle.SectionBreathingMm, Mm);
        });
    }

    private static void Prescription(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        var leftMm = HemosheetThaiUrStyle.PrescriptionLeftColumnWidthMm;
        var machineSplitMm = PrescriptionMachineSplitMm();
        c.Column(col =>
        {
            col.Item().HeaderBar("Hemodialysis Prescription");
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(leftMm, Mm);
                    cols.RelativeColumn();
                });
                t.Cell().MinHeight(machineSplitMm, Mm).Column(left =>
                {
                    LabelValue(left, "Machine", vm.Bed, 17f);
                    LabelValue(left, "Dialyzer", pr.Dialyzer, 17f);
                    LabelValue(left, "Surface area", pr.DialyzerSurfaceArea is not null ? $"{ThaiUrData.Num(pr.DialyzerSurfaceArea)} m\u00B2" : "-", 17f);
                    LabelValue(left, "Use No.", "New", 17f);
                    LabelValue(left, "Last TCV", "-", 17f);
                    LabelValue(left, "Grade", "-", 17f);
                    PassRow(left, "Test Leak");
                    left.Item().MinHeight(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(20, Mm).Label("Disinfectant");
                        r.Checkbox(false);
                        r.ConstantItem(1.5f);
                        r.AutoItem().AlignMiddle().Text("Peracitic acid").Style(ThaiUrText.Base);
                        r.RelativeItem();
                    });
                    PassRow(left, "Disinfectant test");
                    left.Item().MinHeight(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(34, Mm).Label("สาเหตุการทิ้งตัวกรอง");
                        r.RelativeItem().Value(ThaiUrData.OtherText(vm, "reason"));
                    });
                });
                t.Cell().BorderLeft(Bw).MinHeight(machineSplitMm, Mm).Column(right =>
                {
                    right.Item().Height(Rh, Mm).Row(r =>
                    {
                        r.Checkbox(string.Equals(pr.Mode, "HD", StringComparison.OrdinalIgnoreCase));
                        r.ConstantItem(1.5f);
                        r.ConstantItem(12, Mm).AlignMiddle().Text("HD").Style(ThaiUrText.Base);
                        r.Checkbox(string.Equals(pr.Mode, "HDF", StringComparison.OrdinalIgnoreCase) || string.Equals(pr.Mode, "OHDF", StringComparison.OrdinalIgnoreCase));
                        r.ConstantItem(1.5f);
                        r.RelativeItem().AlignMiddle().Text("Online").Style(ThaiUrText.Base);
                    });
                    YesNoRow(right, "Na. Profile", null);
                    YesNoRow(right, "UF. Profile", null);
                    YesNoRow(right, "Isolate", null);
                    ValueUnit(right, "Na", ThaiUrData.Num(pr.DialysateNa), "mEq/L");
                    ValueUnit(right, "K+", ThaiUrData.Num(pr.DialysateK), "mEq/L");
                    ValueUnit(right, "Ca2+", ThaiUrData.Num(pr.DialysateCa), "mEq/L");
                    ValueUnit(right, "HCO3", ThaiUrData.Num(pr.DialysateHco3), "mEq/L");
                    ValueUnit(right, "Dialysis Flow", ThaiUrData.Num(pr.DialysateFlowRate), "ml/min");
                    ValueUnit(right, "Dialysis Temp", ThaiUrData.Num(pr.DialysateTemperature), "\u2103");
                });
            });
            // Fill leftover TopBand height so Anticoagulant|Time Dialysis divider reaches Nursing Plan.
            col.Item().Element(a => Anticoagulant(a, vm));
        });
    }

    private static void PassRow(ColumnDescriptor col, string label)
    {
        col.Item().MinHeight(Rh, Mm).Row(r =>
        {
            r.ConstantItem(20, Mm).Label(label);
            r.Checkbox(false);
            r.ConstantItem(1.5f);
            r.AutoItem().AlignMiddle().Text("Pass").Style(ThaiUrText.Base);
            r.ConstantItem(3f);
            r.Checkbox(false);
            r.ConstantItem(1.5f);
            r.AutoItem().AlignMiddle().Text("Not Pass").Style(ThaiUrText.Base);
            r.RelativeItem();
        });
    }

    private static void YesNoRow(ColumnDescriptor col, string label, bool? yes)
    {
        col.Item().Height(Rh, Mm).PaddingLeft(1f).Row(r =>
        {
            r.ConstantItem(18, Mm).Label(label);
            r.Checkbox(yes == true);
            r.ConstantItem(2.5f);
            r.AutoItem().AlignMiddle().Text("Yes").Style(ThaiUrText.Base);
            r.ConstantItem(6f);
            r.Checkbox(yes == false);
            r.ConstantItem(2.5f);
            r.AutoItem().AlignMiddle().Text("No").Style(ThaiUrText.Base);
            r.RelativeItem();
        });
    }

    private static void ValueUnit(ColumnDescriptor col, string label, string value, string unit)
    {
        col.Item().Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(20, Mm).Label(label);
            r.RelativeItem().Value(value);
            r.ConstantItem(12, Mm).AlignMiddle().Text(unit).Style(ThaiUrText.UnitText);
        });
    }

    private static void Anticoagulant(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        var leftMm = HemosheetThaiUrStyle.PrescriptionLeftColumnWidthMm;
        var timeRh = HemosheetThaiUrStyle.TimeDialysisRowHeightMm;
        // Include TopBand slack (Predialysis taller than Prescription) so BorderLeft reaches Nursing Plan.
        var fillMm = AnticoagulantFillMm();
        // Vertical divider only (same 51mm split as Prescription) — no top/bottom closing box.
        // Table + MinHeight fills Predialysis slack so the divider does not stop mid-gap.
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(leftMm, Mm);
                cols.RelativeColumn();
            });
            t.Cell().MinHeight(fillMm, Mm).Column(col =>
            {
                col.Item().HeaderBar("Anticoagulant");
                col.Item().Height(Rh, Mm).Row(r =>
                {
                    r.Checkbox(vm.IsAcNotUsed);
                    r.ConstantItem(1.5f);
                    r.RelativeItem().AlignMiddle().Text("No Heparin").Style(ThaiUrText.Base);
                });
                col.Item().Height(Rh, Mm).Value($"Loading: {ThaiUrData.Ml(pr.InitialAmountMl)}");
                col.Item().Height(Rh, Mm).Value($"Maintenance: {ThaiUrData.Ml(pr.MaintainAmountMl)}");
            });
            t.Cell().BorderLeft(Bw).MinHeight(fillMm, Mm).Column(col =>
            {
                col.Item().HeaderBar("Time Dialysis");
                LabelValue(col, "Time start", ThaiUrData.Time(vm.CycleStartTime), 14f, timeRh);
                LabelValue(col, "Duration", pr.DurationHours is not null ? $"{ThaiUrData.Num(pr.DurationHours)} Hours" : "-", 14f, timeRh);
                LabelValue(col, "Time off", ThaiUrData.Time(vm.CycleEndTime), 14f, timeRh);
            });
        });
    }

    private static void NursingPlan(IContainer c, HemosheetReportViewModel vm)
    {
        // Source: Nurse Processing (Progress Notes) — Focus / I / E, not assessments.Other.
        var planRows = ThaiUrData.NursingPlanRows(vm);
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(69.55f);
                cols.RelativeColumn(60.85f);
                cols.RelativeColumn(75.44f);
            });
            t.Cell().HeaderBar("Nursing Diagnosis");
            t.Cell().HeaderBar("Nursing Intervention");
            t.Cell().HeaderBar("Expected Outcomes");

            foreach (var (diagnosis, intervention, outcome) in planRows)
            {
                // Single-line rows stay near Rh; multi-line cells still grow past MinHeight.
                t.Cell().Border(Bw).MinHeight(Rh, Mm).ValueBlank(diagnosis);
                t.Cell().Border(Bw).MinHeight(Rh, Mm).ValueBlank(intervention);
                t.Cell().Border(Bw).MinHeight(Rh, Mm).ValueBlank(outcome);
            }
        });
    }

    // Leading numeric columns (Time…UFR). Total UF width = sum(leading)/4 so each of the five
    // fluid-summary cells under the fixed band matches the Total UF column exactly.
    private static readonly (string Head, string Unit, float Mm)[] DialysisLeadingColumns =
    [
        ("Time", "", 12f), ("BP", "mmHg", 15f), ("MAP", "mmHg", 10f), ("Pulse", "/min", 10f),
        ("EBFR", "ml/min", 11f), ("AP", "mmHg", 10f), ("VP", "mmHg", 10f), ("TMP", "mmHg", 10f),
        ("Cond.", "mS/cm", 12f), ("UFR", "ml/hr", 12f),
    ];

    private static readonly (string Head, string Unit, float Mm)[] DialysisColumns = BuildDialysisColumns();

    private static (string Head, string Unit, float Mm)[] BuildDialysisColumns()
    {
        var leadingSumMm = DialysisLeadingColumns.Sum(c => c.Mm);
        var totalUfMm = leadingSumMm / 4f;
        return [.. DialysisLeadingColumns, ("Total UF", "ml", totalUfMm)];
    }

    private static void DialysisTable(IContainer c, HemosheetReportViewModel vm)
    {
        var fixedLines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Dialysis, vm.DialysisRecords.Count);
        if (fixedLines <= 0) fixedLines = 8;

        c.DefaultTextStyle(ThaiUrText.Dialysis).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var col in DialysisColumns)
                    cols.ConstantColumn(col.Mm, Mm);
                cols.RelativeColumn();
            });

            // Header row 1: column names
            foreach (var col in DialysisColumns)
            {
                t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground)
                    .AlignCenter().AlignMiddle().Height(Rh, Mm)
                    .Text(col.Head).Style(ThaiUrText.DialysisBold);
            }
            t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground)
                .AlignCenter().AlignMiddle().Height(Rh, Mm)
                .Text("Note").Style(ThaiUrText.DialysisBold);

            // Header row 2: units (separate cells with horizontal divider)
            foreach (var col in DialysisColumns)
            {
                t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground)
                    .AlignCenter().AlignMiddle().Height(3.2f, Mm)
                    .Text(col.Unit).Style(ThaiUrText.DialysisUnit);
            }
            t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground)
                .Height(3.2f, Mm);

            for (var i = 0; i < fixedLines; i++)
            {
                var rec = i < vm.DialysisRecords.Count ? vm.DialysisRecords[i] : null;
                var cells = new[]
                {
                    ThaiUrData.Time(rec?.Timestamp),
                    rec is null ? "" : ThaiUrData.Bp(rec.Bps, rec.Bpd),
                    rec is null ? "" : (ThaiUrData.Map(rec.Bps, rec.Bpd) ?? ""),
                    ThaiUrData.Num(rec?.Hr),
                    ThaiUrData.Num(rec?.Bfr),
                    "",
                    ThaiUrData.Num(rec?.Vp),
                    ThaiUrData.Num(rec?.Tmp),
                    ThaiUrData.Num(rec?.Dc),
                    rec?.UfRate is not null ? ThaiUrData.Num(rec.UfRate * 1000) : "",
                    rec?.UfTotal is not null ? ThaiUrData.Num(rec.UfTotal * 1000) : "",
                };
                foreach (var value in cells)
                {
                    t.Cell().Border(Bw).MinHeight(Rh, Mm).AlignMiddle().AlignCenter()
                        .Text(string.IsNullOrWhiteSpace(value) ? "" : value).Style(ThaiUrText.Dialysis);
                }
                // Grow with wrapped note (up to DialysisNoteMaxLines); sibling cells MinHeight so the row expands together.
                t.Cell().Border(Bw).MinHeight(Rh, Mm).PaddingHorizontal(1f).PaddingVertical(0.5f).AlignMiddle()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(ThaiUrText.Dialysis);
                        text.ClampLines(HemosheetThaiUrStyle.DialysisNoteMaxLines, "\u2026");
                        text.Span(rec?.Note ?? "");
                    });
            }
        });
    }

    /// <summary>
    /// Horizontal fluid boxes under the dialysis record table.
    /// First 5 cells are equal to the Total UF column width (leading cols sum / 4); last cell
    /// (Net fluid balance) uses the remaining Relative width — same as the Note column above.
    /// </summary>
    private static void FluidSummaryRow(IContainer c, HemosheetReportViewModel vm)
    {
        var boxes = new (string Label, string Value)[]
        {
            ("NSS", ThaiUrData.Ml(ThaiUrData.NssMl(vm))),
            ("50% Glucose", "-"),
            ("Extra-fluid", ThaiUrData.Ml(ThaiUrData.ExtraFluidMl(vm))),
            ("Total fluid replacment", "-"),
            ("Total UF", ThaiUrData.Ml(ThaiUrData.TotalUfMl(vm))),
            ("Net fluid balance", ThaiUrData.Ml(ThaiUrData.NetFluidBalanceMl(vm))),
        };

        // Equals DialysisColumns[^1].Mm (= leading sum / 4) so Total UF cells share one vertical rule.
        var totalUfColMm = DialysisColumns[^1].Mm;

        c.DefaultTextStyle(ThaiUrText.Dialysis).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < 5; i++)
                    cols.ConstantColumn(totalUfColMm, Mm);
                cols.RelativeColumn();
            });

            foreach (var (label, value) in boxes)
            {
                t.Cell().Border(Bw).MinHeight(6.5f, Mm).Padding(1f).AlignCenter().Column(cc =>
                {
                    cc.Item().AlignCenter().Text(label).Style(ThaiUrText.DialysisBold);
                    cc.Item().AlignCenter().Text(value).Style(ThaiUrText.Dialysis);
                });
            }
        });
    }

}

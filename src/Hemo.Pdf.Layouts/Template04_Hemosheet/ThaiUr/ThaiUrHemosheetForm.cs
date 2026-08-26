using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Sections.Hemosheet;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Dense Hemodialysis Record form for ThaiUR override (purple section bars).
/// Pixel-parity baseline: Telerik Hemosheet-ThaiUR.trdp.
/// Default profile uses <c>DefaultHemosheetForm</c> (CICM-style) instead.
/// </summary>
internal sealed class ThaiUrHemosheetForm
{
    private const Unit Mm = Unit.Millimetre;
    private const float Rh = HemosheetThaiUrStyle.RowHeightMm;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    public void Compose(IContainer container, HemosheetReportViewModel vm, PdfReportContext context, HprpPackage? package = null)
    {
        // Dialysis rows absorb leftover page space so the footer band (checks + Nephrologist |
        // notes + Post Vital…NA) sits flush at the bottom of page 1 as one unit.
        //
        // Do NOT wrap header+top+nursing+dialysis in one Border().Column — QuestPDF then orphans
        // page 1 as an empty bordered box when the stack overflows (Studio sample looks "เพี้ยน"
        // vs the normal generate path that often fits). Same rule as DefaultHemosheetForm.
        var notesFloorMm = ThaiUrHemosheetFooter.NurseNotesFloorHeightMm(vm);
        var aboveDialysisMm = EstimateAboveDialysisMm(vm);
        var bottomFloorMm = ThaiUrHemosheetFooter.BottomBlockHeightMm(vm, notesFloorMm);
        var dialysisRows = BudgetDialysisRows(vm, aboveDialysisMm, bottomFloorMm);
        var dialysisHeaders = HprpHemosheetPlanInterpreter.TryDialysisHeaders(package, vm);
        var dialysisFill = HprpChrome.FileHeaderFillOrNull(HprpHemosheetPlanInterpreter.TryDialysisChrome(package));

        container
            .DefaultTextStyle(ThaiUrText.Base)
            .Column(page =>
            {
                page.Item().Element(c => ThaiUrReportHeader.Compose(c, vm));
                page.Item().Border(Bw).Element(c => TopBand(c, vm));
                page.Item().Element(c => NursingPlan(c, vm));
                page.Item().Element(c =>
                {
                    if (dialysisFill is null)
                    {
                        DialysisTable(c, vm, Math.Max(dialysisRows, vm.DialysisRecords.Count), dialysisHeaders);
                        return;
                    }

                    using (ReportSectionHeaderChrome.Begin(dialysisFill))
                        DialysisTable(c, vm, Math.Max(dialysisRows, vm.DialysisRecords.Count), dialysisHeaders);
                });
                page.Item().Border(Bw).Column(mid =>
                {
                    mid.Item().Element(c => FluidSummaryRow(c, vm));
                    mid.Item().Element(c => ThaiUrHemosheetFooter.ComposeBand(c, vm));
                });
            });
    }

    /// <summary>Header + TopBand + Nursing Plan (no dialysis) — mm.</summary>
    private static float EstimateAboveDialysisMm(HemosheetReportViewModel vm)
    {
        var headerMm = HemosheetThaiUrStyle.TitleHeightMm + HemosheetThaiUrStyle.MetaRowHeightMm;
        var topMm = Math.Max(PredialysisTotalHeightMm(), PrescriptionTotalHeightMm());
        var planRows = ThaiUrData.NursingPlanRows(vm);
        // Cells grow past Rh when Focus/I/E wrap — budget with a conservative 2-line floor per row.
        var nursingBodyMm = 0f;
        foreach (var (diagnosis, intervention, outcome) in planRows)
        {
            var lines = Math.Max(
                EstimateWrappedLines(diagnosis),
                Math.Max(EstimateWrappedLines(intervention), EstimateWrappedLines(outcome)));
            nursingBodyMm += Math.Max(Rh, lines * Rh);
        }

        var nursingMm = HemosheetThaiUrStyle.HeaderBarHeightMm + nursingBodyMm;
        // PreVital / filled cells can render taller than fixed-Rh estimates (QuestPDF table growth).
        const float topBandContentSlackMm = 12f;
        return headerMm + topMm + nursingMm + topBandContentSlackMm;
    }

    private static int EstimateWrappedLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 1;
        // Nursing plan columns are ~60–75mm wide at 7.5pt — ~28 Thai/Latin chars per line.
        const int charsPerLine = 28;
        return Math.Clamp((int)Math.Ceiling(text.Trim().Length / (double)charsPerLine), 1, 4);
    }

    /// <summary>
    /// Dialysis rows fill leftover page space. Always keep at least one blank beyond real records
    /// (overridden when FixedLines / budget needs more).
    /// </summary>
    private static int BudgetDialysisRows(
        HemosheetReportViewModel vm,
        float aboveDialysisMm,
        float bottomFloorMm)
    {
        var pageContentMm = 297f
            - 2f * HemosheetThaiUrStyle.PageMarginMm
            - ThaiUrHemosheetFooter.PageNumberFooterMm;

        var showHdf = HemosheetDialysisColumns.ShowHdf(vm);
        var dialysisHeaderMm = HemosheetDialysisColumns.HeaderHeightMm(showHdf, Rh);
        var availableForDialysisMm = pageContentMm
            - ThaiUrHemosheetFooter.LayoutSafetyMm
            - aboveDialysisMm
            - bottomFloorMm;

        // Negative / zero space → no blank padding rows (still paint real records below).
        var maxRowsBySpace = (int)Math.Floor((availableForDialysisMm - dialysisHeaderMm) / Rh);
        if (maxRowsBySpace < 0)
            maxRowsBySpace = 0;

        var dataRows = vm.DialysisRecords.Count;
        // Prefer one trailing blank when space allows; never invent blanks that shove the footer
        // to page 2 (Studio sample + PreVital used to tip over with a forced blank row).
        var desired = Math.Max(
            vm.LayoutContext.ReportSettings.FixedLines.Dialysis,
            dataRows == 0 ? 0 : dataRows + 1);
        if (desired <= 0)
            desired = dataRows > 0 ? dataRows : Math.Min(8, maxRowsBySpace);

        // Paint every real record even when that needs a 2nd page; otherwise clamp to page budget.
        if (dataRows > maxRowsBySpace)
            return dataRows;

        return Math.Min(desired, maxRowsBySpace);
    }

    // Every row in this band is a fixed-length checklist (same count for every patient), so each
    // side's total height is deterministic and can be computed up front from Rh/row counts เนโฌโ€
    // no ExtendVertical needed (that fills *all* available space up to the page boundary, which
    // is why the dividers previously ballooned across two extra pages instead of just closing the
    // gap). MinHeight computed from real content gives an exact match with no wasted space, while
    // still letting QuestPDF's normal Table growth take over if any row organically wraps taller.
    private static float PredialysisInnerSplitMm()
    {
        const int vitalsRows = 5; // BP, PR+RR, BT+Sat, Urine, Pain score
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
                // Vertical divider only เนโฌโ€ no per-row horizontal grid inside the section.
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
        // Border under Pain score — one vitals block (Telerik TopLeftPanel) above the symptom list.
        // IMPORTANT: do NOT nest Row inside RelativeItem — QuestPDF then under-constrains width,
        // clips BP (e.g. "177/") and drops subsequent rows, leaving a large empty left column.
        c.BorderBottom(Bw).Column(col =>
        {
            VitalHalfRow(col, "BP", ThaiUrData.Bp(p?.Bps, p?.Bpd), "mmHg", null, null, null);
            VitalHalfRow(col, "PR", ThaiUrData.Num(p?.Hr), "bpm", "RR", ThaiUrData.Num(p?.Rr), "bpm");
            VitalHalfRow(col, "BT", ThaiUrData.Num(p?.Temp), "\u00B0C", "Sat", ThaiUrData.Num(p?.SpO2), "%");
            col.Item().Height(Rh, Mm).YesNoValue(
                "Urine",
                ThaiUrData.PreOrOtherState(vm, "urine"),
                ThaiUrData.PreOrOtherText(vm, "urine"),
                "ml/day");
            col.Item().Height(Rh, Mm).YesNoValue(
                "Pain score",
                ThaiUrData.PreOrOtherState(vm, "pain"),
                ThaiUrData.PreOrOtherText(vm, "pain"),
                "point");
        });
    }

    /// <summary>
    /// One vitals row as a flat 6-column table (left pair | right pair).
    /// BP uses left pair only so mmHg aligns with PR/BT units.
    /// </summary>
    private static void VitalHalfRow(
        ColumnDescriptor col,
        string label1, string value1, string unit1,
        string? label2, string? value2, string? unit2)
    {
        col.Item().Height(Rh, Mm).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(10, Mm);
                c.RelativeColumn();
                c.ConstantColumn(9, Mm);
                c.ConstantColumn(10, Mm);
                c.RelativeColumn();
                c.ConstantColumn(9, Mm);
            });

            t.Cell().AlignMiddle().PaddingLeft(1f).Text(label1).Style(ThaiUrText.Base);
            t.Cell().AlignMiddle().PaddingLeft(1f).Text(text =>
            {
                text.DefaultTextStyle(ThaiUrText.Base);
                text.ClampLines(1, "\u2026");
                text.Span(string.IsNullOrWhiteSpace(value1) ? "-" : value1);
            });
            t.Cell().AlignMiddle().Text(unit1).Style(ThaiUrText.UnitText);

            if (label2 is null)
            {
                t.Cell();
                t.Cell();
                t.Cell();
            }
            else
            {
                t.Cell().AlignMiddle().PaddingLeft(1f).Text(label2).Style(ThaiUrText.Base);
                t.Cell().AlignMiddle().PaddingLeft(1f).Text(text =>
                {
                    text.DefaultTextStyle(ThaiUrText.Base);
                    text.ClampLines(1, "\u2026");
                    text.Span(string.IsNullOrWhiteSpace(value2) ? "-" : value2);
                });
                t.Cell().AlignMiddle().Text(unit2!).Style(ThaiUrText.UnitText);
            }
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
        // Telerik: header cell only + vertical column splits เนโฌโ€ no bottom closing box around content.
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
                    left.Item().Height(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(20, Mm).Label("Disinfectant");
                        r.Checkbox(false);
                        r.ConstantItem(1.5f);
                        r.AutoItem().AlignMiddle().Text("Peracitic acid").Style(ThaiUrText.Base);
                        r.RelativeItem();
                    });
                    PassRow(left, "Disinfectant test");
                    // One clamped line — long Thai label must not wrap past Rh or escape the machine cell.
                    left.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle().Text(text =>
                    {
                        text.DefaultTextStyle(ThaiUrText.Base);
                        text.ClampLines(1, "\u2026");
                        text.Span("\u0e2a\u0e32\u0e40\u0e2b\u0e15\u0e38\u0e01\u0e32\u0e23\u0e17\u0e34\u0e49\u0e07\u0e15\u0e31\u0e27\u0e01\u0e23\u0e2d\u0e07 ");
                        text.Span(ThaiUrData.OtherText(vm, "reason") ?? "-");
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
        col.Item().Height(Rh, Mm).Row(r =>
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
        // Vertical divider only (same 51mm split as Prescription) เนโฌโ€ no top/bottom closing box.
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
        // Source: Nurse Processing (Progress Notes) เนโฌโ€ Focus / I / E, not assessments.Other.
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

    private static void DialysisTable(
        IContainer c,
        HemosheetReportViewModel vm,
        int fixedLines,
        IReadOnlyList<string>? fileHeaders)
    {
        // Row count is owned by BudgetDialysisRows — do not re-inflate 0 → 8 (that orphans the footer).
        if (fixedLines < 0) fixedLines = 0;
        var showHdf = HemosheetDialysisColumns.ShowHdf(vm);
        var colMm = HemosheetDialysisColumns.DataColumnWidthMm(showHdf);
        var useFileHeaders = fileHeaders is { Count: > 0 };
        var headerMm = HemosheetDialysisColumns.HeaderHeightMm(showHdf && !useFileHeaders, Rh);
        var baseDefs = HemosheetDialysisColumns.BaseColumnDefs;
        var insertAfter = HemosheetDialysisColumns.SubstituteInsertAfterIndex;

        c.DefaultTextStyle(ThaiUrText.Dialysis).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < HemosheetDialysisColumns.DataColumnCount(showHdf); i++)
                    cols.ConstantColumn(colMm, Mm);
                cols.RelativeColumn();
            });

            if (useFileHeaders)
            {
                for (var i = 0; i < fileHeaders!.Count - 1; i++)
                    DialysisHeaderCell(t, fileHeaders[i], "", headerMm, rowSpan: 1);

                t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text(fileHeaders[^1]).Style(ThaiUrText.DialysisBold);
            }
            else if (showHdf)
            {
                for (var i = 0; i <= insertAfter; i++)
                    DialysisHeaderCell(t, baseDefs[i].Head, baseDefs[i].Unit, headerMm, rowSpan: 2);

                t.Cell().ColumnSpan(2).Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                    .Height(headerMm * 0.55f, Mm).AlignCenter().AlignMiddle()
                    .Column(cc =>
                    {
                        cc.Item().AlignCenter().Text("Substitute").Style(ThaiUrText.DialysisBold);
                        cc.Item().AlignCenter().Text("(L/hr)").Style(ThaiUrText.DialysisUnit);
                    });

                for (var i = insertAfter + 1; i < baseDefs.Length; i++)
                    DialysisHeaderCell(t, baseDefs[i].Head, baseDefs[i].Unit, headerMm, rowSpan: 2);

                t.Cell().RowSpan(2).Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text("Note").Style(ThaiUrText.DialysisBold);

                foreach (var sub in new[] { "total", "rate" })
                {
                    t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                        .Height(headerMm * 0.45f, Mm).AlignCenter().AlignMiddle()
                        .Text(sub).Style(ThaiUrText.DialysisUnit);
                }
            }
            else
            {
                foreach (var (head, unit) in baseDefs)
                    DialysisHeaderCell(t, head, unit, headerMm, rowSpan: 1);

                t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text("Note").Style(ThaiUrText.DialysisBold);
            }

            for (var i = 0; i < fixedLines; i++)
            {
                var rec = i < vm.DialysisRecords.Count ? vm.DialysisRecords[i] : null;
                foreach (var value in HemosheetDialysisColumns.CellValues(rec, showHdf))
                {
                    t.Cell().Border(Bw).Height(Rh, Mm).AlignMiddle().AlignCenter()
                        .Text(text =>
                        {
                            text.DefaultTextStyle(ThaiUrText.Dialysis);
                            text.ClampLines(1, "\u2026");
                            text.Span(value ?? "");
                        });
                }
                t.Cell().Border(Bw).Height(Rh, Mm).PaddingHorizontal(1f).AlignMiddle()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(ThaiUrText.Dialysis);
                        text.ClampLines(1, "\u2026");
                        text.Span(rec?.Note ?? "");
                    });
            }
        });
    }

    private static void DialysisHeaderCell(
        TableDescriptor t,
        string head,
        string unit,
        float headerMm,
        uint rowSpan)
    {
        var cell = t.Cell();
        if (rowSpan > 1)
            cell = cell.RowSpan(rowSpan);

        cell.Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
            .Height(headerMm, Mm).AlignCenter().AlignMiddle()
            .Column(cc =>
            {
                cc.Item().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(ThaiUrText.DialysisBold);
                    text.ClampLines(1, "\u2026");
                    text.Span(head);
                });
                if (!string.IsNullOrEmpty(unit))
                {
                    cc.Item().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(ThaiUrText.DialysisUnit);
                        text.ClampLines(1, "\u2026");
                        text.Span(unit);
                    });
                }
            });
    }

    /// <summary>
    /// Fluid boxes under the dialysis table, sharing the same column grid.
    /// HDF widens Extra-fluid to absorb the two Substitute columns.
    /// </summary>
    private static void FluidSummaryRow(IContainer c, HemosheetReportViewModel vm)
    {
        var showHdf = HemosheetDialysisColumns.ShowHdf(vm);
        var colMm = HemosheetDialysisColumns.DataColumnWidthMm(showHdf);
        var boxes = HemosheetDialysisColumns.FluidBoxes(vm, showHdf);

        c.DefaultTextStyle(ThaiUrText.Dialysis).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < HemosheetDialysisColumns.DataColumnCount(showHdf); i++)
                    cols.ConstantColumn(colMm, Mm);
                cols.RelativeColumn();
            });

            foreach (var (span, label, value) in boxes)
            {
                t.Cell().ColumnSpan((uint)span).Border(Bw).Height(ThaiUrHemosheetFooter.FluidSummaryHeightMm, Mm).Padding(1f)
                    .AlignMiddle().AlignCenter()
                    .Column(cc =>
                    {
                        cc.Item().AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(ThaiUrText.DialysisBold);
                            text.ClampLines(1, "\u2026");
                            text.Span(label);
                        });
                        cc.Item().AlignCenter().Text(text =>
                        {
                            text.DefaultTextStyle(ThaiUrText.Dialysis);
                            text.ClampLines(1, "\u2026");
                            text.Span(value);
                        });
                    });
            }
        });
    }

}
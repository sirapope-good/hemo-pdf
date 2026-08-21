using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Sections.Default;
using Hemo.Pdf.Sections.Hemosheet;
using ThaiUrDataHelper = Hemo.Pdf.Sections.ThaiUr.ThaiUrData;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.Default;

/// <summary>
/// clinical-03 Default Hemodialysis Record (CICM-style).
/// One A4 page when dialysis/notes stay within budget; long dynamic series may paginate.
/// </summary>
internal sealed class DefaultHemosheetForm
{
    private const Unit Mm = Unit.Millimetre;
    private const float Rh = HemosheetDefaultStyle.RowHeightMm;
    private const float Bw = HemosheetDefaultStyle.BorderWidth;

    public void Compose(IContainer container, HemosheetReportViewModel vm, PdfReportContext context, HprpPackage? package = null)
    {
        // Dialysis rows absorb leftover page space so the footer band sits flush on page 1.
        // Do NOT wrap header+body in one Border().Column — QuestPDF then orphans the header
        // on page 1 inside an empty bordered box when the stack overflows.
        var notesFloorMm = DefaultHemosheetFooter.NurseNotesFloorHeightMm(vm);
        var aboveDialysisMm = EstimateAboveDialysisMm(vm);
        var bottomFloorMm = PrePostVitalsHeightMm
            + DefaultHemosheetFooter.BottomBlockHeightMm(vm, notesFloorMm);
        var dialysisRows = BudgetDialysisRows(vm, aboveDialysisMm, bottomFloorMm);
        var dialysisHeaders = HprpHemosheetPlanInterpreter.TryDialysisHeaders(package, vm);
        var dialysisFill = HprpChrome.FileHeaderFillOrNull(HprpHemosheetPlanInterpreter.TryDialysisChrome(package));

        container
            .DefaultTextStyle(DefaultText.Base)
            .Column(page =>
            {
                page.Item().Element(c => DefaultReportHeader.Compose(c, vm));
                page.Item().Border(Bw).Element(c => TopBand(c, vm));
                page.Item().Element(c =>
                {
                    if (dialysisFill is null)
                    {
                        DialysisTable(c, vm, dialysisRows, dialysisHeaders);
                        return;
                    }

                    using (ReportSectionHeaderChrome.Begin(dialysisFill))
                        DialysisTable(c, vm, dialysisRows, dialysisHeaders);
                });
                page.Item().Element(c => PrePostVitalsRow(c, vm));
                page.Item().Element(c => FluidSummaryRow(c, vm));
                page.Item().Element(c => DefaultHemosheetFooter.ComposeBand(c, vm));
            });
    }

    /// <summary>Header + TopBand only (CICM has no ThaiUR Nursing Diagnosis band).</summary>
    private static float EstimateAboveDialysisMm(HemosheetReportViewModel vm)
    {
        var headerMm = HemosheetDefaultStyle.TitleHeightMm;
        var topMm = Math.Max(PredialysisTotalHeightMm(), PrescriptionTotalHeightMm());
        return headerMm + topMm;
    }

    /// <summary>
    /// Fill leftover page space with dialysis rows when the form can stay on one A4 page.
    /// Never force more blank rows than fit — that caused header-only page 1 + spill to page 2.
    /// Long real dialysis series may still span pages (dynamic expansion).
    /// </summary>
    private static int BudgetDialysisRows(
        HemosheetReportViewModel vm,
        float aboveDialysisMm,
        float bottomFloorMm)
    {
        var pageContentMm = 297f
            - 2f * HemosheetDefaultStyle.PageMarginMm
            - DefaultHemosheetFooter.PageNumberFooterMm;

        var showHdf = HemosheetDialysisColumns.ShowHdf(vm);
        var dialysisHeaderMm = HemosheetDialysisColumns.HeaderHeightMm(showHdf, Rh);
        var availableForDialysisMm = pageContentMm
            - DefaultHemosheetFooter.LayoutSafetyMm
            - aboveDialysisMm
            - bottomFloorMm;

        var maxRowsBySpace = (int)Math.Floor((availableForDialysisMm - dialysisHeaderMm) / Rh);
        if (maxRowsBySpace < 1)
            maxRowsBySpace = 1;

        var dataRows = vm.DialysisRecords.Count;
        var desired = Math.Max(
            vm.LayoutContext.ReportSettings.FixedLines.Dialysis,
            dataRows + 1);
        if (desired <= 0)
            desired = 8;

        // Must paint every real record (may multi-page). Otherwise clamp to page budget.
        if (dataRows > maxRowsBySpace)
            return dataRows;

        return Math.Min(desired, maxRowsBySpace);
    }

    private const float PrePostVitalsHeightMm = 6.5f;

    // CICM left band: Topic | Assessment (Y/N only) + Volume Assessment — no BP/PR/RR/BT block.
    private static float PredialysisInnerSplitMm()
    {
        var topicMm = TopicRows.Length * Rh;
        // Volume lines + Outcome header + post weights + access checks + high-risk line.
        const int volumeDataRows = 7; // Pre HD BW … UF Goal
        const int outcomeRows = 2; // Post HD BW, Weight loss
        const int accessRows = 3; // Inflammation, Thrill, Bruit
        const int riskRows = 1;
        var volumeMm = volumeDataRows * Rh
            + HemosheetDefaultStyle.HeaderBarHeightMm
            + outcomeRows * Rh
            + accessRows * Rh
            + riskRows * Rh
            + HemosheetDefaultStyle.SectionBreathingMm;
        return Math.Max(topicMm, volumeMm);
    }

    private static float PredialysisTotalHeightMm() =>
        HemosheetDefaultStyle.HeaderBarHeightMm + PredialysisInnerSplitMm();

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
        var acMm = HemosheetDefaultStyle.HeaderBarHeightMm + acRows * Rh + HemosheetDefaultStyle.SectionBreathingMm;
        var timeMm = HemosheetDefaultStyle.HeaderBarHeightMm
            + timeRows * HemosheetDefaultStyle.TimeDialysisRowHeightMm + HemosheetDefaultStyle.SectionBreathingMm;
        return Math.Max(acMm, timeMm);
    }

    /// <summary>
    /// Anticoagulant row absorbs TopBand slack when Topic/Volume is taller than Prescription.
    /// </summary>
    private static float AnticoagulantFillMm()
    {
        var baseMm = AnticoagulantSplitMm();
        var slackMm = Math.Max(0f, PredialysisTotalHeightMm() - PrescriptionTotalHeightMm());
        return baseMm + slackMm;
    }

    private static float PrescriptionTotalHeightMm() =>
        HemosheetDefaultStyle.HeaderBarHeightMm + PrescriptionMachineSplitMm() + AnticoagulantSplitMm();

    private static void TopBand(IContainer c, HemosheetReportViewModel vm)
    {
        var splitMm = Math.Max(PredialysisTotalHeightMm(), PrescriptionTotalHeightMm());
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(HemosheetDefaultStyle.AssessmentColumnWidthMm, Mm);
                cols.RelativeColumn();
            });
            t.Cell().AlignTop().MinHeight(splitMm, Mm).Element(left => TopicAndVolume(left, vm));
            t.Cell().AlignTop().BorderLeft(Bw).MinHeight(splitMm, Mm).Element(right => Prescription(right, vm));
        });
    }

    /// <summary>
    /// CICM pre-HD left: Topic | Assessment (Y/N) + Volume Assessment (weights / outcome / access checks).
    /// Vitals (BP/PR/…) live in the Pre HD | Post HD strip under the dialysis table — not here.
    /// </summary>
    private static void TopicAndVolume(IContainer c, HemosheetReportViewModel vm)
    {
        var bodyMm = PredialysisInnerSplitMm();
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.55f); // Topic
                cols.ConstantColumn(28, Mm); // Assessment Y/N
                cols.RelativeColumn(1.35f); // Volume Assessment
            });

            t.Cell().HeaderBar("Topic");
            t.Cell().HeaderBar("Assessment");
            t.Cell().HeaderBar("Volume Assessment");

            t.Cell().AlignTop().MinHeight(bodyMm, Mm).Column(topics =>
            {
                foreach (var (label, _) in TopicRows)
                    topics.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle()
                        .Text(label).Style(DefaultText.Base);
            });

            t.Cell().AlignTop().BorderLeft(Bw).MinHeight(bodyMm, Mm).Column(assess =>
            {
                foreach (var (_, keys) in TopicRows)
                {
                    assess.Item().Height(Rh, Mm).AlignMiddle().Element(cell =>
                        AssessmentYnOnly(cell, ThaiUrDataHelper.PreState(vm, keys)));
                }
            });

            t.Cell().AlignTop().BorderLeft(Bw).MinHeight(bodyMm, Mm)
                .Element(vol => VolumeAssessment(vol, vm));
        });
    }

    private static void AssessmentYnOnly(IContainer c, bool? yes)
    {
        c.Row(r =>
        {
            r.ConstantItem(2f);
            r.AutoItem().AlignMiddle().Text("Y").Style(DefaultText.Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == true, sizePt: 7f);
            r.ConstantItem(3f);
            r.AutoItem().AlignMiddle().Text("N").Style(DefaultText.Base);
            r.ConstantItem(2f);
            r.Checkbox(yes == false, sizePt: 7f);
            r.RelativeItem();
        });
    }

    // CICM Topic list (hemosheet-default-cicm.pdf) — not ThaiUR Pale/Edema-first list.
    private static readonly (string Label, string[] Keys)[] TopicRows =
    [
        ("Pain", ["pain"]),
        ("Chest discomfort", ["chest", "chestpain", "chest discomfort"]),
        ("Dyspnea", ["dyspnea", "dys"]),
        ("Fever", ["fever"]),
        ("Headache", ["head", "headache"]),
        ("Nausea/Vomit", ["vomit", "nausea", "vomitting"]),
        ("Sleep disturbance", ["sleep"]),
        ("Bleeding", ["pbleed", "bleeding", "prolong"]),
        ("Itching", ["itch", "itching"]),
        ("Engorged neck vein", ["neck", "engorged", "neckvein"]),
        ("Pale", ["pale"]),
        ("Edema", ["edema"]),
        ("Poor oral intake", ["ano", "anorexia", "oral", "intake"]),
        ("Psychosocial problem", ["anxiety", "psycho", "psychosocial"]),
        ("Other", ["other"]),
    ];

    private static void VolumeAssessment(IContainer c, HemosheetReportViewModel vm)
    {
        var d = vm.Dehydration;
        c.Column(col =>
        {
            LabelValue(col, "Pre HD BW", ThaiUrDataHelper.Kg(d.PreWeight), 20f);
            LabelValue(col, "Last BW", ThaiUrDataHelper.Kg(d.LastPostWeight), 20f);
            LabelValue(col, "Dry Weight", ThaiUrDataHelper.Kg(vm.DialysisPrescription.DryWeight), 20f);
            LabelValue(col, "Weight gain", ThaiUrDataHelper.WeightGain(vm), 20f);
            LabelValue(col, "UF Net", Liters(d.UfNet ?? d.TotalUf), 20f);
            LabelValue(col, "Meal/drink", ThaiUrDataHelper.Kg(d.FoodIntakeWeight), 20f);
            LabelValue(col, "UF Goal", Liters(d.UfGoal ?? d.UfEstimate ?? d.UfNet), 20f);

            col.Item().HeaderBar("Outcome");
            LabelValue(col, "Post HD BW", ThaiUrDataHelper.Kg(d.PostWeight), 20f);
            LabelValue(col, "Weight loss", ThaiUrDataHelper.WeightLoss(vm), 20f);

            col.Item().Height(Rh, Mm).YesNo(
                "Inflammation",
                ThaiUrDataHelper.PreState(vm, "inf", "inflame", "inflamation", "vas:inflammation"),
                16f, 10f, 10f);
            col.Item().Height(Rh, Mm).YesNo(
                "Thrill",
                ThaiUrDataHelper.PreState(vm, "thrill", "vas:av:thrill"),
                16f, 10f, 10f);
            col.Item().Height(Rh, Mm).Element(b => BruitRow(b, vm));
            col.Item().Height(Rh, Mm).CheckLine(
                "High risk of fall",
                ThaiUrDataHelper.Checked(vm, "High risk of fall"));
            col.Item().Height(HemosheetDefaultStyle.SectionBreathingMm, Mm);
        });
    }

    private static void BruitRow(IContainer c, HemosheetReportViewModel vm)
    {
        // Narrow Volume column — abbreviate CICM "Continue" / "Systolic" so the row fits.
        var continueOn = ThaiUrDataHelper.PreState(vm, "bruit", "vas:av:bruit", "bruit:continue") == true;
        var systolic = ThaiUrDataHelper.PreState(vm, "bruit:systolic", "systolic") == true;
        c.Row(r =>
        {
            r.ConstantItem(12, Mm).AlignMiddle().PaddingLeft(1f).Text("Bruit").Style(DefaultText.Base);
            r.Checkbox(continueOn, sizePt: 6.5f);
            r.ConstantItem(1f);
            r.AutoItem().AlignMiddle().Text("Cont.").Style(DefaultText.Base);
            r.ConstantItem(2f);
            r.Checkbox(systolic, sizePt: 6.5f);
            r.ConstantItem(1f);
            r.AutoItem().AlignMiddle().Text("Sys.").Style(DefaultText.Base);
            r.RelativeItem();
        });
    }

    private static string Liters(float? liters) =>
        liters is null ? "-" : $"{ThaiUrDataHelper.Num(liters)} L";

    private static void LabelValue(ColumnDescriptor col, string label, string? value, float labelMm = 22f, float? rowHeightMm = null)
    {
        col.Item().Height(rowHeightMm ?? Rh, Mm).Row(r =>
        {
            r.ConstantItem(labelMm, Mm).Label(label);
            r.RelativeItem().Value(value);
        });
    }

    private static void Prescription(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        var leftMm = HemosheetDefaultStyle.PrescriptionLeftColumnWidthMm;
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
                    LabelValue(left, "Surface area", pr.DialyzerSurfaceArea is not null ? $"{ThaiUrDataHelper.Num(pr.DialyzerSurfaceArea)} m\u00B2" : "-", 17f);
                    LabelValue(left, "Use No.", "New", 17f);
                    LabelValue(left, "Last TCV", "-", 17f);
                    LabelValue(left, "Grade", "-", 17f);
                    PassRow(left, "Test Leak");
                    left.Item().MinHeight(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(20, Mm).Label("Disinfectant");
                        r.Checkbox(false);
                        r.ConstantItem(1.5f);
                        r.AutoItem().AlignMiddle().Text("Peracitic acid").Style(DefaultText.Base);
                        r.RelativeItem();
                    });
                    PassRow(left, "Disinfectant test");
                    // One clamped line — long Thai label must not wrap past Rh or escape the machine cell.
                    left.Item().Height(Rh, Mm).PaddingLeft(1f).AlignMiddle().Text(text =>
                    {
                        text.DefaultTextStyle(DefaultText.Base);
                        text.ClampLines(1, "\u2026");
                        text.Span("\u0e2a\u0e32\u0e40\u0e2b\u0e15\u0e38\u0e01\u0e32\u0e23\u0e17\u0e34\u0e49\u0e07\u0e15\u0e31\u0e27\u0e01\u0e23\u0e2d\u0e07 ");
                        text.Span(ThaiUrDataHelper.OtherText(vm, "reason") ?? "-");
                    });
                });
                t.Cell().BorderLeft(Bw).MinHeight(machineSplitMm, Mm).Column(right =>
                {
                    right.Item().Height(Rh, Mm).Row(r =>
                    {
                        r.Checkbox(string.Equals(pr.Mode, "HD", StringComparison.OrdinalIgnoreCase));
                        r.ConstantItem(1.5f);
                        r.ConstantItem(12, Mm).AlignMiddle().Text("HD").Style(DefaultText.Base);
                        r.Checkbox(string.Equals(pr.Mode, "HDF", StringComparison.OrdinalIgnoreCase) || string.Equals(pr.Mode, "OHDF", StringComparison.OrdinalIgnoreCase));
                        r.ConstantItem(1.5f);
                        r.RelativeItem().AlignMiddle().Text("Online").Style(DefaultText.Base);
                    });
                    YesNoRow(right, "Na. Profile", null);
                    YesNoRow(right, "UF. Profile", null);
                    YesNoRow(right, "Isolate", null);
                    ValueUnit(right, "Na", ThaiUrDataHelper.Num(pr.DialysateNa), "mEq/L");
                    ValueUnit(right, "K+", ThaiUrDataHelper.Num(pr.DialysateK), "mEq/L");
                    ValueUnit(right, "Ca2+", ThaiUrDataHelper.Num(pr.DialysateCa), "mEq/L");
                    ValueUnit(right, "HCO3", ThaiUrDataHelper.Num(pr.DialysateHco3), "mEq/L");
                    ValueUnit(right, "Dialysis Flow", ThaiUrDataHelper.Num(pr.DialysateFlowRate), "ml/min");
                    ValueUnit(right, "Dialysis Temp", ThaiUrDataHelper.Num(pr.DialysateTemperature), "\u2103");
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
            r.AutoItem().AlignMiddle().Text("Pass").Style(DefaultText.Base);
            r.ConstantItem(3f);
            r.Checkbox(false);
            r.ConstantItem(1.5f);
            r.AutoItem().AlignMiddle().Text("Not Pass").Style(DefaultText.Base);
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
            r.AutoItem().AlignMiddle().Text("Yes").Style(DefaultText.Base);
            r.ConstantItem(6f);
            r.Checkbox(yes == false);
            r.ConstantItem(2.5f);
            r.AutoItem().AlignMiddle().Text("No").Style(DefaultText.Base);
            r.RelativeItem();
        });
    }

    private static void ValueUnit(ColumnDescriptor col, string label, string value, string unit)
    {
        col.Item().Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(20, Mm).Label(label);
            r.RelativeItem().Value(value);
            r.ConstantItem(12, Mm).AlignMiddle().Text(unit).Style(DefaultText.UnitText);
        });
    }

    private static void Anticoagulant(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        var leftMm = HemosheetDefaultStyle.PrescriptionLeftColumnWidthMm;
        var timeRh = HemosheetDefaultStyle.TimeDialysisRowHeightMm;
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
                    r.RelativeItem().AlignMiddle().Text("No Heparin").Style(DefaultText.Base);
                });
                col.Item().Height(Rh, Mm).Value($"Loading: {ThaiUrDataHelper.Ml(pr.InitialAmountMl)}");
                col.Item().Height(Rh, Mm).Value($"Maintenance: {ThaiUrDataHelper.Ml(pr.MaintainAmountMl)}");
            });
            t.Cell().BorderLeft(Bw).MinHeight(fillMm, Mm).Column(col =>
            {
                col.Item().HeaderBar("Time Dialysis");
                LabelValue(col, "Time start", ThaiUrDataHelper.Time(vm.CycleStartTime), 14f, timeRh);
                LabelValue(col, "Duration", pr.DurationHours is not null ? $"{ThaiUrDataHelper.Num(pr.DurationHours)} Hours" : "-", 14f, timeRh);
                LabelValue(col, "Time off", ThaiUrDataHelper.Time(vm.CycleEndTime), 14f, timeRh);
            });
        });
    }

    /// <summary>CICM Pre HD | Post HD vitals strip between dialysis table and fluid boxes.</summary>
    private static void PrePostVitalsRow(IContainer c, HemosheetReportViewModel vm)
    {
        var pre = vm.PreVital;
        var post = vm.PostVital;
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            t.Cell().Border(Bw).Height(PrePostVitalsHeightMm, Mm).PaddingHorizontal(2).AlignMiddle()
                .Text(text =>
                {
                    text.DefaultTextStyle(DefaultText.Base);
                    text.Span("Pre HD  ").Style(DefaultText.Bold);
                    text.Span(
                        $"BP {ThaiUrDataHelper.Bp(pre?.Bps, pre?.Bpd)}  " +
                        $"PR {DashNum(pre?.Hr)}  RR {DashNum(pre?.Rr)}  " +
                        $"BT {DashNum(pre?.Temp)}\u00B0C");
                });

            t.Cell().Border(Bw).Height(PrePostVitalsHeightMm, Mm).PaddingHorizontal(2).AlignMiddle()
                .Text(text =>
                {
                    text.DefaultTextStyle(DefaultText.Base);
                    text.Span("Post HD  ").Style(DefaultText.Bold);
                    text.Span(
                        $"BP {ThaiUrDataHelper.Bp(post?.Bps, post?.Bpd)}  " +
                        $"PR {DashNum(post?.Hr)}  RR {DashNum(post?.Rr)}  " +
                        $"BT {DashNum(post?.Temp)}\u00B0C");
                });
        });
    }

    private static string DashNum(float? v) =>
        v is null ? "-" : ThaiUrDataHelper.Num(v);

    private static void DialysisTable(
        IContainer c,
        HemosheetReportViewModel vm,
        int fixedLines,
        IReadOnlyList<string>? fileHeaders)
    {
        if (fixedLines <= 0) fixedLines = 8;
        var showHdf = HemosheetDialysisColumns.ShowHdf(vm);
        var colMm = HemosheetDialysisColumns.DataColumnWidthMm(showHdf);
        var useFileHeaders = fileHeaders is { Count: > 0 };
        var headerMm = HemosheetDialysisColumns.HeaderHeightMm(showHdf && !useFileHeaders, Rh);
        var baseDefs = HemosheetDialysisColumns.BaseColumnDefs;
        var insertAfter = HemosheetDialysisColumns.SubstituteInsertAfterIndex;

        c.DefaultTextStyle(DefaultText.Dialysis).Table(t =>
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

                t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text(fileHeaders[^1]).Style(DefaultText.DialysisBold);
            }
            else if (showHdf)
            {
                // Dual header: other columns RowSpan(2); Substitute parent ColumnSpan(2) + total/rate.
                for (var i = 0; i <= insertAfter; i++)
                    DialysisHeaderCell(t, baseDefs[i].Head, baseDefs[i].Unit, headerMm, rowSpan: 2);

                t.Cell().ColumnSpan(2).Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
                    .Height(headerMm * 0.55f, Mm).AlignCenter().AlignMiddle()
                    .Column(cc =>
                    {
                        cc.Item().AlignCenter().Text("Substitute").Style(DefaultText.DialysisBold);
                        cc.Item().AlignCenter().Text("(L/hr)").Style(DefaultText.DialysisUnit);
                    });

                for (var i = insertAfter + 1; i < baseDefs.Length; i++)
                    DialysisHeaderCell(t, baseDefs[i].Head, baseDefs[i].Unit, headerMm, rowSpan: 2);

                t.Cell().RowSpan(2).Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text("Note").Style(DefaultText.DialysisBold);

                foreach (var sub in new[] { "total", "rate" })
                {
                    t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
                        .Height(headerMm * 0.45f, Mm).AlignCenter().AlignMiddle()
                        .Text(sub).Style(DefaultText.DialysisUnit);
                }
            }
            else
            {
                foreach (var (head, unit) in baseDefs)
                    DialysisHeaderCell(t, head, unit, headerMm, rowSpan: 1);

                t.Cell().Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
                    .Height(headerMm, Mm).AlignCenter().AlignMiddle()
                    .Text("Note").Style(DefaultText.DialysisBold);
            }

            for (var i = 0; i < fixedLines; i++)
            {
                var rec = i < vm.DialysisRecords.Count ? vm.DialysisRecords[i] : null;
                foreach (var value in HemosheetDialysisColumns.CellValues(rec, showHdf))
                {
                    t.Cell().Border(Bw).Height(Rh, Mm).AlignMiddle().AlignCenter()
                        .Text(value).Style(DefaultText.Dialysis);
                }
                t.Cell().Border(Bw).Height(Rh, Mm).PaddingHorizontal(1f).AlignMiddle()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(DefaultText.Dialysis);
                        text.ClampLines(HemosheetDefaultStyle.DialysisNoteMaxLines, "\u2026");
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

        cell.Border(Bw).Background(ReportSectionHeaderChrome.Resolve(HemosheetDefaultStyle.HeaderBackground))
            .Height(headerMm, Mm).AlignCenter().AlignMiddle()
            .Column(cc =>
            {
                cc.Item().AlignCenter().Text(head).Style(DefaultText.DialysisBold);
                if (!string.IsNullOrEmpty(unit))
                    cc.Item().AlignCenter().Text(unit).Style(DefaultText.DialysisUnit);
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

        c.DefaultTextStyle(DefaultText.Dialysis).Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < HemosheetDialysisColumns.DataColumnCount(showHdf); i++)
                    cols.ConstantColumn(colMm, Mm);
                cols.RelativeColumn();
            });

            foreach (var (span, label, value) in boxes)
            {
                t.Cell().ColumnSpan((uint)span).Border(Bw).MinHeight(6.5f, Mm).Padding(1f)
                    .AlignMiddle().AlignCenter()
                    .Column(cc =>
                    {
                        cc.Item().AlignCenter().Text(label).Style(DefaultText.DialysisBold);
                        cc.Item().AlignCenter().Text(value).Style(DefaultText.Dialysis);
                    });
            }
        });
    }

}
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

/// <summary>
/// Pixel-parity reproduction of the Telerik "Hemodialysis Record" (Hemosheet-ThaiUR.trdp).
/// Layout is built from fixed millimetre tables so it fits one dense A4 page and matches the
/// original. Each region is a small method so the form stays maintainable / individually tunable.
/// See .cursor/docs/hemosheet-thaiur-spec.md for the coordinate source.
/// </summary>
internal sealed class ThaiUrHemosheetForm
{
    private const Unit Mm = Unit.Millimetre;
    private const float Rh = HemosheetThaiUrStyle.RowHeightMm;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    public void Compose(IContainer container, HemosheetReportViewModel vm, PdfReportContext context)
    {
        container
            .DefaultTextStyle(ThaiUrText.Base)
            .Border(Bw)
            .Column(page =>
            {
                page.Item().Element(c => Header(c, vm));
                page.Item().Element(c => DiagnosisRow(c, vm));
                page.Item().Element(c => TopBand(c, vm));
                page.Item().Element(c => NursingPlan(c, vm));
                page.Item().Element(c => DialysisTable(c, vm));
                page.Item().Element(c => BottomBand(c, vm));
                page.Item().Element(c => PostVital(c, vm));
                page.Item().Element(c => Signatures(c, vm));
            });
    }

    // ---- Header: logo | title | patient meta -----------------------------------------------
    private static void Header(IContainer c, HemosheetReportViewModel vm)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(48, Mm);
                cols.RelativeColumn();
                cols.ConstantColumn(70, Mm);
            });

            t.Cell().Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm).AlignMiddle().AlignCenter()
                .Element(logo => Logo(logo, vm));

            t.Cell().Border(Bw).AlignMiddle().AlignCenter().Text("Hemodialysis Record").Style(ThaiUrText.Title);

            t.Cell().Border(Bw).Padding(1f).Column(meta =>
            {
                MetaLine(meta, "Name", vm.Patient.Name, null, null);
                MetaLine(meta, "CN", vm.Patient.Hn, "Age", ThaiUrData.Num(vm.Patient.Age));
                MetaLine(meta, "Coverage", vm.Patient.Coverage, null, null);
                MetaLine(meta, "ID Card NO.", vm.Patient.IdentityNumber, null, null);
                MetaLine(meta, "Date", ThaiUrData.Date(vm.CycleStartTime), "HD NO.", ThaiUrData.Num(vm.TreatmentNo));
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
                // fall through to text placeholder
            }
        }

        c.Text(vm.Unit.FullName ?? "").Style(ThaiUrText.Base);
    }

    private static void MetaLine(ColumnDescriptor col, string label, string? value, string? label2, string? value2)
    {
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

    private static void DiagnosisRow(IContainer c, HemosheetReportViewModel vm)
    {
        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
            });
            t.Cell().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(16, Mm).Label("Diagnosis");
                r.RelativeItem().Value(vm.Patient.Diagnosis ?? vm.Patient.Underlying);
            });
            t.Cell().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(20, Mm).Label("Drug Allergy");
                r.RelativeItem().Value(ThaiUrData.Allergies(vm));
            });
        });
    }

    // ---- Top band: Predialysis (left 100mm) + Prescription (right) --------------------------
    private static void TopBand(IContainer c, HemosheetReportViewModel vm)
    {
        c.Row(row =>
        {
            row.ConstantItem(100, Mm).Border(Bw).Element(left => Predialysis(left, vm));
            row.RelativeItem().Border(Bw).Element(right => Prescription(right, vm));
        });
    }

    private static void Predialysis(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().HeaderBar("Predialysys Assessment");
            col.Item().Row(r =>
            {
                r.ConstantItem(58, Mm).Column(leftCol =>
                {
                    leftCol.Item().Element(v => Vitals(v, vm));
                    leftCol.Item().Element(s => Symptoms(s, vm));
                });
                r.RelativeItem().Column(rightCol =>
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
            col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(14, Mm).Label("Urine");
                r.Checkbox(ThaiUrData.PreState(vm, "urine") == true);
            });
        });
    }

    private static void LabeledValueUnit(ColumnDescriptor col, string label, string value, string unit)
    {
        col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(12, Mm).Label(label);
            r.RelativeItem().Value(value);
            r.ConstantItem(10, Mm).AlignMiddle().Text(unit).Style(ThaiUrText.UnitText);
        });
    }

    private static readonly (string Label, string[] Keys)[] SymptomRows =
    [
        ("Pale", ["pale"]),
        ("Edema", ["edema"]),
        ("Dyspnea", ["dyspnea"]),
        ("Fever", ["fever"]),
        ("Crepitatic", ["crepitatic", "crepitation"]),
        ("Headache", ["headache"]),
        ("Nausea/Vomitting", ["nausea", "vomit", "vomitting"]),
        ("Anorexia", ["anorexia"]),
        ("Itching", ["itching"]),
        ("Engorged neck vein", ["engorged", "neckvein"]),
        ("Anxiety", ["anxiety"]),
        ("Sleep disturbance", ["sleep"]),
        ("Constipation", ["constipation"]),
        ("Prolong bleeding", ["prolong", "bleeding"]),
    ];

    private static void Symptoms(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            foreach (var (label, keys) in SymptomRows)
            {
                col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f)
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
            LabelValue(col, "Target UF", ThaiUrData.Ml(d.TotalUf is not null ? d.TotalUf * 1000 : null));
            LabelValue(col, "Post BW", ThaiUrData.Kg(d.PostWeight));
            LabelValue(col, "Weight loss", ThaiUrData.WeightLoss(vm));
            LabelValue(col, "IDWG", ThaiUrData.Idwg(vm));
        });
    }

    private static void LabelValue(ColumnDescriptor col, string label, string? value, float labelMm = 22f)
    {
        col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(labelMm, Mm).Label(label);
            r.RelativeItem().Value(value);
        });
    }

    private static void VascularAccess(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().HeaderBar("Vascular Access");
            col.Item().Border(Bw).Height(Rh, Mm).Value(vm.AvShunt.ShuntSite ?? vm.DialysisPrescription.BloodAccessRoute);
            LabelValue(col, "Needle No.", $"A {ThaiUrData.Num(vm.AvShunt.ANeedleSize)}  V {ThaiUrData.Num(vm.AvShunt.VNeedleSize)}", 18f);
            col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).YesNo("Thrill", ThaiUrData.PreState(vm, "thrill", "vas:av:thrill"));
            col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).YesNo("Bruit", ThaiUrData.PreState(vm, "bruit", "vas:av:bruit"));
            col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).YesNo("Edema", ThaiUrData.PreState(vm, "vas:edema", "edema"));
            col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).YesNo("Inflamation", ThaiUrData.PreState(vm, "vas:inflammation", "inflamation"));
        });
    }

    private static void Prescription(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        c.Column(col =>
        {
            col.Item().HeaderBar("Hemodialysis Prescription");
            col.Item().Row(row =>
            {
                row.ConstantItem(51, Mm).Column(left =>
                {
                    LabelValue(left, "Machine", vm.Bed, 17f);
                    LabelValue(left, "Dialyzer", pr.Dialyzer, 17f);
                    LabelValue(left, "Surface area", pr.DialyzerSurfaceArea is not null ? $"{ThaiUrData.Num(pr.DialyzerSurfaceArea)} m\u00B2" : "-", 17f);
                    LabelValue(left, "Use No.", "New", 17f);
                    LabelValue(left, "Last TCV", "-", 17f);
                    LabelValue(left, "Grade", "-", 17f);
                    PassRow(left, "Test Leak");
                    left.Item().Border(Bw).MinHeight(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(20, Mm).Label("Disinfectant");
                        r.Checkbox(false);
                        r.ConstantItem(1.5f);
                        r.AutoItem().AlignMiddle().Text("Peracitic acid").Style(ThaiUrText.Base);
                        r.RelativeItem();
                    });
                    PassRow(left, "Disinfectant test");
                    left.Item().Border(Bw).MinHeight(Rh, Mm).Row(r =>
                    {
                        r.ConstantItem(34, Mm).Label("สาเหตุการทิ้งตัวกรอง");
                        r.RelativeItem().Value(ThaiUrData.OtherText(vm, "reason"));
                    });
                });
                row.RelativeItem().Column(right =>
                {
                    right.Item().Border(Bw).Height(Rh, Mm).Row(r =>
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
            col.Item().Element(a => Anticoagulant(a, vm));
        });
    }

    // Label + [ ] Pass  [ ] Not Pass. The trailing RelativeItem spacer absorbs slack so the
    // "Pass"/"Not Pass" captions stay single-line (AutoItem) and never wrap to overflow the row.
    private static void PassRow(ColumnDescriptor col, string label)
    {
        col.Item().Border(Bw).MinHeight(Rh, Mm).Row(r =>
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
        col.Item().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).Row(r =>
        {
            r.ConstantItem(18, Mm).Label(label);
            r.Checkbox(yes == true);
            r.ConstantItem(1.5f);
            r.AutoItem().AlignMiddle().Text("Yes").Style(ThaiUrText.Base);
            r.ConstantItem(3f);
            r.Checkbox(yes == false);
            r.ConstantItem(1.5f);
            r.AutoItem().AlignMiddle().Text("No").Style(ThaiUrText.Base);
            r.RelativeItem();
        });
    }

    private static void ValueUnit(ColumnDescriptor col, string label, string value, string unit)
    {
        col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(20, Mm).Label(label);
            r.RelativeItem().Value(value);
            r.ConstantItem(12, Mm).AlignMiddle().Text(unit).Style(ThaiUrText.UnitText);
        });
    }

    private static void Anticoagulant(IContainer c, HemosheetReportViewModel vm)
    {
        var pr = vm.DialysisPrescription;
        c.Row(row =>
        {
            row.RelativeItem().Border(Bw).Column(col =>
            {
                col.Item().HeaderBar("Anticoagulant");
                col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
                {
                    r.Checkbox(vm.IsAcNotUsed);
                    r.ConstantItem(1.5f);
                    r.RelativeItem().AlignMiddle().Text("No Heparin").Style(ThaiUrText.Base);
                });
                col.Item().Border(Bw).Height(Rh, Mm).Value($"Loading: {ThaiUrData.Ml(pr.InitialAmountMl)}");
                col.Item().Border(Bw).Height(Rh, Mm).Value($"Maintenance: {ThaiUrData.Ml(pr.MaintainAmountMl)}");
            });
            row.RelativeItem().Border(Bw).Column(col =>
            {
                col.Item().HeaderBar("Time Dialysis");
                LabelValue(col, "Time start", ThaiUrData.Time(vm.CycleStartTime), 14f);
                LabelValue(col, "Duration", pr.DurationHours is not null ? $"{ThaiUrData.Num(pr.DurationHours)} Hours" : "-", 14f);
                LabelValue(col, "Time off", ThaiUrData.Time(vm.CycleEndTime), 14f);
            });
        });
    }

    // ---- Nursing diagnosis / intervention / expected outcomes ------------------------------
    private static void NursingPlan(IContainer c, HemosheetReportViewModel vm)
    {
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

            t.Cell().Border(Bw).MinHeight(9, Mm).Value(ThaiUrData.OtherText(vm, "nursing_diagnosis"));
            t.Cell().Border(Bw).MinHeight(9, Mm).Value(ThaiUrData.OtherText(vm, "nursing_intervention"));
            t.Cell().Border(Bw).MinHeight(9, Mm).Value(ThaiUrData.OtherText(vm, "expected_outcomes"));
        });
    }

    // ---- Dialysis records table -------------------------------------------------------------
    private static readonly (string Head, string Unit, float Mm)[] DialysisColumns =
    [
        ("Time", "", 12f), ("BP", "mmHg", 15f), ("MAP", "mmHg", 10f), ("Pulse", "/min", 10f),
        ("EBFR", "ml/min", 11f), ("AP", "mmHg", 10f), ("VP", "mmHg", 10f), ("TMP", "mmHg", 10f),
        ("Cond.", "mS/cm", 12f), ("UFR", "ml/hr", 12f), ("Total UF", "ml", 14f),
    ];

    private static void DialysisTable(IContainer c, HemosheetReportViewModel vm)
    {
        var fixedLines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Dialysis, vm.DialysisRecords.Count);
        if (fixedLines <= 0) fixedLines = 8;

        c.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (var col in DialysisColumns)
                    cols.ConstantColumn(col.Mm, Mm);
                cols.RelativeColumn();
            });

            foreach (var col in DialysisColumns)
            {
                t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground).AlignCenter().AlignMiddle()
                    .Column(cc =>
                    {
                        cc.Item().Text(col.Head).Style(ThaiUrText.Bold);
                        if (!string.IsNullOrEmpty(col.Unit))
                            cc.Item().Text(col.Unit).Style(ThaiUrText.UnitText);
                    });
            }
            t.Cell().Border(Bw).Background(HemosheetThaiUrStyle.HeaderBackground).AlignCenter().AlignMiddle()
                .Text("Note").Style(ThaiUrText.Bold);

            for (var i = 0; i < fixedLines; i++)
            {
                var rec = i < vm.DialysisRecords.Count ? vm.DialysisRecords[i] : null;
                var cells = new[]
                {
                    ThaiUrData.Time(rec?.Timestamp),
                    rec is null ? "" : ThaiUrData.Bp(rec.Bps, rec.Bpd),
                    "",
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
                    t.Cell().Border(Bw).Height(Rh, Mm).ValueCentered(value);
                t.Cell().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).AlignMiddle().Text(rec?.Note ?? "").Style(ThaiUrText.Base);
            }
        });
    }

    // ---- Bottom band: fluid summary + Complication / Nursing / Health / Medication ----------
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

    private static void BottomBand(IContainer c, HemosheetReportViewModel vm)
    {
        c.Row(row =>
        {
            row.ConstantItem(37, Mm).Border(Bw).Element(fs => FluidSummary(fs, vm));
            row.ConstantItem(39, Mm).Border(Bw).Element(cp => CheckGroup(cp, vm, "Complication", ComplicationItems, "Technical complication", TechnicalItems));
            row.ConstantItem(41, Mm).Border(Bw).Element(nm => CheckGroup(nm, vm, "Nursing management", NursingItems, null, null));
            row.RelativeItem().Border(Bw).Element(he => HealthAndMedication(he, vm));
        });
    }

    private static void FluidSummary(IContainer c, HemosheetReportViewModel vm)
    {
        var d = vm.Dehydration;
        c.Column(col =>
        {
            SummaryBox(col, "NSS", ThaiUrData.Ml(d.FlushNss));
            SummaryBox(col, "50% Glucose", "-");
            SummaryBox(col, "Extra-fluid", ThaiUrData.Ml(d.ExtraFluid));
            SummaryBox(col, "Total fluid replacment", "-");
            SummaryBox(col, "Total UF", ThaiUrData.Ml(d.TotalUf is not null ? d.TotalUf * 1000 : null));
            SummaryBox(col, "Net fluid balance", "-");
        });
    }

    private static void SummaryBox(ColumnDescriptor col, string label, string value)
    {
        col.Item().Border(Bw).MinHeight(8, Mm).Padding(1f).Column(cc =>
        {
            cc.Item().Text(label).Style(ThaiUrText.Bold);
            cc.Item().Text(value).Style(ThaiUrText.Base);
        });
    }

    private static void CheckGroup(IContainer c, HemosheetReportViewModel vm, string title, string[] items, string? title2, string[]? items2)
    {
        c.Column(col =>
        {
            col.Item().HeaderBar(title);
            foreach (var item in items)
                col.Item().PaddingLeft(1f).CheckLine(item, ThaiUrData.Checked(vm, item));

            if (title2 is not null && items2 is not null)
            {
                col.Item().HeaderBar(title2);
                foreach (var item in items2)
                    col.Item().PaddingLeft(1f).CheckLine(item, ThaiUrData.Checked(vm, item));
            }
        });
    }

    private static void HealthAndMedication(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.ConstantItem(40, Mm).Column(he =>
                {
                    he.Item().HeaderBar("Health education");
                    foreach (var item in HealthItems)
                        he.Item().PaddingLeft(1f).CheckLine(item, ThaiUrData.Checked(vm, item));
                    he.Item().Border(Bw).Height(Rh, Mm).Row(lab =>
                    {
                        lab.RelativeItem().Label($"Hct: {vm.Labs.Hct ?? "-"}");
                        lab.RelativeItem().Label($"Hb: {vm.Labs.Hb ?? "-"}");
                    });
                });
                r.RelativeItem().Element(med => MedicationTable(med, vm));
            });
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
                    cols.ConstantColumn(20, Mm);
                    cols.ConstantColumn(18, Mm);
                });
                t.Cell().Border(Bw).AlignCenter().Text("Name/Dose/Route").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).AlignCenter().Text("Time").Style(ThaiUrText.Bold);
                t.Cell().Border(Bw).AlignCenter().Text("Sign").Style(ThaiUrText.Bold);

                var lines = Math.Max(vm.LayoutContext.ReportSettings.FixedLines.Medicine, vm.MedicineRecords.Count);
                if (lines <= 0) lines = 4;
                for (var i = 0; i < lines; i++)
                {
                    var m = i < vm.MedicineRecords.Count ? vm.MedicineRecords[i] : null;
                    var name = m is null ? "" : $"{m.MedicineName} {ThaiUrData.Num(m.Quantity)} {m.Route}".Trim();
                    t.Cell().Border(Bw).Height(Rh, Mm).PaddingLeft(1f).Text(name).Style(ThaiUrText.Base);
                    t.Cell().Border(Bw).Height(Rh, Mm).ValueCentered(ThaiUrData.Time(m?.Timestamp));
                    t.Cell().Border(Bw).Height(Rh, Mm);
                }
            });
        });
    }

    // ---- Post vital + signatures ------------------------------------------------------------
    private static void PostVital(IContainer c, HemosheetReportViewModel vm)
    {
        var p = vm.PostVital;
        c.Border(Bw).Height(Rh, Mm).Row(r =>
        {
            r.ConstantItem(20, Mm).LabelBold("Post Vital");
            PostVitalItem(r, "BP", ThaiUrData.Bp(p?.Bps, p?.Bpd));
            PostVitalItem(r, "PR", ThaiUrData.Num(p?.Hr));
            PostVitalItem(r, "RR", ThaiUrData.Num(p?.Rr));
            PostVitalItem(r, "Sat", $"{ThaiUrData.Num(p?.SpO2)} %");
            PostVitalItem(r, "BT", $"{ThaiUrData.Num(p?.Temp)} \u00B0C");
        });
    }

    private static void PostVitalItem(RowDescriptor r, string label, string value)
    {
        r.RelativeItem().Row(inner =>
        {
            inner.AutoItem().AlignMiddle().Text($"{label} ").Style(ThaiUrText.Bold);
            inner.RelativeItem().AlignMiddle().Text(value).Style(ThaiUrText.Base);
        });
    }

    private static void Signatures(IContainer c, HemosheetReportViewModel vm)
    {
        c.Column(col =>
        {
            col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis Nurse");
                r.RelativeItem().Value(vm.NursesInShiftNonPn);
            });
            col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Dialysis NA");
                r.RelativeItem().Value(vm.NursesInShift);
            });
            col.Item().Border(Bw).Height(Rh, Mm).Row(r =>
            {
                r.ConstantItem(28, Mm).LabelBold("Nephrologist");
                r.RelativeItem().Value(vm.DoctorName ?? vm.Patient.DoctorName);
            });
        });
    }
}

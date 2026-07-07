using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetPreviewMappers
{
    public static PatientInfoReportBlock? MapPatient(HemosheetReportViewModel vm) =>
        new()
        {
            Title = "ข้อมูลผู้ป่วย",
            Columns =
            [
                [
                    Lv("ชื่อ-สกุล", vm.Patient.Name),
                    Lv("HN", vm.Patient.Hn),
                    Lv("เลขบัตรประชาชน", vm.Patient.IdentityNumber),
                ],
                [
                    Lv("วันเกิด", FormatDate(vm.Patient.BirthDate)),
                    Lv("เพศ", vm.Patient.Sex),
                    Lv("หน่วย", vm.Unit.FullName),
                ],
            ],
        };

    public static FieldGridReportBlock? MapSessionMeta(HemosheetReportViewModel vm)
    {
        var fields = new List<FieldGridField>
        {
            F("Ward", vm.Ward),
            F("Bed", vm.Bed),
            F("Treatment No.", vm.TreatmentNo?.ToString()),
            F("เริ่มฟอก", FormatDateTime(vm.CycleStartTime)),
            F("สิ้นสุด", FormatDateTime(vm.CycleEndTime)),
            F("Complete", FormatDateTime(vm.CompletedTime)),
            F("Kt/V", FormatFloat(vm.Ktv)),
            F("URR", FormatFloat(vm.Urr)),
            F("PRR", FormatFloat(vm.Prr)),
            F("Recirc", FormatFloat(vm.Recir)),
        };

        if (!string.IsNullOrWhiteSpace(vm.CreatorName))
        {
            fields.Add(F("ผู้สร้าง", vm.CreatorName));
        }

        return new FieldGridReportBlock
        {
            Title = "ข้อมูลรอบฟอก",
            Columns = 4,
            Fields = fields,
        };
    }

    public static FieldGridReportBlock? MapDehydration(HemosheetReportViewModel vm) =>
        new()
        {
            Title = "น้ำหนัก / Dehydration",
            Columns = 4,
            Fields =
            [
                F("Pre Weight", FormatFloat(vm.Dehydration.PreWeight)),
                F("Post Weight", FormatFloat(vm.Dehydration.PostWeight)),
                F("Last Post", FormatFloat(vm.Dehydration.LastPostWeight)),
                F("Food Intake", FormatFloat(vm.Dehydration.FoodIntakeWeight)),
                F("Extra Fluid", FormatFloat(vm.Dehydration.ExtraFluid)),
                F("Blood Transfusion", FormatFloat(vm.Dehydration.BloodTransfusion)),
                F("UF Net", FormatFloat(vm.Dehydration.UfNet)),
            ],
        };

    public static FieldGridReportBlock? MapPrescription(
        HemosheetReportViewModel vm,
        IReadOnlyDictionary<string, bool> features)
    {
        var fields = new List<FieldGridField>
        {
            F("Mode", vm.DialysisPrescription.Mode),
            F("Route", vm.DialysisPrescription.BloodAccessRoute),
            F("Dry Weight", FormatFloat(vm.DialysisPrescription.DryWeight)),
        };

        if (IsEnabled(features, "showDurationHours"))
        {
            fields.Add(F("Duration (hr)", FormatFloat(vm.DialysisPrescription.DurationHours)));
        }

        if (IsEnabled(features, "showDurationMinutes"))
        {
            fields.Add(F("Duration (min)", FormatFloat(vm.DialysisPrescription.DurationMinutes)));
        }

        if (IsEnabled(features, "showHdfColumns"))
        {
            fields.Add(F("HDF Type", vm.DialysisPrescription.HdfType));
        }

        if (IsEnabled(features, "showAcFields"))
        {
            fields.Add(F("Anticoagulant", vm.DialysisPrescription.Anticoagulant));
        }
        else if (IsEnabled(features, "showAcNotUsed"))
        {
            fields.Add(F("Anticoagulant", "ไม่ใช้"));
        }

        return new FieldGridReportBlock
        {
            Title = "คำสั่งการฟอก",
            Columns = 4,
            Fields = fields,
        };
    }

    public static VascularAccessReportBlock? MapVascularAccess(HemosheetReportViewModel vm, string? variant)
    {
        if (string.IsNullOrWhiteSpace(variant))
        {
            return null;
        }

        var rows = variant == "av-fistula"
            ? new List<LabelValue>
            {
                Lv("Site", vm.AvShunt.ShuntSite),
                Lv("A Needle", FormatFloat(vm.AvShunt.ANeedleSize)),
                Lv("V Needle", FormatFloat(vm.AvShunt.VNeedleSize)),
            }
            : new List<LabelValue>
            {
                Lv("Site", vm.AvShunt.ShuntSite),
                Lv("Catheter Length", FormatFloat(vm.AvShunt.CatheterLength)),
                Lv("Catheter Type", vm.AvShunt.CatheterType?.ToString()),
            };

        return new VascularAccessReportBlock
        {
            Variant = variant,
            Title = variant == "av-fistula" ? "Vascular Access (AV Fistula)" : "Vascular Access (Perm Cath)",
            Rows = rows,
        };
    }

    public static ChecklistTableReportBlock? MapAssessment(
        string title,
        IList<HemosheetAssessmentItemViewModel> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        return ChecklistTablePreviewMapper.Map(new ChecklistTableModel
        {
            Title = title,
            Items = items.Select(i => new ChecklistItem
            {
                Label = i.Name ?? "",
                IsChecked = i.Checked,
                Notes = i.Text,
            }).ToList(),
        });
    }

    public static KeyValueTableReportBlock? MapLabs(HemosheetReportViewModel vm)
    {
        var labs = vm.Labs;
        var rows = new List<LabelValue>
        {
            Lv("Hct", labs.Hct),
            Lv("Hb", labs.Hb),
            Lv("Plt", labs.Plt),
            Lv("WBC", labs.Wbc),
            Lv("Na", labs.Na),
            Lv("K", labs.K),
            Lv("Cl", labs.Cl),
            Lv("CO2", labs.Co2),
            Lv("BUN", labs.Bun),
            Lv("Cr", labs.Cr),
            Lv("Alb", labs.Alb),
            Lv("Ca", labs.Ca),
            Lv("P", labs.P),
            Lv("Mg", labs.Mg),
            Lv("HBsAg", labs.Hbsag),
            Lv("Anti-HCV", labs.AntiHcv),
            Lv("Anti-HIV", labs.AntiHiv),
        };

        if (rows.All(r => r.Value == "—"))
        {
            return null;
        }

        return new KeyValueTableReportBlock { Title = "Lab", Rows = rows };
    }

    public static DataGridReportBlock? MapDialysisRecords(
        HemosheetReportViewModel vm,
        IReadOnlyList<string> columns,
        int fixedLineCount)
    {
        if (columns.Count == 0)
        {
            return null;
        }

        var rows = vm.DialysisRecords.Select(record => MapDialysisRow(record, columns)).ToList();
        PadRows(rows, fixedLineCount, columns.Count);

        return new DataGridReportBlock
        {
            Title = "บันทึกระหว่างฟอก",
            Columns = columns,
            ColumnWeights = ResolveDialysisColumnWeights(columns),
            Rows = rows,
        };
    }

    public static DataGridReportBlock? MapTextRecords(
        string title,
        IList<HemosheetNurseRecordViewModel> records,
        int fixedLineCount) =>
        MapTimestampContentGrid(title, records.Select(r => (r.Timestamp, r.Content)).ToList(), fixedLineCount);

    public static DataGridReportBlock? MapTextRecords(
        string title,
        IList<HemosheetDoctorRecordViewModel> records,
        int fixedLineCount) =>
        MapTimestampContentGrid(title, records.Select(r => (r.Timestamp, r.Content)).ToList(), fixedLineCount);

    public static DataGridReportBlock? MapMedicineRecords(
        HemosheetReportViewModel vm,
        int fixedLineCount)
    {
        var rows = vm.MedicineRecords
            .Select(r => (IReadOnlyList<string>)
            [
                FormatDateTime(r.Timestamp) ?? "—",
                r.MedicineName ?? "—",
                r.Route ?? "—",
                FormatFloat(r.Quantity) ?? "—",
            ])
            .ToList();

        PadRows(rows, fixedLineCount, 4);

        return new DataGridReportBlock
        {
            Title = "ยา",
            Columns = ["เวลา", "ชื่อยา", "Route", "จำนวน"],
            Rows = rows,
        };
    }

    public static DataGridReportBlock? MapProgressNotes(HemosheetReportViewModel vm, int fixedLineCount)
    {
        var rows = vm.ProgressNotes
            .Select(r => (IReadOnlyList<string>) [r.A ?? "—", r.I ?? "—", r.E ?? "—"])
            .ToList();

        PadRows(rows, fixedLineCount, 3);

        return new DataGridReportBlock
        {
            Title = "Progress Note",
            Columns = ["A", "I", "E"],
            Rows = rows,
        };
    }

    public static string? BuildNursesInShiftLine(
        HemosheetReportViewModel vm,
        IReadOnlyDictionary<string, bool> features)
    {
        var text = IsEnabled(features, "showNurseInShiftNonPn")
            ? vm.NursesInShiftNonPn
            : vm.NursesInShift;

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static IReadOnlyList<float> ResolveDialysisColumnWeights(IReadOnlyList<string> columns) =>
        columns.Select(ResolveDialysisColumnWeight).ToList();

    private static float ResolveDialysisColumnWeight(string column) =>
        column switch
        {
            "เวลา" => 1.5f,
            "BP" => 1f,
            "HR" => 0.6f,
            "RR" => 0.6f,
            "BFR" => 0.7f,
            "VP" => 0.7f,
            "TMP" => 0.7f,
            "DC" => 0.6f,
            "NSS" => 0.6f,
            "UF Rate" => 0.8f,
            "HDF Vol." => 0.8f,
            "หมายเหตุ" => 3.5f,
            _ => 1f,
        };

    public static TextReportBlock? MapNursesInShift(HemosheetReportViewModel vm, IReadOnlyDictionary<string, bool> features)
    {
        var text = BuildNursesInShiftLine(vm, features);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new TextReportBlock
        {
            Content = text,
            Style = "body",
        };
    }

    public static TextReportBlock? MapConsent(HemosheetReportViewModel vm) =>
        vm.IsConsent
            ? new TextReportBlock
            {
                Title = "ความยินยอม",
                Content = "ผู้ป่วยให้ความยินยอมในการรักษา",
                Style = "caption",
            }
            : null;

    private static DataGridReportBlock? MapTimestampContentGrid(
        string title,
        IList<(DateTime? Timestamp, string? Content)> records,
        int fixedLineCount)
    {
        var rows = records
            .Select(r => (IReadOnlyList<string>) [FormatDateTime(r.Timestamp) ?? "—", r.Content ?? "—"])
            .ToList();

        PadRows(rows, fixedLineCount, 2);

        return new DataGridReportBlock
        {
            Title = title,
            Columns = ["เวลา", "รายละเอียด"],
            Rows = rows,
        };
    }

    private static IReadOnlyList<string> MapDialysisRow(
        HemosheetDialysisRecordViewModel record,
        IReadOnlyList<string> columns)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["เวลา"] = FormatDateTime(record.Timestamp),
            ["BP"] = record.Bps.HasValue || record.Bpd.HasValue ? $"{record.Bps}/{record.Bpd}" : null,
            ["HR"] = record.Hr?.ToString(),
            ["RR"] = record.Rr?.ToString(),
            ["BFR"] = FormatFloat(record.Bfr),
            ["VP"] = FormatFloat(record.Vp),
            ["TMP"] = FormatFloat(record.Tmp),
            ["DC"] = FormatFloat(record.Dc),
            ["NSS"] = FormatFloat(record.Nss),
            ["UF Rate"] = FormatFloat(record.UfRate),
            ["HDF Vol."] = FormatFloat(record.HdfVolume),
            ["หมายเหตุ"] = record.Note,
        };

        return columns.Select(col => map.TryGetValue(col, out var value) ? value ?? "—" : "—").ToList();
    }

    private static void PadRows(List<IReadOnlyList<string>> rows, int fixedLineCount, int columnCount)
    {
        while (rows.Count < fixedLineCount)
        {
            rows.Add(Enumerable.Repeat("—", columnCount).ToList());
        }
    }

    private static bool IsEnabled(IReadOnlyDictionary<string, bool> features, string key) =>
        features.TryGetValue(key, out var enabled) && enabled;

    private static LabelValue Lv(string label, string? value) => new() { Label = label, Value = value ?? "—" };

    private static FieldGridField F(string label, string? value) => new() { Label = label, Value = value ?? "—" };

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd");

    private static string? FormatDateTime(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm");

    private static string? FormatFloat(float? value) =>
        value?.ToString("0.##");
}

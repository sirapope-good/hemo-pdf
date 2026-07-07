using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;

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
                    Lv("เพศ", vm.Patient.Sex),
                ],
                [
                    Lv("อายุ", vm.Patient.Age?.ToString()),
                    Lv("วันเกิด", FormatDate(vm.Patient.BirthDate)),
                    Lv("แพทย์", vm.Patient.DoctorName),
                ],
                [
                    Lv("สิทธิ์", vm.Patient.Coverage),
                    Lv("แพ้ยา", vm.Patient.Allergies.Count > 0 ? string.Join(", ", vm.Patient.Allergies) : null),
                    Lv("หน่วย", vm.Unit.FullName),
                ],
            ],
        };

    public static KeyValueTableReportBlock? MapSessionMeta(HemosheetReportViewModel vm)
    {
        var rows = new List<LabelValue>
        {
            Lv("Ward", vm.Ward),
            Lv("Bed", vm.Bed),
            Lv("Treatment No.", vm.TreatmentNo?.ToString()),
            Lv("เริ่มฟอก", FormatDateTime(vm.CycleStartTime)),
            Lv("สิ้นสุด", FormatDateTime(vm.CycleEndTime)),
            Lv("Complete", FormatDateTime(vm.CompletedTime)),
            Lv("Kt/V", FormatFloat(vm.Ktv)),
            Lv("URR", FormatFloat(vm.Urr)),
            Lv("PRR", FormatFloat(vm.Prr)),
            Lv("Recirc", FormatFloat(vm.Recir)),
        };

        if (!string.IsNullOrWhiteSpace(vm.CreatorName))
        {
            rows.Add(Lv("ผู้สร้าง", vm.CreatorName));
        }

        return new KeyValueTableReportBlock { Title = "ข้อมูลรอบฟอก", Rows = rows };
    }

    public static KeyValueTableReportBlock? MapDehydration(HemosheetReportViewModel vm) =>
        new()
        {
            Title = "น้ำหนัก / Dehydration",
            Rows =
            [
                Lv("Pre Weight", FormatFloat(vm.Dehydration.PreWeight)),
                Lv("Post Weight", FormatFloat(vm.Dehydration.PostWeight)),
                Lv("Last Post", FormatFloat(vm.Dehydration.LastPostWeight)),
                Lv("Food Intake", FormatFloat(vm.Dehydration.FoodIntakeWeight)),
                Lv("Extra Fluid", FormatFloat(vm.Dehydration.ExtraFluid)),
                Lv("Blood Transfusion", FormatFloat(vm.Dehydration.BloodTransfusion)),
                Lv("UF Net", FormatFloat(vm.Dehydration.UfNet)),
            ],
        };

    public static KeyValueTableReportBlock? MapPrescription(HemosheetReportViewModel vm, IReadOnlyDictionary<string, bool> features)
    {
        var rows = new List<LabelValue>
        {
            Lv("Mode", vm.DialysisPrescription.Mode),
            Lv("Route", vm.DialysisPrescription.BloodAccessRoute),
            Lv("Dry Weight", FormatFloat(vm.DialysisPrescription.DryWeight)),
        };

        if (IsEnabled(features, "showDurationHours"))
        {
            rows.Add(Lv("Duration (hr)", FormatFloat(vm.DialysisPrescription.DurationHours)));
        }

        if (IsEnabled(features, "showDurationMinutes"))
        {
            rows.Add(Lv("Duration (min)", FormatFloat(vm.DialysisPrescription.DurationMinutes)));
        }

        if (IsEnabled(features, "showHdfColumns"))
        {
            rows.Add(Lv("HDF Type", vm.DialysisPrescription.HdfType));
        }

        if (IsEnabled(features, "showAcFields"))
        {
            rows.Add(Lv("Anticoagulant", vm.DialysisPrescription.Anticoagulant));
        }
        else if (IsEnabled(features, "showAcNotUsed"))
        {
            rows.Add(Lv("Anticoagulant", "ไม่ใช้"));
        }

        return new KeyValueTableReportBlock { Title = "คำสั่งการฟอก", Rows = rows };
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

        return new ChecklistTableReportBlock
        {
            Columns = ["รายการ", "หมายเหตุ"],
            Rows = items.Select(item => (IReadOnlyList<ChecklistCellValue>)
            [
                new ChecklistCheckboxCell { Checked = item.Checked, Label = item.Name },
                new ChecklistTextCell { Text = item.Text ?? "" },
            ]).ToList(),
        };
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

    public static TextReportBlock? MapNursesInShift(HemosheetReportViewModel vm, IReadOnlyDictionary<string, bool> features)
    {
        var text = IsEnabled(features, "showNurseInShiftNonPn")
            ? vm.NursesInShiftNonPn
            : vm.NursesInShift;

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

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd");

    private static string? FormatDateTime(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm");

    private static string? FormatFloat(float? value) =>
        value?.ToString("0.##");
}

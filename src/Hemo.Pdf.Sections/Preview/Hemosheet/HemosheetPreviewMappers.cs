using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Sections.Preview.Hemosheet;

public static class HemosheetPreviewMappers
{
    public static SubHeaderBarReportBlock? MapSubHeaderBar(HemosheetReportViewModel vm)
    {
        var fields = new List<LabelValue>();
        if (!string.IsNullOrWhiteSpace(vm.Patient.Diagnosis))
        {
            fields.Add(Lv("Diagnosis", vm.Patient.Diagnosis));
        }

        fields.Add(Lv("Drug Allergy", FormatAllergies(vm.Patient.Allergies)));

        return fields.Count == 0 ? null : new SubHeaderBarReportBlock { Fields = fields };
    }
    public static PatientInfoReportBlock? MapPatient(HemosheetReportViewModel vm) =>
        new()
        {
            Title = "ข้อมูลผู้ป่วย",
            // 2 columns to mirror Telerik Basic panel (not 3) — PDF + preview share this block.
            Columns =
            [
                [
                    Lv("ชื่อ-สกุล", vm.Patient.Name),
                    Lv("HN", vm.Patient.Hn),
                    Lv("เลขบัตรประชาชน", vm.Patient.IdentityNumber),
                    Lv("วันเกิด", FormatDate(vm.Patient.BirthDate)),
                    Lv("อายุ", vm.Patient.Age?.ToString()),
                    Lv("เพศ", vm.Patient.Sex),
                ],
                [
                    Lv("แพทย์", vm.Patient.DoctorName ?? vm.DoctorName),
                    Lv("แพ้ยา", FormatAllergies(vm.Patient.Allergies)),
                    Lv("สิทธิ์", vm.Patient.Coverage),
                    Lv("Diagnosis", vm.Patient.Diagnosis),
                    Lv("Underlying", vm.Patient.Underlying),
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

        var features = vm.LayoutContext.Features;
        if (IsEnabled(features, "showCreatorName") && !string.IsNullOrWhiteSpace(vm.CreatorName))
        {
            fields.Add(F("ผู้สร้าง", vm.CreatorName));
        }
        else if (!string.IsNullOrWhiteSpace(vm.CreatorName) && vm.LayoutContext.LayoutProfile == HemosheetLayoutProfile.Rama)
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

    public static FieldGridReportBlock? MapDehydration(
        HemosheetReportViewModel vm,
        IReadOnlyDictionary<string, bool> features)
    {
        var fields = new List<FieldGridField>
        {
            F("Pre Weight", FormatFloat(vm.Dehydration.PreWeight)),
            F("Post Weight", FormatFloat(vm.Dehydration.PostWeight)),
            F("Last Post", FormatFloat(vm.Dehydration.LastPostWeight)),
            F("Food Intake", FormatFloat(vm.Dehydration.FoodIntakeWeight)),
            F("Extra Fluid", FormatFloat(vm.Dehydration.ExtraFluid)),
            F("Blood Transfusion", FormatFloat(vm.Dehydration.BloodTransfusion)),
            F("UF Net", FormatFloat(vm.Dehydration.UfNet)),
            F("Total UF", FormatFloat(vm.Dehydration.TotalUf)),
            F("UF Estimate", FormatFloat(vm.Dehydration.UfEstimate)),
            F("UF Goal", FormatFloat(vm.Dehydration.UfGoal)),
        };

        if (IsEnabled(features, "showFlushNss")
            || vm.Dehydration.FlushNss is not null
            || vm.Dehydration.FlushNssTotal is not null)
        {
            fields.Add(F("Flush NSS", FormatFloat(vm.Dehydration.FlushNss)));
            fields.Add(F("Flush NSS Total", FormatFloat(vm.Dehydration.FlushNssTotal)));
        }

        return new FieldGridReportBlock
        {
            Title = "น้ำหนัก / Dehydration",
            // Telerik Basic dehydration panel is typically 2–3 columns, not a tall 4-col strip.
            Columns = 3,
            Fields = fields,
        };
    }

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

        // When layout features omit duration flags (older payloads), still show hours if present.
        if (!IsEnabled(features, "showDurationHours")
            && !IsEnabled(features, "showDurationMinutes")
            && vm.DialysisPrescription.DurationHours is not null)
        {
            fields.Add(F("Duration (hr)", FormatFloat(vm.DialysisPrescription.DurationHours)));
        }

        if (IsEnabled(features, "showHdfColumns"))
        {
            fields.Add(F("HDF Type", vm.DialysisPrescription.HdfType));
        }

        if (IsEnabled(features, "showAcFields"))
        {
            fields.Add(F("Anticoagulant", vm.DialysisPrescription.Anticoagulant));
            fields.Add(F("Initial", FormatAc(vm.DialysisPrescription.InitialAmount, vm.DialysisPrescription.InitialAmountMl)));
            fields.Add(F("Maintain", FormatAc(vm.DialysisPrescription.MaintainAmount, vm.DialysisPrescription.MaintainAmountMl)));
            fields.Add(F("AC/Session", FormatAc(vm.DialysisPrescription.AcPerSession, vm.DialysisPrescription.AcPerSessionMl)));
            if (!string.IsNullOrWhiteSpace(vm.DialysisPrescription.ReasonForRefraining))
            {
                fields.Add(F("Reason", vm.DialysisPrescription.ReasonForRefraining));
            }
        }
        else if (IsEnabled(features, "showAcNotUsed") || vm.IsAcNotUsed)
        {
            fields.Add(F("Anticoagulant", "ไม่ใช้"));
            if (!string.IsNullOrWhiteSpace(vm.DialysisPrescription.ReasonForRefraining))
            {
                fields.Add(F("Reason", vm.DialysisPrescription.ReasonForRefraining));
            }
        }
        else if (!string.IsNullOrWhiteSpace(vm.DialysisPrescription.Anticoagulant))
        {
            // Fallback when Features omitted AC flags but prescription has AC data.
            fields.Add(F("Anticoagulant", vm.DialysisPrescription.Anticoagulant));
            fields.Add(F("Initial", FormatAc(vm.DialysisPrescription.InitialAmount, vm.DialysisPrescription.InitialAmountMl)));
            fields.Add(F("Maintain", FormatAc(vm.DialysisPrescription.MaintainAmount, vm.DialysisPrescription.MaintainAmountMl)));
            fields.Add(F("AC/Session", FormatAc(vm.DialysisPrescription.AcPerSession, vm.DialysisPrescription.AcPerSessionMl)));
        }

        fields.Add(F("Dialyzer", vm.DialysisPrescription.Dialyzer));
        fields.Add(F("Surface Area", FormatFloat(vm.DialysisPrescription.DialyzerSurfaceArea)));
        fields.Add(F("Blood Flow", FormatFloat(vm.DialysisPrescription.BloodFlow)));
        fields.Add(F("Dialysate K", FormatFloat(vm.DialysisPrescription.DialysateK)));
        fields.Add(F("Dialysate Ca", FormatFloat(vm.DialysisPrescription.DialysateCa)));
        fields.Add(F("Na", FormatFloat(vm.DialysisPrescription.DialysateNa)));
        fields.Add(F("HCO3", FormatFloat(vm.DialysisPrescription.DialysateHco3)));
        fields.Add(F("Dialysate Temp", FormatFloat(vm.DialysisPrescription.DialysateTemperature)));
        fields.Add(F("Dialysate Flow", FormatFloat(vm.DialysisPrescription.DialysateFlowRate)));

        if (!string.IsNullOrWhiteSpace(vm.DialysisPrescription.Note))
        {
            fields.Add(new FieldGridField
            {
                Label = "Note",
                Value = vm.DialysisPrescription.Note,
                ColumnSpan = 3,
            });
        }

        return new FieldGridReportBlock
        {
            Title = "คำสั่งการฟอก",
            Columns = 3,
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
                Lv("Route", vm.DialysisPrescription.BloodAccessRoute),
                Lv("A Needle", FormatFloat(vm.AvShunt.ANeedleSize)),
                Lv("V Needle", FormatFloat(vm.AvShunt.VNeedleSize)),
            }
            : new List<LabelValue>
            {
                Lv("Site", vm.AvShunt.ShuntSite),
                Lv("Route", vm.DialysisPrescription.BloodAccessRoute),
                Lv("Catheter Length", FormatFloat(vm.AvShunt.CatheterLength)),
                Lv("Catheter Type", FormatCatheterType(vm.AvShunt.CatheterType)),
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
        IList<HemosheetAssessmentItemViewModel> items,
        bool ynLayout = false)
    {
        var expanded = ExpandAssessmentItems(items);
        if (expanded.Count == 0)
        {
            return null;
        }

        return ChecklistTablePreviewMapper.Map(new ChecklistTableModel
        {
            Title = title,
            Layout = ynLayout
                ? ChecklistTablePreviewMapper.LayoutYnColumns
                : ChecklistTablePreviewMapper.LayoutDefault,
            Items = expanded.ToList(),
        });
    }

    /// <summary>
    /// Default/Rama Telerik AssessmentTable: Topic | Pre Y/N | Re Y/N.
    /// </summary>
    public static ChecklistTableReportBlock? MapPreReAssessmentMatrix(HemosheetReportViewModel vm)
    {
        var preByName = IndexByName(vm.Assessments.Pre);
        var reByName = IndexByName(vm.Assessments.Re);
        if (preByName.Count == 0 && reByName.Count == 0)
        {
            return null;
        }

        var topics = preByName.Keys
            .Union(reByName.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = topics.Select(topic =>
        {
            preByName.TryGetValue(topic, out var pre);
            reByName.TryGetValue(topic, out var re);
            var notes = FirstNonEmpty(pre?.Text, re?.Text);
            return (
                Topic: FormatTopicLabel(topic),
                PreChecked: pre is null ? (bool?)null : pre.Checked,
                ReChecked: re is null ? (bool?)null : re.Checked,
                Notes: notes);
        }).ToList();

        return ChecklistTablePreviewMapper.MapPreReMatrix("Assessment (Pre / Re)", rows);
    }

    public static SectionRowReportBlock? MapTopLayoutRow(
        HemosheetReportViewModel vm,
        IReadOnlyDictionary<string, bool> features)
    {
        var leftBlocks = new List<ReportBlock>();
        var preVitals = MapPreVitals(vm);
        if (preVitals is not null)
        {
            leftBlocks.Add(preVitals);
        }

        var dehydration = MapDehydration(vm, features);
        if (dehydration is not null)
        {
            leftBlocks.Add(dehydration);
        }

        var prescription = MapPrescription(vm, features);
        if (leftBlocks.Count == 0 && prescription is null)
        {
            return null;
        }

        var blocks = new List<ReportBlock>
        {
            new ColumnStackReportBlock { Blocks = leftBlocks },
        };

        if (prescription is not null)
        {
            blocks.Add(prescription);
        }

        return new SectionRowReportBlock
        {
            Columns = blocks.Count,
            Blocks = blocks,
        };
    }

    public static FieldGridReportBlock? MapPreVitals(HemosheetReportViewModel vm)
    {
        var vital = vm.PreVital;
        if (vital is null)
        {
            return null;
        }

        return new FieldGridReportBlock
        {
            Title = "Predialysis Assessment",
            Columns = 3,
            Fields =
            [
                F("BP", FormatBp(vital.Bps, vital.Bpd)),
                F("PR", vital.Hr?.ToString()),
                F("RR", vital.Rr?.ToString()),
                F("BT", FormatFloat(vital.Temp)),
                F("Sat", FormatPercent(vital.SpO2)),
            ],
        };
    }

    public static KeyValueTableReportBlock? MapUfSummary(HemosheetReportViewModel vm) =>
        new()
        {
            Title = "สรุปน้ำ",
            Rows =
            [
                Lv("NSS", FormatMl(vm.Dehydration.FlushNssTotal)),
                Lv("Extra-fluid", FormatMl(vm.Dehydration.ExtraFluid)),
                Lv("Total UF", FormatMl(vm.Dehydration.TotalUf ?? vm.Dehydration.UfNet)),
            ],
        };

    public static DataGridReportBlock? MapNursingCarePlan(HemosheetReportViewModel vm)
    {
        var diagnosis = FindAssessmentText(vm.Assessments.Other, "nursing_diagnosis");
        var intervention = FindAssessmentText(vm.Assessments.Other, "nursing_intervention");
        var outcomes = FindAssessmentText(vm.Assessments.Other, "expected_outcomes");

        if (diagnosis is null && intervention is null && outcomes is null)
        {
            return null;
        }

        return new DataGridReportBlock
        {
            Title = "Nursing Care Plan",
            Columns = ["Nursing Diagnosis", "Nursing Intervention", "Expected Outcomes"],
            Rows = [[diagnosis ?? "—", intervention ?? "—", outcomes ?? "—"]],
        };
    }

    public static ChecklistClusterReportBlock? MapFooterChecklists(HemosheetReportViewModel vm)
    {
        var tables = new ChecklistTableReportBlock?[]
        {
            MapAssessmentGroup("Complication", ResolveFooterGroup(vm.Assessments.Post, "complication")),
            MapAssessmentGroup("Nursing management", ResolveFooterGroup(vm.Assessments.Post, "nursing")),
            MapAssessmentGroup("Health education", ResolveFooterGroup(vm.Assessments.Post, "health")),
            MapAssessmentGroup("Medication duration HD", ResolveFooterGroup(vm.Assessments.Other, "medication")),
        }.Where(t => t is not null).Cast<ChecklistTableReportBlock>().ToList();

        return tables.Count == 0 ? null : new ChecklistClusterReportBlock { Tables = tables };
    }

    public static PrePostHdNotesReportBlock? MapPrePostHdNotes(HemosheetReportViewModel vm)
    {
        var pre = vm.NurseRecords.FirstOrDefault()?.Content;
        var post = vm.NurseRecords.Skip(1).FirstOrDefault()?.Content
            ?? vm.DoctorRecords.FirstOrDefault()?.Content;

        if (string.IsNullOrWhiteSpace(pre) && string.IsNullOrWhiteSpace(post))
        {
            return null;
        }

        var preRecord = vm.NurseRecords.FirstOrDefault();
        var postRecord = vm.NurseRecords.Skip(1).FirstOrDefault();
        vm.SignatureNames.TryGetValue("pre_hd", out var preSig);
        vm.SignatureNames.TryGetValue("post_hd", out var postSig);

        static string? Prefer(string? author, string? signature) =>
            !string.IsNullOrWhiteSpace(author) ? author.Trim()
            : !string.IsNullOrWhiteSpace(signature) ? signature.Trim()
            : null;

        return new PrePostHdNotesReportBlock
        {
            PreHdContent = pre,
            PreHdSigner = Prefer(preRecord?.CreatorName, preSig),
            PostHdContent = post,
            PostHdSigner = Prefer(postRecord?.CreatorName, postSig),
        };
    }

    public static FieldGridReportBlock? MapPostVitals(HemosheetReportViewModel vm)
    {
        var vital = vm.PostVital;
        if (vital is null)
        {
            return null;
        }

        return new FieldGridReportBlock
        {
            Title = "Post Vital",
            Columns = 5,
            Fields =
            [
                F("BP", FormatBp(vital.Bps, vital.Bpd)),
                F("PR", vital.Hr?.ToString()),
                F("RR", vital.Rr?.ToString()),
                F("BT", FormatFloat(vital.Temp)),
                F("Sat", FormatPercent(vital.SpO2)),
            ],
        };
    }

    public static ChecklistTableReportBlock? MapAvfAssessment(HemosheetReportViewModel vm) =>
        MapAssessment("AVF/AVG", FilterAvfItems(vm.Assessments.Post), ynLayout: true);

    /// <summary>Post items that are not footer clusters or AVF Y/N rows.</summary>
    public static IList<HemosheetAssessmentItemViewModel> SelectPostBodyItems(
        IList<HemosheetAssessmentItemViewModel> post) =>
        HemosheetAssessmentFilters.SelectPostBodyItems(post);

    /// <summary>Other items that are not medication footer or nursing care-plan fields.</summary>
    public static IList<HemosheetAssessmentItemViewModel> SelectOtherBodyItems(
        IList<HemosheetAssessmentItemViewModel> other) =>
        HemosheetAssessmentFilters.SelectOtherBodyItems(other);

    private static ChecklistTableReportBlock? MapAssessmentGroup(string title, IList<HemosheetAssessmentItemViewModel> items) =>
        MapAssessment(title, items);

    private static IList<ChecklistItem> ExpandAssessmentItems(IList<HemosheetAssessmentItemViewModel> items)
    {
        var result = new List<ChecklistItem>();
        foreach (var item in items)
        {
            if (item.SelectedOptions.Count > 0)
            {
                var notesAttached = false;
                foreach (var option in item.SelectedOptions.Where(o => !string.IsNullOrWhiteSpace(o)))
                {
                    result.Add(new ChecklistItem
                    {
                        Label = option,
                        IsChecked = true,
                        Notes = !notesAttached && !string.IsNullOrWhiteSpace(item.Text) ? item.Text : null,
                    });
                    notesAttached = true;
                }

                continue;
            }

            result.Add(new ChecklistItem
            {
                Label = FormatTopicLabel(item.Name),
                IsChecked = item.Checked,
                Notes = string.IsNullOrWhiteSpace(item.Text) ? null : item.Text,
            });
        }

        return result;
    }

    private static IList<HemosheetAssessmentItemViewModel> ResolveFooterGroup(
        IList<HemosheetAssessmentItemViewModel> items,
        string groupKey)
    {
        var dottedPrefix = groupKey + ".";
        var dotted = items
            .Where(i => i.Name?.StartsWith(dottedPrefix, StringComparison.OrdinalIgnoreCase) == true)
            .Select(i => new HemosheetAssessmentItemViewModel
            {
                Name = i.Name![dottedPrefix.Length..],
                Checked = i.Checked,
                Text = i.Text,
                SelectedOptions = i.SelectedOptions,
            })
            .ToList();

        if (dotted.Count > 0)
        {
            return dotted;
        }

        var parent = items.FirstOrDefault(i =>
            string.Equals(i.Name, groupKey, StringComparison.OrdinalIgnoreCase));
        if (parent is null)
        {
            return [];
        }

        if (parent.SelectedOptions.Count > 0)
        {
            return parent.SelectedOptions
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => new HemosheetAssessmentItemViewModel
                {
                    Name = o,
                    Checked = true,
                    Text = parent.Text,
                })
                .ToList();
        }

        return parent.Checked
            ? [new HemosheetAssessmentItemViewModel { Name = groupKey, Checked = true, Text = parent.Text }]
            : [];
    }

    private static IList<HemosheetAssessmentItemViewModel> FilterAvfItems(IList<HemosheetAssessmentItemViewModel> items) =>
        HemosheetAssessmentFilters.SelectAvfItems(items);

    private static string? FindAssessmentText(IList<HemosheetAssessmentItemViewModel> items, string name) =>
        items.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))?.Text;

    private static Dictionary<string, HemosheetAssessmentItemViewModel> IndexByName(
        IList<HemosheetAssessmentItemViewModel> items) =>
        items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .GroupBy(i => i.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    private static string FormatTopicLabel(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "—" : name;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? FormatAllergies(IList<string>? allergies)
    {
        if (allergies is null || allergies.Count == 0)
        {
            return "ไม่มีแพ้ยา";
        }

        return string.Join(", ", allergies);
    }

    /// <summary>Matches <c>Wasenshi.HemoDialysisPro.Models.Enums.CatheterType</c> int values.</summary>
    private static string? FormatCatheterType(int? catheterType) =>
        catheterType switch
        {
            null => null,
            0 => "AV Fistula",
            1 => "AV Graft",
            2 => "Perm Cath",
            3 => "Double Lumen",
            _ => catheterType.Value.ToString(),
        };

    private static string? FormatAc(float? amount, float? amountMl)
    {
        if (amountMl.HasValue)
        {
            return $"{FormatFloat(amountMl)} ml";
        }

        return FormatFloat(amount);
    }

    private static string? FormatBp(int? bps, int? bpd) =>
        bps.HasValue || bpd.HasValue ? $"{bps}/{bpd}" : null;

    private static string? FormatPercent(float? value) =>
        value.HasValue ? $"{value:0.#}%" : null;

    private static string? FormatMl(float? value) =>
        value.HasValue ? $"{value:0.#} ml" : "0 ml";

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
            .Select(r => (IReadOnlyList<string>) [r.Focus ?? "—", r.A ?? "—", r.I ?? "—", r.E ?? "—"])
            .ToList();

        PadRows(rows, fixedLineCount, 4);

        return new DataGridReportBlock
        {
            Title = "Progress Note",
            Columns = ["Focus", "A", "I", "E"],
            Rows = rows,
        };
    }

    public static string? BuildNursesInShiftLine(
        HemosheetReportViewModel vm,
        IReadOnlyDictionary<string, bool> features)
    {
        if (!IsEnabled(features, "showNurseInShift"))
        {
            return null;
        }

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
            "Total" => 0.7f,
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
            ["Total"] = FormatFloat(record.UfTotal),
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

    public static IReadOnlyList<SignatureSlot> MapStaffSignatureSlots(HemosheetReportViewModel vm)
    {
        var roleMap = new (string Key, string Label)[]
        {
            ("dialysis_nurse", "พยาบาลฟอกไต"),
            ("na", "ผู้ช่วยพยาบาล"),
            ("nephrologist", "Nephrologist"),
            ("DialysisNurse", "พยาบาลฟอกไต"),
            ("Nurse", "พยาบาลฟอกไต"),
        };

        var slots = new List<SignatureSlot>();
        var usedLabels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (key, label) in roleMap)
        {
            if (!vm.SignatureNames.TryGetValue(key, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!usedLabels.Add(label))
            {
                continue;
            }

            slots.Add(new SignatureSlot { Role = label, Name = name });
        }

        return slots;
    }

    private static string? FormatDate(DateTime? value) =>
        value?.ToString("yyyy-MM-dd");

    private static string? FormatDateTime(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm");

    private static string? FormatFloat(float? value) =>
        value?.ToString("0.##");
}

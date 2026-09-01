using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;

namespace Hemo.Pdf.Core.Tests;

public class HprpHeaderLayoutEngineTests
{
    private static HprpHeaderPreset ThaiUrPreset()
    {
        var json = File.ReadAllText(
            Path.Combine(HprpTestAssets.TemplatesRoot(), "presets", "headers", "clinical-header-thaiur.json"));
        return JsonSerializer.Deserialize<HprpHeaderPreset>(json, HprpJson.Options)!;
    }

    [Fact]
    public void Build_WithSample_FillsTitleAndMeta()
    {
        var data = JsonDocument.Parse("""
            {
              "title": "Hemodialysis Review Hct and EPO",
              "header": {
                "patient": {
                  "name": "Sample Patient",
                  "hn": "6512620",
                  "age": 55,
                  "coverage": "สปสช.",
                  "identityNumber": "3101401131780",
                  "diagnosis": "ESRD",
                  "allergies": ["ไม่มีแพ้ยา"],
                  "hdPerWeek": "3"
                },
                "unit": { "fullName": "Unit A" }
              }
            }
            """).RootElement;

        var model = HprpHeaderLayoutEngine.Build(ThaiUrPreset(), data);
        Assert.Equal("Hemodialysis Review Hct and EPO", model.TitleText);
        Assert.Equal("Sample Patient", model.MetaLines[0].Value);
        Assert.Equal("6512620", model.MetaLines[1].Value);
        Assert.Equal("55", model.MetaLines[1].Value2);
        Assert.Contains(model.BottomFields, f => f.Label == "Diagnosis" && f.Value == "ESRD");
        Assert.Contains(model.BottomFields, f => f.Label == "Drug Allergy" && f.Value.Contains("ไม่มีแพ้ยา"));
        Assert.Equal(HprpHeaderBottomModes.Diagnosis, model.BottomMode);
    }

    [Fact]
    public void Build_ChecklistPatientBottomMode_UsesPatientFields()
    {
        var data = JsonDocument.Parse("""
            {
              "title": "Hemodialysis Progress note",
              "header": {
                "patient": {
                  "name": "จินนี่ วัลลีย์",
                  "hn": "6512620",
                  "age": 31,
                  "coverage": "—",
                  "diagnosis": "ESRD",
                  "hdPerWeek": "2"
                },
                "unit": { "fullName": "Hemodialysis Unit" }
              },
              "patient": {
                "birthDateLabel": "03/01/1995 (31 years)",
                "sessionsPerWeekLabel": "2 times/week",
                "dialysisDays": "Wed Fri",
                "dialysisMode": "HD",
                "underlying": "—"
              }
            }
            """).RootElement;

        var model = HprpHeaderLayoutEngine.Build(
            ThaiUrPreset(),
            data,
            bottomModeOverride: HprpHeaderBottomModes.ChecklistPatient);

        Assert.Equal(HprpHeaderBottomModes.ChecklistPatient, model.BottomMode);
        Assert.Equal(10.8f, model.BottomRowHeightMm);
        Assert.Equal(2, model.BottomRowCount);
        Assert.DoesNotContain(model.BottomFields, f => f.Label == "Diagnosis");
        Assert.Contains(model.BottomFields, f => f.Label == "DOB" && f.Value.Contains("1995"));
        Assert.Contains(model.BottomFields, f => f.Label == "Dialysis days" && f.Value == "Wed Fri");
        Assert.Contains(model.BottomFields, f => f.Label == "Mode" && f.Value == "HD");
    }

    [Fact]
    public void Build_WithoutData_KeepsEmptyGridLabels()
    {
        var model = HprpHeaderLayoutEngine.Build(ThaiUrPreset(), null, "Fallback Title");
        Assert.Equal("Fallback Title", model.TitleText);
        Assert.Equal(4, model.MetaLines.Count);
        Assert.All(model.MetaLines, m => Assert.Equal("", m.Value));
        Assert.True(model.BottomFields.Count >= 2);
    }

    [Fact]
    public void StudioJson_HeaderPreset_RoundTrip()
    {
        var json = """
            {
              "id": "hdr",
              "type": "header",
              "box": { "xMm": 0, "yMm": 0, "wMm": 206, "hMm": 27 },
              "headerPreset": {
                "id": "inline",
                "columns": [
                  { "id": "logo", "kind": "logo", "widthMm": 40 },
                  { "id": "title", "kind": "title", "widthMm": 100 },
                  { "id": "meta", "kind": "meta", "widthMm": 66 }
                ],
                "metaLines": [
                  { "id": "name", "label": "Name", "bind": "$.header.patient.name" }
                ],
                "bottomFields": [
                  { "id": "dx", "label": "Diagnosis", "bind": "$.header.patient.diagnosis", "weight": 2 }
                ]
              }
            }
            """;
        var el = JsonSerializer.Deserialize<Hemo.Pdf.Core.Hprp.Table.HprpDesignerElement>(json, HprpJson.Options)!;
        Assert.NotNull(el.HeaderPreset);
        Assert.Equal(40f, el.HeaderPreset!.Columns[0].WidthMm);
        Assert.Equal(100f, el.HeaderPreset.Columns[1].WidthMm);
        var model = HprpHeaderLayoutEngine.Build(el.HeaderPreset, null, "T");
        Assert.Equal("T", model.TitleText);
        Assert.Single(model.MetaLines);
    }
}

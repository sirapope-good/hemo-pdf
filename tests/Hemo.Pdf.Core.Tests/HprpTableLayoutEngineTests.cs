using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Tests;

public class HprpTableLayoutEngineTests
{
    private static readonly JsonElement Sample = JsonDocument.Parse("""
        {
          "months": [
            {
              "monthIndex": 2,
              "monthLabel": "ก.พ.",
              "entries": [
                { "dayLabel": "06-02-2023", "hb": "9.5", "hct": "28.0", "labIsHistorical": true },
                { "dayLabel": "20-02-2023", "hb": "9.8", "hct": "29.4", "labIsHistorical": false }
              ]
            }
          ]
        }
        """).RootElement;

    private static HprpTablePreset AnnualPreset => new()
    {
        Id = "hct-epo-annual-v1",
        DisplayName = "Annual",
        RowMode = HprpTableRowModes.Annual,
        GroupCount = 12,
        SlotsPerGroup = 3,
        DateColumns = new HprpTableDateColumns { MonthWeight = 0.45f, DayWeight = 1.35f },
        Columns =
        [
            new HprpTableColumnDef { Id = "hb", LabelKey = "colHb", Weight = 1f, Center = true, IsLab = true },
            new HprpTableColumnDef { Id = "hct", LabelKey = "colHct", Weight = 1f, Center = true, IsLab = true },
        ],
    };

    private static IReadOnlyList<HprpTableBinding> AnnualBindings =>
    [
        new HprpTableBinding { Path = "months[].monthLabel", Column = "month", Context = "group-label" },
        new HprpTableBinding { Path = "months[].entries[].dayLabel", Column = "day", Context = "entry" },
        new HprpTableBinding { Path = "months[].entries[].hb", Column = "hb", Context = "entry" },
        new HprpTableBinding { Path = "months[].entries[].hct", Column = "hct", Context = "entry" },
        new HprpTableBinding { Path = "months[].entries[].labIsHistorical", Column = "lab", Context = "lab-historical" },
    ];

    [Fact]
    public void Build_AnnualMode_ProducesTwelveGroupsTimesSlots()
    {
        var resolved = HprpTablePresetResolver.Resolve(AnnualPreset);
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["colDate"] = "DATE",
            ["colHb"] = "Hb",
            ["colHct"] = "Hct",
        };

        var model = HprpTableLayoutEngine.Build(resolved, AnnualBindings, labels, Sample, boxHeightMm: 228f);

        Assert.Equal(12 * 3, model.Rows.Count);
        Assert.Contains(model.Rows, r => r.GroupIndex == 0 && r.GroupLabel == "ก.พ.");
        var febSlot = Assert.Single(model.Rows, r => r.GroupIndex == 0 && r.SlotIndex == 0);
        Assert.Contains(febSlot.Cells, c => c.Text == "9.5");
    }

    [Fact]
    public void MergeColumns_OverrideReplacesWeight()
    {
        var merged = HprpTablePresetResolver.MergeColumns(
            AnnualPreset.Columns,
            [new HprpTableColumnDef { Id = "hb", LabelKey = "colHb", Weight = 2.5f }]);

        Assert.Equal(2.5f, merged[0].Weight);
        Assert.Equal("hct", merged[1].Id);
    }

    [Fact]
    public void MergeColumns_OverridePreservesCellKind()
    {
        var bas = new List<HprpTableColumnDef>
        {
            new() { Id = "progress", Weight = 2f, CellKind = HprpTableCellKinds.SoapProgress },
            new() { Id = "date", Weight = 1f, CellKind = HprpTableCellKinds.Text },
        };
        var merged = HprpTablePresetResolver.MergeColumns(
            bas,
            [new HprpTableColumnDef { Id = "progress", Weight = 3f }]);

        Assert.Equal(3f, merged[0].Weight);
        Assert.Equal(HprpTableCellKinds.SoapProgress, merged[0].CellKind);
    }

    [Fact]
    public void Resolve_MergesElementBandWeightsOntoPresetChrome()
    {
        var preset = new HprpTablePreset
        {
            Id = "progress-note-soap-v1",
            RowMode = HprpTableRowModes.Freedom,
            FreedomRowCount = 2,
            Columns =
            [
                new HprpTableColumnDef { Id = "progress", CellKind = HprpTableCellKinds.SoapProgress, Weight = 2f },
            ],
            Chrome = new HprpChrome
            {
                Border = "thin",
                HeaderFill = HprpChrome.BrandingHeaderFill,
                BandWeights = [1f, 2.5f, 1f, 1f],
            },
        };
        var element = new HprpDesignerElement
        {
            Id = "soap",
            Type = HprpDesignerElementTypes.ConfigTable,
            Chrome = new HprpChrome { BandWeights = [1f, 4f, 1f, 1f] },
        };

        var resolved = HprpTablePresetResolver.Resolve(preset, element);

        Assert.NotNull(resolved.Chrome);
        Assert.Equal("thin", resolved.Chrome!.Border);
        Assert.Equal(HprpChrome.BrandingHeaderFill, resolved.Chrome.HeaderFill);
        Assert.Equal([1f, 4f, 1f, 1f], resolved.Chrome.BandWeights);
        Assert.Equal(HprpTableCellKinds.SoapProgress, resolved.Columns[0].CellKind);
    }

    [Fact]
    public void ValidateDesignerPackage_PassesForClinical05ChecklistMatrix()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "clinical-05-progress-note-checklist");
        var manifest = JsonSerializer.Deserialize<HprpManifest>(
            File.ReadAllText(Path.Combine(dir, "manifest.json")),
            HprpJson.Options)!;
        var layout = JsonSerializer.Deserialize<HprpLayout>(
            File.ReadAllText(Path.Combine(dir, "layout.json")),
            HprpJson.Options)!;

        var package = new HprpPackage { Manifest = manifest, Layout = layout };
        var result = HprpValidator.Validate(package);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var grid = Assert.Single(layout.Elements!, e => e.Id == "grid");
        Assert.Equal(HprpDesignerElementTypes.ConfigTable, grid.Type);
        Assert.Equal("progress-note-checklist-matrix-v1", grid.PresetId);
    }

    [Fact]
    public void Build_MatrixMode_BuildsItemRowsFromChecklistSample()
    {
        var samplePath = Path.Combine(
            HprpTestAssets.TemplatesRoot(),
            "reports",
            "clinical-05-progress-note-checklist",
            "sample.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(samplePath));
        var preset = new HprpTablePreset
        {
            Id = "progress-note-checklist-matrix-v1",
            RowMode = HprpTableRowModes.Matrix,
            Columns = [new HprpTableColumnDef { Id = "item", Title = "Item", Weight = 1.6f }],
        };
        var resolved = HprpTablePresetResolver.Resolve(preset);
        var model = HprpTableLayoutEngine.Build(resolved, [], new Dictionary<string, string>(), doc.RootElement, 118f);

        Assert.Contains(model.Rows, r => r.Kind == "matrix" && r.Cells[0].Text == "General symptoms");
        Assert.Contains(model.Rows, r => r.Kind == "group" && r.GroupLabel == "Volume status");
    }

    [Fact]
    public void ValidateDesignerPackage_PassesForClinical05SoapConfigTable()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "clinical-05-progress-note");
        var manifest = JsonSerializer.Deserialize<HprpManifest>(
            File.ReadAllText(Path.Combine(dir, "manifest.json")),
            HprpJson.Options)!;
        var layout = JsonSerializer.Deserialize<HprpLayout>(
            File.ReadAllText(Path.Combine(dir, "layout.json")),
            HprpJson.Options)!;

        var package = new HprpPackage { Manifest = manifest, Layout = layout };
        var result = HprpValidator.Validate(package);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var soap = Assert.Single(layout.Elements!, e => e.Id == "soap");
        Assert.Equal(HprpDesignerElementTypes.ConfigTable, soap.Type);
        Assert.Equal("progress-note-soap-v1", soap.PresetId);
    }

    [Fact]
    public void ValidateDesignerPackage_PassesForClinical01()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "clinical-01-hct-epo");
        var manifest = JsonSerializer.Deserialize<HprpManifest>(
            File.ReadAllText(Path.Combine(dir, "manifest.json")),
            HprpJson.Options)!;
        var layout = JsonSerializer.Deserialize<HprpLayout>(
            File.ReadAllText(Path.Combine(dir, "layout.json")),
            HprpJson.Options)!;

        var package = new HprpPackage { Manifest = manifest, Layout = layout };
        var result = HprpValidator.Validate(package);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Resolve_UsesInlineTablePresetColumnWeights()
    {
        var inline = new HprpTablePreset
        {
            Id = "inline",
            RowMode = HprpTableRowModes.Annual,
            GroupCount = 12,
            SlotsPerGroup = 3,
            DateColumns = new HprpTableDateColumns { MonthWeight = 0.2f, DayWeight = 2.0f },
            Columns =
            [
                new HprpTableColumnDef { Id = "hb", Weight = 3.5f, Center = true, IsLab = true },
                new HprpTableColumnDef { Id = "hct", Weight = 0.5f, Center = true, IsLab = true },
            ],
        };
        var element = new HprpDesignerElement
        {
            Id = "annual",
            Type = "config-table",
            TablePreset = inline,
        };

        var resolved = HprpTablePresetResolver.Resolve(inline, element);
        Assert.Equal(0.2f, resolved.DateColumns.MonthWeight);
        Assert.Equal(2.0f, resolved.DateColumns.DayWeight);
        Assert.Equal(3.5f, resolved.Columns[0].Weight);
        Assert.Equal(0.5f, resolved.Columns[1].Weight);
    }

    [Fact]
    public void Build_SlotHeightsFillBoxExactly()
    {
        var resolved = HprpTablePresetResolver.Resolve(AnnualPreset);
        const float boxH = 228f;
        var model = HprpTableLayoutEngine.Build(
            resolved,
            AnnualBindings,
            new Dictionary<string, string>(),
            Sample,
            boxH);

        var total = model.HeaderHeightMm + model.BlockHeightMm * 12;
        Assert.InRange(total, boxH - 0.05f, boxH + 0.05f);
    }

    [Fact]
    public void FreedomStaticRows_UsesHardcodedCells()
    {
        var preset = new HprpTablePreset
        {
            Id = "nhso",
            RowMode = HprpTableRowModes.Freedom,
            FreedomRowCount = 3,
            Columns =
            [
                new HprpTableColumnDef { Id = "a", Title = "สปสช", Weight = 1.6f, Center = true },
                new HprpTableColumnDef { Id = "b", Title = "เข็ม", Weight = 1.4f, Center = true },
            ],
            StaticRows =
            [
                ["Hb < 10", "2"],
                ["Hb 10-11.9", "1"],
                ["Hb ≥ 12", "0"],
            ],
        };
        var resolved = HprpTablePresetResolver.Resolve(preset);
        var model = HprpTableLayoutEngine.Build(
            resolved,
            [],
            new Dictionary<string, string>(),
            null,
            22f);

        Assert.Equal(3, model.Rows.Count);
        Assert.Equal("Hb < 10", model.Rows[0].Cells[0].Text);
        Assert.Equal("2", model.Rows[0].Cells[1].Text);
        Assert.Equal(2, model.HeaderLabels.Count);
        Assert.DoesNotContain(model.HeaderLabels, h => h.Contains("วัน"));
        Assert.InRange(model.HeaderHeightMm + model.SlotHeightMm * 3, 21.9f, 22.1f);
    }

    [Fact]
    public void StudioPreviewJson_InlineTablePreset_DeserializesWeights()
    {
        var json = """
            {
              "manifest": { "id": "clinical-01-hct-epo", "layoutMode": "designer", "version": "1" },
              "layout": {
                "page": { "size": "A4", "marginMm": 2 },
                "elements": [
                  {
                    "id": "annual",
                    "type": "config-table",
                    "box": { "xMm": 0, "yMm": 29, "wMm": 206, "hMm": 228 },
                    "tablePreset": {
                      "id": "inline",
                      "rowMode": "annual",
                      "groupCount": 12,
                      "slotsPerGroup": 3,
                      "dateColumns": { "monthWeight": 0.2, "dayWeight": 2.5 },
                      "columns": [
                        { "id": "hb", "weight": 4.5, "center": true, "isLab": true },
                        { "id": "hct", "weight": 0.4, "center": true, "isLab": true }
                      ]
                    },
                    "bindings": []
                  }
                ]
              },
              "labels": {}
            }
            """;
        var layout = JsonSerializer.Deserialize<HprpLayout>(
            JsonDocument.Parse(json).RootElement.GetProperty("layout").GetRawText(),
            HprpJson.Options)!;
        var el = Assert.Single(layout.Elements);
        Assert.NotNull(el.TablePreset);
        var resolved = HprpTablePresetResolver.Resolve(el.TablePreset!, el);
        Assert.Equal(0.2f, resolved.DateColumns.MonthWeight, 3);
        Assert.Equal(2.5f, resolved.DateColumns.DayWeight, 3);
        Assert.Equal(4.5f, resolved.Columns[0].Weight, 3);
        Assert.Equal(0.4f, resolved.Columns[1].Weight, 3);
    }
}

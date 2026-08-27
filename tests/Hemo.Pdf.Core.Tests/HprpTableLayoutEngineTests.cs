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
    public void ValidateDesignerPackage_PassesForClinical01Designer()
    {
        var dir = Path.Combine(HprpTestAssets.TemplatesRoot(), "reports", "clinical-01-hct-epo-designer");
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
}

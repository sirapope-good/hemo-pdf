using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

namespace Hemo.Pdf.Core.Tests;

public class HprpWidgetRecipeCatalogTests
{
    [Fact]
    public void Describe_AnnualTableRecipe_HasBindFieldsAndDefaultPlan()
    {
        var catalog = HprpStudioCatalog.Describe();
        var json = System.Text.Json.JsonSerializer.Serialize(catalog, HprpJson.Options);

        Assert.Contains("clinical.hct-epo-annual-table", json);
        Assert.Contains("\"bind\":\"hb\"", json.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);

        var recipe = HprpWidgetRecipes.ClinicalHctEpoAnnualTable;
        Assert.Equal(HprpWidgetRecipe.KindDense, recipe.Kind);
        Assert.Contains(ClinicalReportCatalog.HctEpo, recipe.AllowedOn);
        Assert.Equal(6, recipe.BindFields.Count);
        Assert.Equal(6, recipe.DefaultColumnPlan.Count);
        Assert.Equal("hb", recipe.DefaultColumnPlan[0].Bind);
        Assert.Equal("hct", recipe.DefaultColumnPlan[1].Bind);
        Assert.Contains("columnPlan", recipe.InspectorFields);
    }

    [Fact]
    public void CopayRecipe_HasChromeInspector_NoColumnPlan()
    {
        var recipe = HprpWidgetRecipes.ClinicalHctEpoCopay;
        Assert.Empty(recipe.BindFields);
        Assert.Empty(recipe.DefaultColumnPlan);
        Assert.DoesNotContain("columnPlan", recipe.InspectorFields);
        Assert.Contains("chrome.headerFill", recipe.InspectorFields);
        Assert.Contains("nhso", recipe.LabelKeys);
    }

    [Fact]
    public void DialysisRecipe_HasSectionsSlotAndDefaultColumns()
    {
        var recipe = HprpWidgetRecipes.HemosheetDialysisRecords;
        Assert.Equal(HprpWidgetRecipe.SlotSections, recipe.Slot);
        Assert.Contains(ClinicalReportCatalog.HemodialysisRecord, recipe.AllowedOn);
        Assert.Equal(12, recipe.DefaultColumns.Count);
        Assert.Equal("เวลา", recipe.DefaultColumns[0]);
        Assert.NotNull(recipe.DefaultColumnsWhen);
        Assert.True(recipe.DefaultColumnsWhen!.ContainsKey("feature:showHdfColumns"));
        Assert.Contains("columns", recipe.InspectorFields);
        Assert.Contains("columnsWhen", recipe.InspectorFields);
        Assert.DoesNotContain("columnPlan", recipe.InspectorFields);
        Assert.Empty(recipe.DefaultColumnPlan);
    }

    [Fact]
    public void HemosheetWidgets_UseSectionsSlot()
    {
        foreach (var recipe in HprpWidgetRecipes.Dense.Where(r => r.Id.StartsWith("hemosheet.", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.Equal(HprpWidgetRecipe.SlotSections, recipe.Slot);
            Assert.Contains(ClinicalReportCatalog.HemodialysisRecord, recipe.AllowedOn);
        }
    }
}

public class HctEpoAnnualColumnPlanTests
{
    [Fact]
    public void Resolve_EmptyPlan_MatchesDefaultHbThenHct()
    {
        var columns = HctEpoAnnualColumnPlan.Resolve(new HprpLayoutNode
        {
            Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable,
        });

        Assert.Equal(
            new[] { "hb", "hct", "epoName", "frequencyText", "injectionDate", "remarks" },
            columns.Select(c => c.Bind).ToArray());
        Assert.True(columns[0].IsLab);
        Assert.True(columns[0].Center);
    }

    [Fact]
    public void Resolve_SwappedHbHct_ReadsCellsInPlanOrder()
    {
        var defaults = HprpWidgetRecipes.ClinicalHctEpoAnnualTable.DefaultColumnPlan;
        var swapped = new List<HprpColumnPlanItem>
        {
            new() { Bind = "hct", LabelKey = "colHct" },
            new() { Bind = "hb", LabelKey = "colHb" },
        };
        swapped.AddRange(defaults.Skip(2));

        var columns = HctEpoAnnualColumnPlan.Resolve(new HprpLayoutNode
        {
            Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable,
            ColumnPlan = swapped,
        });

        Assert.Equal("hct", columns[0].Bind);
        Assert.Equal("hb", columns[1].Bind);

        var entry = new Hemo.Pdf.Core.Models.Clinical.HctEpoMonthEntry
        {
            Hb = "9.5",
            Hct = "28.0",
            EpoName = "Nesp",
        };
        Assert.Equal("28.0", HctEpoAnnualColumnPlan.ReadCell(entry, columns[0].Bind));
        Assert.Equal("9.5", HctEpoAnnualColumnPlan.ReadCell(entry, columns[1].Bind));
        Assert.Equal("Nesp", HctEpoAnnualColumnPlan.ReadCell(entry, "epoName"));
        Assert.Null(HctEpoAnnualColumnPlan.ReadCell(entry, ""));
    }

    [Fact]
    public void SamplePayload_Clinical01_Exists()
    {
        var root = HprpTestAssets.TemplatesRoot();
        var data = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HctEpo);
        Assert.True(data.HasValue);
        Assert.Equal(JsonValueKind.Object, data!.Value.ValueKind);
        Assert.True(data.Value.TryGetProperty("months", out _));
    }

    [Fact]
    public void SamplePayload_Clinical03_Exists_AndVariantSetsLayoutProfile()
    {
        var root = HprpTestAssets.TemplatesRoot();
        Assert.Contains(ClinicalReportCatalog.HemodialysisRecord, HprpStudioSamplePayloads.KnownTemplateIds);

        var thaiur = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HemodialysisRecord, "thaiur");
        Assert.True(thaiur.HasValue);
        Assert.Equal(
            "ThaiUr",
            thaiur!.Value.GetProperty("layoutContext").GetProperty("layoutProfile").GetString());

        var rama = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HemodialysisRecord, "rama");
        Assert.Equal(
            "Rama",
            rama!.Value.GetProperty("layoutContext").GetProperty("layoutProfile").GetString());

        var def = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HemodialysisRecord, "default");
        Assert.Equal(
            "Default",
            def!.Value.GetProperty("layoutContext").GetProperty("layoutProfile").GetString());
    }

    [Fact]
    public void ApplyHemosheetPreviewContext_PrefersManifestLayoutProfile()
    {
        var root = HprpTestAssets.TemplatesRoot();
        var sample = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HemodialysisRecord, "default");
        Assert.True(sample.HasValue);

        var overlay = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.HemodialysisRecord,
                Variant = "thaiur",
                LayoutProfile = "ThaiUr",
            },
            Layout = new HprpLayout(),
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            SourcePath = "",
        };

        var adjusted = HprpStudioSamplePayloads.ApplyHemosheetPreviewContext(sample!.Value, overlay, "default");
        Assert.Equal(
            "ThaiUr",
            adjusted.GetProperty("layoutContext").GetProperty("layoutProfile").GetString());
    }

    [Fact]
    public void Clinical03_ProductionLayouts_HaveNoExperimentalKeys()
    {
        var root = HprpTestAssets.TemplatesRoot();
        foreach (var variant in new[] { "default", "rama", "thaiur" })
        {
            var path = Path.Combine(root, "reports", ClinicalReportCatalog.HemodialysisRecord, "variants", variant, "layout.json");
            Assert.True(File.Exists(path), path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.TryGetProperty("sections", out var sections));
            foreach (var section in sections.EnumerateArray())
            {
                Assert.False(section.TryGetProperty("columnPlan", out _), $"{variant}: columnPlan is clinical-01 only");
                Assert.False(section.TryGetProperty("experimental", out _), $"{variant}: experimental key");
                Assert.False(section.TryGetProperty("x-", out _), $"{variant}: x- key");
            }
        }
    }
}

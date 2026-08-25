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
        var root = FindTemplatesRoot();
        var data = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.HctEpo);
        Assert.True(data.HasValue);
        Assert.Equal(JsonValueKind.Object, data!.Value.ValueKind);
        Assert.True(data.Value.TryGetProperty("months", out _));
    }

    private static string FindTemplatesRoot()
    {
        var outputCandidate = Path.Combine(AppContext.BaseDirectory, "assets", "templates");
        if (Directory.Exists(Path.Combine(outputCandidate, "reports", "clinical-01-hct-epo")))
            return outputCandidate;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "templates");
            if (Directory.Exists(Path.Combine(candidate, "reports", "clinical-01-hct-epo")))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("assets/templates/reports/clinical-01-hct-epo not found.");
    }
}

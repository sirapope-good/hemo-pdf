using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpRowBinderTests
{
    [Fact]
    public void Bind_Row_ProducesSectionRowWithWidths()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-10-patient-data",
                DisplayName = "X",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "row",
                        GapMm = 3,
                        Cells =
                        [
                            new HprpCellNode
                            {
                                Width = "40%",
                                Nodes =
                                [
                                    new HprpLayoutNode { Type = "text", Content = JsonSerializer.SerializeToElement("L") },
                                ],
                            },
                            new HprpCellNode
                            {
                                Width = "*",
                                Nodes =
                                [
                                    new HprpLayoutNode { Type = "text", Content = JsonSerializer.SerializeToElement("R") },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        var block = Assert.IsType<SectionRowReportBlock>(HprpBinder.Bind(package, data: null).Single());
        Assert.Equal(2, block.Blocks.Count);
        Assert.Equal(["40%", "*"], block.ColumnWidths);
        Assert.Equal(3, block.GapMm);
        Assert.Equal("L", Assert.IsType<TextReportBlock>(block.Blocks[0]).Content);
        Assert.Equal("R", Assert.IsType<TextReportBlock>(block.Blocks[1]).Content);
    }

    [Fact]
    public void Validator_RejectsUnknownNestedType()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "x",
                DisplayName = "X",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "row",
                        Cells =
                        [
                            new HprpCellNode
                            {
                                Nodes = [new HprpLayoutNode { Type = "not-a-block" }],
                            },
                        ],
                    },
                ],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown block type"));
    }

    [Fact]
    public void Validator_AcceptsClinical10DesignerLayout()
    {
        var dir = HprpTestAssets.PackageDir(ClinicalReportCatalog.PatientData);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.True(HprpValidator.Validate(package).IsValid, string.Join("\n", HprpValidator.Validate(package).Errors));
        Assert.Equal(HprpLayoutModes.Designer, package.Manifest.LayoutMode);
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, "data-grid", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, "header", StringComparison.OrdinalIgnoreCase));
    }
}

public class HprpRowWidthTests
{
    [Fact]
    public void ParseRowCellWidths_PercentAndStar()
    {
        var parsed = HprpChrome.ParseRowCellWidths(["40%", "*"]);
        Assert.Equal(2, parsed.Count);
        Assert.False(parsed[0].ConstantMm);
        Assert.Equal(40, parsed[0].Value);
        Assert.False(parsed[1].ConstantMm);
        Assert.Equal(60, parsed[1].Value);
    }

    [Fact]
    public void ParseRowCellWidths_ConstantMm()
    {
        var parsed = HprpChrome.ParseRowCellWidths(["32mm", "*"]);
        Assert.True(parsed[0].ConstantMm);
        Assert.Equal(32, parsed[0].Value);
        Assert.False(parsed[1].ConstantMm);
    }
}

public class HprpLayoutPlanRowTests
{
    [Fact]
    public void ResolveNodes_KeepsRowBetweenWidgets()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-01-hct-epo",
                DisplayName = "x",
                DataAdapter = HprpDataAdapterIds.Clinical01HctEpo,
            },
            Layout = new HprpLayout
            {
                Header = new HprpLayoutNode { Widget = HprpWidgetIds.ThaiUrHeader },
                Body =
                [
                    new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable },
                    new HprpLayoutNode
                    {
                        Type = "row",
                        Cells =
                        [
                            new HprpCellNode
                            {
                                Width = "*",
                                Nodes = [new HprpLayoutNode { Type = "text", Content = JsonSerializer.SerializeToElement("n") }],
                            },
                        ],
                    },
                    new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoCopay },
                ],
            },
        };

        var nodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        Assert.Equal(4, nodes.Count);
        Assert.Equal("row", nodes[2].Type);
    }
}

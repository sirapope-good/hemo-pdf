using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Tests;

public class HprpBinderTests
{
    [Fact]
    public void Bind_FieldGridAndFlatten_UsesJsonPathAndLabels()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-07-lab",
                DisplayName = "Laboratory Record",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "text",
                        Style = "title",
                        Bind = "$title",
                    },
                    new HprpLayoutNode
                    {
                        Type = "field-grid",
                        Columns = 2,
                        Fields =
                        [
                            new HprpFieldNode
                            {
                                Label = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["$label"] = "hn" }),
                                Bind = "$.patient.hn",
                            },
                        ],
                    },
                    new HprpLayoutNode
                    {
                        Type = "key-value-table",
                        AppendFlatten = true,
                    },
                    new HprpLayoutNode
                    {
                        Type = "data-grid",
                        BindRows = "$.rows",
                        When = JsonSerializer.SerializeToElement("$.rows.length > 0"),
                    },
                ],
            },
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["th"] = new Dictionary<string, string> { ["hn"] = "HN" },
            },
        };

        var data = JsonSerializer.SerializeToElement(new
        {
            patient = new { hn = "HN-1" },
            note = "ok",
            rows = new[] { new { a = "1" } },
        });
        var context = new PdfReportContext
        {
            ReportTemplateId = "clinical-07-lab",
            TenantCode = "local",
            Metadata = new() { Title = "Laboratory Record" },
        };

        var blocks = HprpBinder.Bind(package, data, context, "th");

        Assert.Contains(blocks, b => b is TextReportBlock text && text.Content == "Laboratory Record");
        var grid = Assert.IsType<FieldGridReportBlock>(blocks.OfType<FieldGridReportBlock>().Single());
        Assert.Equal("HN", grid.Fields[0].Label);
        Assert.Equal("HN-1", grid.Fields[0].Value);
        var kv = Assert.IsType<KeyValueTableReportBlock>(blocks.OfType<KeyValueTableReportBlock>().Single());
        Assert.Contains(kv.Rows, r => r.Label == "note" && r.Value == "ok");
        Assert.Contains(blocks, b => b is DataGridReportBlock);
    }

    [Fact]
    public void Bind_LabMatrix_UsesColumnHeadersBind()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.Lab,
                DisplayName = "Laboratory Record",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "data-grid",
                        ColumnHeadersBind = "$.columnHeaders",
                        BindRows = "$.rows",
                    },
                ],
            },
        };

        var data = JsonSerializer.SerializeToElement(new
        {
            columnHeaders = new[] { "Lab item", "01/01/2026" },
            rows = new[] { new[] { "HCT", "32" } },
        });

        var blocks = HprpBinder.Bind(package, data, null, "th");
        var grid = Assert.IsType<DataGridReportBlock>(blocks.Single());
        Assert.Equal(["Lab item", "01/01/2026"], grid.Columns);
        Assert.Equal("HCT", grid.Rows[0][0]);
        Assert.Equal("32", grid.Rows[0][1]);
    }

    [Fact]
    public void Bind_EmptyLabRows_StillRendersGrid()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.Lab,
                DisplayName = "Laboratory Record",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "data-grid",
                        ColumnHeadersBind = "$.columnHeaders",
                        BindRows = "$.rows",
                    },
                ],
            },
        };

        var data = JsonSerializer.SerializeToElement(new
        {
            columnHeaders = new[] { "", "DATE", "DATE" },
            rows = new[]
            {
                new[] { "1 Month", "", "" },
                new[] { "Hb", "", "" },
            },
        });

        var grid = Assert.IsType<DataGridReportBlock>(HprpBinder.Bind(package, data).Single());
        Assert.Equal(2, grid.Rows.Count);
        Assert.Equal("1 Month", grid.Rows[0][0]);
        Assert.Equal("", grid.Rows[1][1]);
    }

    [Fact]
    public void Bind_DataGridChrome_ParsesColumnWidthsAndHeaderFill()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.Lab,
                DisplayName = "Laboratory Record",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Type = "data-grid",
                        BindRows = "$.rows",
                        ColumnHeaders = ["A", "B"],
                        Chrome = new HprpChrome
                        {
                            HeaderFill = "#C0C0FF",
                            Border = "medium",
                            FontSize = 8,
                            ColumnWidths = ["2", "*"],
                        },
                    },
                ],
            },
        };

        var data = JsonSerializer.SerializeToElement(new
        {
            rows = new[] { new[] { "1", "2" } },
        });

        var grid = Assert.IsType<DataGridReportBlock>(HprpBinder.Bind(package, data).Single());
        Assert.Equal("#C0C0FF", grid.Chrome?.HeaderFill);
        Assert.Equal("medium", grid.Chrome?.Border);
        Assert.Equal([2f, 1f], grid.ColumnWeights);
        Assert.Equal("#C0C0FF", HprpChrome.ResolveHeaderFill(grid.Chrome, null, "#DCE6F2"));
        Assert.Equal(1f, HprpChrome.ResolveBorderWidth(grid.Chrome));
    }

    [Fact]
    public void FirstFileHeaderFillFromLayout_SkipsBrandingToken_AndUsesLaterHex()
    {
        var layout = new HprpLayout
        {
            Body =
            [
                new HprpLayoutNode
                {
                    Type = "field-grid",
                    Chrome = new HprpChrome { HeaderFill = HprpChrome.BrandingHeaderFill },
                },
                new HprpLayoutNode
                {
                    Type = "data-grid",
                    When = JsonSerializer.SerializeToElement("$.rows.length > 0"),
                    Chrome = new HprpChrome { HeaderFill = "#FFCC00" },
                },
            ],
        };

        Assert.Equal("#FFCC00", HprpChrome.FirstFileHeaderFillFromLayout(layout));
    }

    [Fact]
    public void Bind_SkipsEmptyFieldGridRows_AndBindsThaiUrHeaderWidget()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "clinical-01-hct-epo",
                DisplayName = "HCT/EPO",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Widget = HprpWidgetIds.ThaiUrHeader,
                        Title = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["$bind"] = "$title" }),
                    },
                    new HprpLayoutNode
                    {
                        Type = "field-grid",
                        Fields =
                        [
                            new HprpFieldNode { Label = JsonSerializer.SerializeToElement(""), Bind = "$.missing" },
                            new HprpFieldNode
                            {
                                Label = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["$label"] = "hn" }),
                                Bind = "$.hn",
                            },
                        ],
                    },
                ],
            },
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["th"] = new Dictionary<string, string> { ["hn"] = "HN" },
            },
        };

        var data = JsonSerializer.SerializeToElement(new { hn = "HN-9" });
        var context = new PdfReportContext
        {
            ReportTemplateId = "clinical-01-hct-epo",
            TenantCode = "local",
            Metadata = new() { Title = "HCT/EPO Report" },
        };

        var blocks = HprpBinder.Bind(package, data, context, "th");

        Assert.Contains(blocks, b => b is TextReportBlock text && text.Content == "HCT/EPO Report" && text.Style == "title");
        var grid = Assert.IsType<FieldGridReportBlock>(blocks.OfType<FieldGridReportBlock>().Single());
        Assert.Single(grid.Fields);
        Assert.Equal("HN-9", grid.Fields[0].Value);
    }

    [Fact]
    public void Validator_RejectsUnknownWidgetAndNewEngine()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = "x",
                DisplayName = "X",
                EngineVersion = 99,
                DataAdapter = "nope",
            },
            Layout = new HprpLayout
            {
                Body = [new HprpLayoutNode { Widget = "unknown.widget" }],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("engineVersion"));
        Assert.Contains(result.Errors, e => e.Contains("dataAdapter"));
        Assert.Contains(result.Errors, e => e.Contains("unknown widget"));
    }

    [Fact]
    public void Validator_RejectsInvalidChromeHeaderFill()
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
                        Type = "text",
                        Bind = "$title",
                        Chrome = new HprpChrome { HeaderFill = "purple" },
                    },
                ],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("headerFill"));
    }

    [Fact]
    public void Validator_RejectsUnknownColumnPlanBind()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.HctEpo,
                DisplayName = "X",
                DataAdapter = HprpDataAdapterIds.Clinical01HctEpo,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable,
                        ColumnPlan = [new HprpColumnPlanItem { Bind = "notAField" }],
                    },
                ],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("notAField"));
    }

    [Fact]
    public void Validator_RejectsColumnPlanOnCopay()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.HctEpo,
                DisplayName = "X",
                DataAdapter = HprpDataAdapterIds.Clinical01HctEpo,
            },
            Layout = new HprpLayout
            {
                Body =
                [
                    new HprpLayoutNode
                    {
                        Widget = HprpWidgetIds.ClinicalHctEpoCopay,
                        ColumnPlan = [new HprpColumnPlanItem { Bind = "hb" }],
                    },
                ],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("columnPlan"));
    }

    [Fact]
    public void BindGeneric_KeyValueResolvesLabelKeyAndLiteralContent()
    {
        var node = new HprpLayoutNode
        {
            Type = "key-value-table",
            Rows =
            [
                new HprpRowNode
                {
                    Label = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["$label"] = "extraNote" }),
                    Content = JsonSerializer.SerializeToElement("sample"),
                },
            ],
        };
        var labels = new Dictionary<string, string> { ["extraNote"] = "หมายเหตุเพิ่ม" };

        var block = HprpBinder.BindGeneric(node, data: null, labels, context: null);
        var table = Assert.IsType<KeyValueTableReportBlock>(block);
        var row = Assert.Single(table.Rows);
        Assert.Equal("หมายเหตุเพิ่ม", row.Label);
        Assert.Equal("sample", row.Value);
    }

    [Fact]
    public void BindGeneric_WidgetOnlyNode_ReturnsNull()
    {
        var node = new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoCopay };
        var block = HprpBinder.BindGeneric(
            node,
            data: null,
            labels: new Dictionary<string, string>(),
            context: null);
        Assert.Null(block);
    }
}

public class HprpPackageAndStoreTests
{
    [Fact]
    public void ReadDirectory_DefaultLabTemplate_IsValid()
    {
        var dir = HprpTestAssets.PackageDir(ClinicalReportCatalog.Lab);
        Assert.True(Directory.Exists(dir), dir);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.Equal(ClinicalReportCatalog.Lab, package.Manifest.Id);
        Assert.True(HprpValidator.Validate(package).IsValid);
        Assert.NotEmpty(package.Layout.Body);
    }

    [Fact]
    public void Serialize_LabPackage_DefaultStjThrows_HprpJsonSucceeds()
    {
        var package = HprpPackageReader.ReadDirectory(HprpTestAssets.PackageDir(ClinicalReportCatalog.Lab));
        var dto = HprpStudioPackageDto.FromPackage(package);

        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(dto));

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        HprpJson.ApplyTo(options);
        var json = JsonSerializer.Serialize(dto, options);
        Assert.Contains("data-grid", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("labResults", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadDirectory_Hemosheet_HasSectionPlan()
    {
        var dir = HprpTestAssets.PackageDir(ClinicalReportCatalog.HemodialysisRecord);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.NotEmpty(package.Layout.Sections);
        Assert.Contains(package.Layout.Sections, s => s.Widget == HprpWidgetIds.HemosheetPatient);
        Assert.Contains(package.Layout.Sections, s => s.Widget == HprpWidgetIds.HemosheetDialysisRecords);
    }

    [Fact]
    public async Task ZipRoundTrip_PreservesManifest()
    {
        var dir = HprpTestAssets.PackageDir(ClinicalReportCatalog.Prescription);
        var original = HprpPackageReader.ReadDirectory(dir);
        using var stream = new MemoryStream();
        await HprpPackageReader.WriteZipAsync(original, stream, CancellationToken.None);
        stream.Position = 0;
        var restored = HprpPackageReader.ReadZip(stream);
        Assert.Equal(original.Manifest.Id, restored.Manifest.Id);
        Assert.Equal(original.Manifest.RequiresSignature, restored.Manifest.RequiresSignature);
    }
}

using System.Text.Json;
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
}

public class HprpPackageAndStoreTests
{
    [Fact]
    public void ReadDirectory_DefaultLabTemplate_IsValid()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "templates", ClinicalReportCatalog.Lab);
        Assert.True(Directory.Exists(dir), dir);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.Equal(ClinicalReportCatalog.Lab, package.Manifest.Id);
        Assert.True(HprpValidator.Validate(package).IsValid);
        Assert.NotEmpty(package.Layout.Body);
    }

    [Fact]
    public void ReadDirectory_Hemosheet_HasSectionPlan()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "templates", ClinicalReportCatalog.HemodialysisRecord);
        var package = HprpPackageReader.ReadDirectory(dir);
        Assert.NotEmpty(package.Layout.Sections);
        Assert.Contains(package.Layout.Sections, s => s.Widget == HprpWidgetIds.HemosheetDialysisRecords);
    }

    [Fact]
    public async Task ZipRoundTrip_PreservesManifest()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "assets", "templates", ClinicalReportCatalog.Prescription);
        var original = HprpPackageReader.ReadDirectory(dir);
        using var stream = new MemoryStream();
        await HprpPackageReader.WriteZipAsync(original, stream, CancellationToken.None);
        stream.Position = 0;
        var restored = HprpPackageReader.ReadZip(stream);
        Assert.Equal(original.Manifest.Id, restored.Manifest.Id);
        Assert.Equal(original.Manifest.RequiresSignature, restored.Manifest.RequiresSignature);
    }
}

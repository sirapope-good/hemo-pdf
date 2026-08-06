using System.Text.Json;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Tests;

public class ReportDocumentSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [Fact]
    public void ReportDocument_RoundTrips_AllBlockTypes()
    {
        var document = new ReportDocument
        {
            Meta = new ReportDocumentMeta
            {
                TemplateId = "clinical-07-lab",
                Title = "ผล Lab",
                PageSize = "A4",
                GeneratedAt = "2026-07-06T10:00:00Z",
            },
            Branding = new ReportBranding
            {
                LogoUrl = "data:image/png;base64,abc",
                CompanyLines = ["โรงพยาบาลทดสอบ"],
                Alignment = "center",
            },
            Header = new ReportHeaderBlock
            {
                Title = "ผล Lab",
                ReportCode = "LAB-001",
            },
            Pages =
            [
                new ReportPage
                {
                    Blocks =
                    [
                        new PatientInfoReportBlock
                        {
                            Title = "ข้อมูลผู้ป่วย",
                            Columns =
                            [
                                [new LabelValue { Label = "ชื่อ", Value = "สมชาย" }],
                            ],
                        },
                        new KeyValueTableReportBlock
                        {
                            Title = "ผลตรวจ",
                            Rows = [new LabelValue { Label = "Hb", Value = "12.5" }],
                        },
                        new DataGridReportBlock
                        {
                            Title = "ตาราง",
                            Columns = ["A", "B"],
                            Rows = [["1", "2"]],
                        },
                        new SignatureReportBlock
                        {
                            Slots = [new SignatureSlot { Role = "แพทย์", Name = "Dr. A" }],
                        },
                        new TextReportBlock { Content = "หมายเหตุ", Style = "caption" },
                    ],
                },
            ],
            Footer = new ReportFooterBlock
            {
                Type = "page-number",
                PageNumber = new PageNumberInfo { Current = 1, Total = 1 },
            },
        };

        var json = JsonSerializer.Serialize(document, JsonOptions);
        var restored = JsonSerializer.Deserialize<ReportDocument>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal("clinical-07-lab", restored.Meta.TemplateId);
        Assert.Single(restored.Pages);
        Assert.Equal(5, restored.Pages[0].Blocks.Count);
        Assert.IsType<PatientInfoReportBlock>(restored.Pages[0].Blocks[0]);
        Assert.IsType<KeyValueTableReportBlock>(restored.Pages[0].Blocks[1]);
        Assert.IsType<DataGridReportBlock>(restored.Pages[0].Blocks[2]);
        Assert.IsType<SignatureReportBlock>(restored.Pages[0].Blocks[3]);
        Assert.IsType<TextReportBlock>(restored.Pages[0].Blocks[4]);
        Assert.Contains("\"type\":\"patient-info\"", json);
    }
}

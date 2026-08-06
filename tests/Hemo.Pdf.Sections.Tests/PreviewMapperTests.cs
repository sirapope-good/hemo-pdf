using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Sections.Tests;

public class PreviewMapperTests
{
    private static PdfReportContext CreateContext() =>
        new()
        {
            ReportTemplateId = ClinicalReportCatalog.Lab,
            TenantCode = "tenant-demo-a",
            Branding = new CustomerBrandingProfile
            {
                CustomerId = "tenant-demo-a",
                DisplayName = "Demo Hospital",
                Header = new HeaderBranding
                {
                    CompanyLines = ["Hospital A", "Bangkok"],
                    TitleAlignment = HeaderAlignment.Center,
                    LogoUrl = "data:image/png;base64,logo",
                },
                Footer = new FooterBranding { DisclaimerText = "Confidential" },
            },
            Metadata = new ReportMetadata { Title = "Lab Result", ReportCode = "LAB-1" },
        };

    [Fact]
    public void KeyValueTablePreviewMapper_MapsSimpleReportViewModel()
    {
        var viewModel = new SimpleReportViewModel
        {
            Title = "Results",
            Rows = [new KeyValuePair<string, string?>("Hb", "12.5")],
        };

        var block = KeyValueTablePreviewMapper.Map(viewModel);

        Assert.NotNull(block);
        Assert.Equal("Results", block!.Title);
        Assert.Single(block.Rows);
        Assert.Equal("Hb", block.Rows[0].Label);
        Assert.Equal("12.5", block.Rows[0].Value);
    }

    [Fact]
    public void PatientInfoPreviewMapper_MapsPatientFields()
    {
        var viewModel = new TestPatientSource
        {
            PatientInfo = new PatientInfoModel
            {
                Name = "สมชาย",
                HospitalNumber = "HN-001",
            },
        };

        var block = PatientInfoPreviewMapper.Map(viewModel);

        Assert.NotNull(block);
        Assert.Equal(2, block!.Columns.Count);
        Assert.Contains(block.Columns[0], x => x.Label == "ชื่อ-สกุล" && x.Value == "สมชาย");
    }

    [Fact]
    public void HeaderAndBrandingPreviewMapper_UseContextMetadata()
    {
        var context = CreateContext();

        var branding = BrandingPreviewMapper.Map(context);
        var header = HeaderPreviewMapper.Map(context);

        Assert.Equal("data:image/png;base64,logo", branding.LogoUrl);
        Assert.Equal(2, branding.CompanyLines.Count);
        Assert.Equal("center", branding.Alignment);
        Assert.Equal("Lab Result", header.Title);
        Assert.Equal("LAB-1", header.ReportCode);
    }

    [Fact]
    public void FooterPreviewMapper_IncludesDisclaimerAndPageNumber()
    {
        var context = CreateContext();
        var footer = FooterPreviewMapper.Map(context);

        Assert.Equal("configurable", footer.Type);
        Assert.Single(footer.Lines);
        Assert.NotNull(footer.PageNumber);
    }

    private sealed class TestPatientSource : IPatientInfoSource
    {
        public required PatientInfoModel PatientInfo { get; init; }
    }
}

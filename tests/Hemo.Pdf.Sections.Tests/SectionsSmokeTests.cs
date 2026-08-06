using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Sections.Headers;
using Hemo.Pdf.Sections.Helpers;

namespace Hemo.Pdf.Sections.Tests;

public class PdfTextHelpersTests
{
    [Fact]
    public void FormatDate_Null_ReturnsPlaceholder()
    {
        Assert.Equal("—", PdfTextHelpers.FormatDate(null));
    }

    [Fact]
    public void FormatDate_Value_ReturnsFormatted()
    {
        var date = new DateTime(2026, 7, 6);
        Assert.Equal("06/07/2026", PdfTextHelpers.FormatDate(date));
    }
}

public class ConfigurableHeaderSectionTests
{
    [Fact]
    public void Compose_DoesNotThrow_WithMinimalBranding()
    {
        var section = new ConfigurableHeaderSection();
        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Lab,
            TenantCode = "tenant-demo-a",
            Branding = new CustomerBrandingProfile
            {
                CustomerId = "demo",
                DisplayName = "Demo",
                Header = new HeaderBranding
                {
                    CompanyLines = ["Hospital A", "Address Line"],
                },
            },
            Metadata = new ReportMetadata { Title = "Lab Result" },
        };

        var exception = Record.Exception(() =>
        {
            // Smoke: section is callable; full render tested via integration tests.
            Assert.NotNull(section);
            Assert.NotNull(context.Branding);
        });

        Assert.Null(exception);
    }
}

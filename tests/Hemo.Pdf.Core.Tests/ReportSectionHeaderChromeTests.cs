using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Tests;

public sealed class ReportSectionHeaderChromeTests
{
    [Theory]
    [InlineData("#c0c0ff", "#C0C0FF")]
    [InlineData("#AABBCC", "#AABBCC")]
    [InlineData("#AABBCCDD", "#AABBCCDD")]
    public void Normalize_AcceptsHexColors(string input, string expected)
    {
        Assert.Equal(expected, ReportSectionHeaderChrome.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("purple")]
    [InlineData("#GGG")]
    [InlineData("#12345")]
    public void Normalize_RejectsInvalid(string? input)
    {
        Assert.Null(ReportSectionHeaderChrome.Normalize(input));
    }

    [Fact]
    public void Resolve_UsesAmbientOverrideThenFallback()
    {
        Assert.Equal("#DCE6F2", ReportSectionHeaderChrome.Resolve("#DCE6F2"));

        using (ReportSectionHeaderChrome.Begin("#C0C0FF"))
        {
            Assert.Equal("#C0C0FF", ReportSectionHeaderChrome.Resolve("#DCE6F2"));
        }

        Assert.Equal("#DCE6F2", ReportSectionHeaderChrome.Resolve("#DCE6F2"));
    }

    [Fact]
    public void Resolve_PrefersContextBrandingOverAmbient()
    {
        var context = new PdfReportContext
        {
            ReportTemplateId = "clinical-01",
            TenantCode = "local",
            Branding = new CustomerBrandingProfile
            {
                Style = new BrandingStyle { SectionHeaderBackground = "#FFCC00" },
            },
        };

        using (ReportSectionHeaderChrome.Begin("#C0C0FF"))
        {
            Assert.Equal(
                "#FFCC00",
                ReportSectionHeaderChrome.Resolve(context, "#DCE6F2"));
        }
    }
}

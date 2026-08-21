using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Hprp;
using Hemo.Pdf.Sections.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HprpDialysisHeadersTests
{
    [Fact]
    public void TryDialysisHeaders_ThaiUrTemplate_MatchesDenseGrid()
    {
        var package = HprpPackageReader.ReadDirectory(
            HprpTestAssets.PackageDir(ClinicalReportCatalog.HemodialysisRecord, "thaiur"));
        var vm = new HemosheetReportViewModel();

        var headers = HprpHemosheetPlanInterpreter.TryDialysisHeaders(package, vm);

        Assert.NotNull(headers);
        Assert.Equal(HemosheetDialysisColumns.HeaderColumnCount(false), headers.Count);
        Assert.Equal("เวลา", headers[0]);
        Assert.Equal("หมายเหตุ", headers[^1]);
    }

    [Fact]
    public void TryDialysisHeaders_DefaultTemplate_UsesEnglishLabels()
    {
        var package = HprpPackageReader.ReadDirectory(
            HprpTestAssets.PackageDir(ClinicalReportCatalog.HemodialysisRecord, "default"));
        var vm = new HemosheetReportViewModel();

        var headers = HprpHemosheetPlanInterpreter.TryDialysisHeaders(package, vm);

        Assert.NotNull(headers);
        Assert.Equal("Time", headers[0]);
        Assert.Equal("Note", headers[^1]);
    }

    [Fact]
    public void TryDialysisHeaders_HdfFeature_UsesColumnsWhen()
    {
        var package = HprpPackageReader.ReadDirectory(
            HprpTestAssets.PackageDir(ClinicalReportCatalog.HemodialysisRecord, "thaiur"));
        var vm = new HemosheetReportViewModel
        {
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool> { ["showHdfColumns"] = true },
            },
        };

        var headers = HprpHemosheetPlanInterpreter.TryDialysisHeaders(package, vm);

        Assert.NotNull(headers);
        Assert.Equal(HemosheetDialysisColumns.HeaderColumnCount(true), headers.Count);
        Assert.Contains("Substitute total", headers);
    }

    [Fact]
    public void LabTemplate_HasDataGridChrome()
    {
        var package = HprpPackageReader.ReadDirectory(HprpTestAssets.PackageDir(ClinicalReportCatalog.Lab));
        var grid = package.Layout.Body.Single(n => n.Type == "data-grid");
        Assert.Equal(HprpChrome.BrandingHeaderFill, grid.Chrome?.HeaderFill);
        Assert.Equal("thin", grid.Chrome?.Border);
        Assert.True(HprpValidator.Validate(package).IsValid);
    }
}

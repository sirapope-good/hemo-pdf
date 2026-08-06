using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetDialysisColumnsTests
{
    [Fact]
    public void ShowHdf_True_WhenFeatureFlagSet()
    {
        var vm = new HemosheetReportViewModel
        {
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = "HD" },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool> { ["showHdfColumns"] = true },
            },
        };

        Assert.True(HemosheetDialysisColumns.ShowHdf(vm));
    }

    [Fact]
    public void ShowHdf_True_WhenPrescriptionModeHdf()
    {
        var vm = new HemosheetReportViewModel
        {
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = "HDF" },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool>(),
            },
        };

        Assert.True(HemosheetDialysisColumns.ShowHdf(vm));
    }

    [Fact]
    public void CellValues_InsertsSubstituteTotalAndRate_BetweenVpAndTmp()
    {
        var cells = HemosheetDialysisColumns.CellValues(
            new HemosheetDialysisRecordViewModel
            {
                Timestamp = new DateTime(2026, 8, 6, 10, 35, 0, DateTimeKind.Utc),
                Bps = 142,
                Bpd = 75,
                Hr = 80,
                Bfr = 300,
                Ap = -180,
                Vp = 120,
                HdfVolume = 4,
                HdfRate = 11.5f,
                Tmp = 80,
                Dc = 14,
                UfRate = 0.5f,
                UfTotal = 0.2f,
            },
            showHdf: true);

        Assert.Equal(HemosheetDialysisColumns.DataColumnCount(true), cells.Length);
        // After VP (index 6): total, rate, then TMP
        Assert.Equal("4", cells[7]);
        Assert.Equal("11.5", cells[8]);
        Assert.False(string.IsNullOrEmpty(cells[9])); // TMP
    }

    [Fact]
    public void FluidBoxes_WidenExtraFluid_WhenHdf()
    {
        var vm = new HemosheetReportViewModel();
        var hd = HemosheetDialysisColumns.FluidBoxes(vm, showHdf: false);
        var hdf = HemosheetDialysisColumns.FluidBoxes(vm, showHdf: true);

        Assert.Equal(12, hd.Sum(b => b.Span));
        Assert.Equal(14, hdf.Sum(b => b.Span));
        Assert.Equal(5, hdf.Single(b => b.Label == "Extra-fluid").Span);
    }
}

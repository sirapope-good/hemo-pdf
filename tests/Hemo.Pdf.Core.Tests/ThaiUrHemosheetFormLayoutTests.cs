using System.Reflection;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

namespace Hemo.Pdf.Core.Tests;

public class ThaiUrHemosheetFormLayoutTests
{
    [Fact]
    public void BudgetDialysisRows_ClampsDesiredRowsToAvailablePageSpace()
    {
        var vm = new HemosheetReportViewModel
        {
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel { Dialysis = 8 },
                },
            },
            DialysisRecords =
            [
                new HemosheetDialysisRecordViewModel(),
                new HemosheetDialysisRecordViewModel(),
                new HemosheetDialysisRecordViewModel(),
            ],
        };

        var rows = InvokeBudgetDialysisRows(vm, aboveDialysisMm: 150f, bottomFloorMm: 100f);

        Assert.InRange(rows, 1, 6);
        Assert.True(rows < 8, "Should not force fixedLines when they exceed page budget.");
    }

    [Fact]
    public void BudgetDialysisRows_PaintsAllRecordsWhenTheyExceedBudget()
    {
        var vm = new HemosheetReportViewModel
        {
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel { Dialysis = 4 },
                },
            },
            DialysisRecords = Enumerable.Range(0, 12)
                .Select(_ => new HemosheetDialysisRecordViewModel())
                .ToList(),
        };

        var rows = InvokeBudgetDialysisRows(vm, aboveDialysisMm: 150f, bottomFloorMm: 100f);

        Assert.Equal(12, rows);
    }

    private static int InvokeBudgetDialysisRows(
        HemosheetReportViewModel vm,
        float aboveDialysisMm,
        float bottomFloorMm)
    {
        var method = typeof(ThaiUrHemosheetForm).GetMethod(
            "BudgetDialysisRows",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method.Invoke(null, [vm, aboveDialysisMm, bottomFloorMm]);
        return Assert.IsType<int>(result);
    }
}

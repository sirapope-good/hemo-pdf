using Hemo.Pdf.Core.Formatting;
using Hemo.Pdf.Core.Models.Clinical;
using Xunit;

namespace Hemo.Pdf.Core.Tests;

public class MedRxFrequencyFormatterTests
{
    [Fact]
    public void Format_BwUnlimited_MatchesFeConvention()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.BW,
            dosePerTarget: 2,
            targetLoopAmount: 1,
            limitDose: 0);

        Assert.Equal("2 dose within 1 week", text);
    }

    [Fact]
    public void Format_BmUnlimited()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.BM,
            dosePerTarget: 1,
            targetLoopAmount: 1,
            limitDose: 0);

        Assert.Equal("1 dose within 1 month", text);
    }

    [Fact]
    public void Format_BwWithLimit()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.BW,
            dosePerTarget: 2,
            targetLoopAmount: 1,
            limitDose: 12);

        Assert.Equal("2 dose over 6 week(s) · Limit 12", text);
    }

    [Fact]
    public void Format_Prn()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.PRN,
            dosePerTarget: 1,
            targetLoopAmount: 1,
            limitDose: 0);

        Assert.Equal("Use when needed", text);
    }

    [Fact]
    public void Format_Stat()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.ST,
            dosePerTarget: 1,
            targetLoopAmount: 1,
            limitDose: 0);

        Assert.Equal("Use immediately", text);
    }

    [Fact]
    public void Format_BsPluralSessions()
    {
        var text = MedRxFrequencyFormatter.Format(
            MedRxFrequency.BS,
            dosePerTarget: 1,
            targetLoopAmount: 3,
            limitDose: 0);

        Assert.Equal("1 dose within 3 session(s)", text);
    }
}

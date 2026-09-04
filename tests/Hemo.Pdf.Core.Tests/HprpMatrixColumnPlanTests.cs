using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpMatrixColumnPlanTests
{
    [Fact]
    public void Resolve_Default_FixedLabelAndRelativeMonths()
    {
        var plan = HprpMatrixColumnPlan.Resolve(null, monthCount: 12);
        Assert.Equal(13, plan.Count);
        Assert.True(plan[0].ConstantMm);
        Assert.Equal(46f, plan[0].Value);
        Assert.All(plan.Skip(1), c =>
        {
            Assert.False(c.ConstantMm);
            Assert.Equal(1f / 12f, c.Value, 4);
        });
    }

    [Fact]
    public void Resolve_StarStar_SplitsBandEqually()
    {
        var plan = HprpMatrixColumnPlan.Resolve(["*", "*"], 4);
        Assert.Equal(5, plan.Count);
        Assert.False(plan[0].ConstantMm);
        Assert.Equal(1f, plan[0].Value);
        Assert.All(plan.Skip(1), c =>
        {
            Assert.False(c.ConstantMm);
            Assert.Equal(0.25f, c.Value, 4);
        });
    }

    [Fact]
    public void Resolve_Weights_ItemAndBand()
    {
        var plan = HprpMatrixColumnPlan.Resolve(["1.6", "2"], 10);
        Assert.Equal(11, plan.Count);
        Assert.False(plan[0].ConstantMm);
        Assert.Equal(1.6f, plan[0].Value, 3);
        Assert.All(plan.Skip(1), c =>
        {
            Assert.False(c.ConstantMm);
            Assert.Equal(0.2f, c.Value, 4);
        });
    }

    [Fact]
    public void Resolve_BothConstantMm_SplitsBandAcrossMonths()
    {
        var plan = HprpMatrixColumnPlan.Resolve(["40mm", "200mm"], 10);
        Assert.True(plan[0].ConstantMm);
        Assert.Equal(40f, plan[0].Value);
        Assert.All(plan.Skip(1), c =>
        {
            Assert.True(c.ConstantMm);
            Assert.Equal(20f, c.Value, 3);
        });
    }

    [Fact]
    public void Resolve_ZeroMonths_ReturnsItemOnly()
    {
        var plan = HprpMatrixColumnPlan.Resolve(["46mm", "*"], 0);
        Assert.Single(plan);
        Assert.True(plan[0].ConstantMm);
        Assert.Equal(46f, plan[0].Value);
    }

    [Fact]
    public void FormatToken_RoundTripsCommonCases()
    {
        Assert.Equal("46mm", HprpMatrixColumnPlan.FormatToken(true, 46f));
        Assert.Equal("*", HprpMatrixColumnPlan.FormatToken(false, 1f));
        Assert.Equal("1.6", HprpMatrixColumnPlan.FormatToken(false, 1.6f));
    }
}

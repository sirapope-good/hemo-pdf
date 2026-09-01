using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpDataGridColumnPlanTests
{
    [Fact]
    public void Resolve_ExactTokenCount_UsesParseColumnWeights()
    {
        var tokens = new[] { "3", "*", "*", "2", "2", "2", "2" };
        var weights = HprpDataGridColumnPlan.Resolve(tokens, 7);
        Assert.Equal([3f, 1f, 1f, 2f, 2f, 2f, 2f], weights);
    }

    [Fact]
    public void Resolve_LabTemplateTwoTokens_ExpandsToColumnCount()
    {
        var weights = HprpDataGridColumnPlan.Resolve(["3", "*"], 7);
        Assert.Equal(7, weights.Count);
        Assert.Equal(3f, weights[0]);
        Assert.All(weights.Skip(1), w => Assert.Equal(1f, w));
    }

    [Fact]
    public void Resolve_MissingTokens_UsesDefaultLabPattern()
    {
        var weights = HprpDataGridColumnPlan.Resolve(null, 5);
        Assert.Equal([3f, 1f, 1f, 1f, 1f], weights);
    }

    [Fact]
    public void NormalizeTokens_PadsToColumnCount()
    {
        var tokens = HprpDataGridColumnPlan.NormalizeTokens(["3", "*"], 4);
        Assert.Equal(["3", "*", "*", "*"], tokens);
    }
}

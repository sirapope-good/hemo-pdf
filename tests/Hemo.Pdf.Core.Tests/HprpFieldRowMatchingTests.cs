using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Tests;

public class HprpFieldRowMatchingTests
{
    [Theory]
    [InlineData("ชาย", true)]
    [InlineData("M", true)]
    [InlineData("Male", true)]
    [InlineData("หญิง", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSelected_MatchesValueLabelAndAliases(string? bound, bool expected)
    {
        var option = new HprpFieldOption
        {
            Value = "ชาย",
            Label = "ชาย",
            Match = ["M", "Male", "ชาย"],
        };

        Assert.Equal(expected, HprpFieldRowMatching.IsSelected(bound, option));
    }

    [Fact]
    public void IsSelected_BloodTypePrefixAliases()
    {
        var option = new HprpFieldOption
        {
            Value = "O",
            Label = "โอ",
            Match = ["O", "O+", "O-", "โอ"],
        };

        Assert.True(HprpFieldRowMatching.IsSelected("O+", option));
        Assert.False(HprpFieldRowMatching.IsSelected("A+", option));
    }
}

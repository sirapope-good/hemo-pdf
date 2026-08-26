using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Tests;

public class HprpBoxParseTests
{
    [Theory]
    [InlineData("2", 2, 2, 2, 2)]
    public void Parse_Number_Uniform(string json, float t, float r, float b, float l)
    {
        var sides = HprpBox.TryParseSides(JsonSerializer.Deserialize<JsonElement>(json));
        Assert.NotNull(sides);
        Assert.Equal(t, sides!.Top);
        Assert.Equal(r, sides.Right);
        Assert.Equal(b, sides.Bottom);
        Assert.Equal(l, sides.Left);
    }

    [Fact]
    public void Parse_TwoValueArray_VerticalHorizontal()
    {
        var sides = HprpBox.TryParseSides(JsonSerializer.SerializeToElement(new[] { 1f, 4f }));
        Assert.Equal(1, sides!.Top);
        Assert.Equal(1, sides.Bottom);
        Assert.Equal(4, sides.Right);
        Assert.Equal(4, sides.Left);
    }

    [Fact]
    public void Parse_FourValueArray()
    {
        var sides = HprpBox.TryParseSides(JsonSerializer.SerializeToElement(new[] { 1f, 2f, 3f, 4f }));
        Assert.Equal(1, sides!.Top);
        Assert.Equal(2, sides.Right);
        Assert.Equal(3, sides.Bottom);
        Assert.Equal(4, sides.Left);
    }
}

public class HprpBoxBinderTests
{
    [Fact]
    public void Bind_Text_CopiesChromeAndBox()
    {
        var node = new HprpLayoutNode
        {
            Type = "text",
            Content = JsonSerializer.SerializeToElement("Hi"),
            Chrome = new HprpChrome { FontSize = 11 },
            Box = new HprpNodeBox { MarginMm = JsonSerializer.SerializeToElement(2) },
        };

        var block = Assert.IsType<TextReportBlock>(
            HprpBinder.BindGeneric(node, null, new Dictionary<string, string>(), null));
        Assert.Equal(11, block.Chrome?.FontSize);
        Assert.NotNull(block.Box);
    }
}

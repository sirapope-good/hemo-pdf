using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpChromeHeaderTests
{
    [Fact]
    public void ResolveHeaderAlign_Omitted_IsMiddle()
    {
        Assert.Equal(HprpHeaderAlign.Middle, HprpChrome.ResolveHeaderAlign(null));
        Assert.Equal(HprpHeaderAlign.Middle, HprpChrome.ResolveHeaderAlign(new HprpChrome()));
    }

    [Theory]
    [InlineData("top", HprpHeaderAlign.Top)]
    [InlineData("TOP", HprpHeaderAlign.Top)]
    [InlineData("bottom", HprpHeaderAlign.Bottom)]
    [InlineData("middle", HprpHeaderAlign.Middle)]
    [InlineData("nope", HprpHeaderAlign.Middle)]
    public void ResolveHeaderAlign_ParsesKnownValues(string raw, HprpHeaderAlign expected)
    {
        Assert.Equal(expected, HprpChrome.ResolveHeaderAlign(new HprpChrome { HeaderAlign = raw }));
    }

    [Fact]
    public void ResolveHeaderHeightMm_UsesFileOrFallback()
    {
        Assert.Equal(5f, HprpChrome.ResolveHeaderHeightMm(null, 5f));
        Assert.Equal(3.5f, HprpChrome.ResolveHeaderHeightMm(new HprpChrome { HeaderHeightMm = 3.5f }, 5f));
    }

    [Fact]
    public void Validator_RejectsBadHeaderAlign()
    {
        var errors = new List<string>();
        HprpChrome.Validate(new HprpChrome { HeaderAlign = "center" }, "chrome", errors);
        Assert.Contains(errors, e => e.Contains("headerAlign"));
    }

    [Fact]
    public void SoapRecipe_ExposesHeaderAlignFields()
    {
        var recipe = HprpWidgetRecipes.TryGet(HprpWidgetIds.ClinicalSoapTable);
        Assert.NotNull(recipe);
        Assert.Contains("chrome.headerAlign", recipe!.InspectorFields);
        Assert.Contains("chrome.headerHeightMm", recipe.InspectorFields);
        Assert.Contains("chrome.headerPaddingMm", recipe.InspectorFields);
    }
}

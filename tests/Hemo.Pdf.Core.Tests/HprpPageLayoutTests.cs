using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpPageLayoutTests
{
    [Fact]
    public void Resolve_OmittedMargin_KeepsFallback()
    {
        var page = HprpPageLayout.Resolve(new HprpPage { Size = "A4" }, HprpPageFallback.Uniform(2));
        Assert.Equal(2, page.Top);
        Assert.Equal(2, page.Left);
        Assert.Equal(2, page.SpacingMm);
        Assert.Null(page.FontSize);
    }

    [Fact]
    public void Resolve_MarginMm_AppliesAllSides()
    {
        var page = HprpPageLayout.Resolve(new HprpPage { MarginMm = 10 }, HprpPageFallback.Uniform(2));
        Assert.Equal(10, page.Top);
        Assert.Equal(10, page.Right);
        Assert.Equal(10, page.Bottom);
        Assert.Equal(10, page.Left);
        Assert.Equal(20, page.Vertical);
    }

    [Fact]
    public void Resolve_NamedSides_OverrideShorthand()
    {
        var page = HprpPageLayout.Resolve(
            new HprpPage
            {
                MarginMm = 10,
                Margin = new HprpSides { Top = 4, Left = 6 },
                SpacingMm = 3,
                FontSize = 8,
            },
            HprpPageFallback.Uniform(2, 1));

        Assert.Equal(4, page.Top);
        Assert.Equal(10, page.Right);
        Assert.Equal(10, page.Bottom);
        Assert.Equal(6, page.Left);
        Assert.Equal(3, page.SpacingMm);
        Assert.Equal(8, page.FontSize);
    }
}

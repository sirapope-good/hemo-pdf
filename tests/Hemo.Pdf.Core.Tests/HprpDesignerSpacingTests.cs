using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Tests;

public class HprpDesignerSpacingTests
{
    [Fact]
    public void ResolveGaps_None_IsZero()
    {
        var gaps = HprpPageLayout.ResolveDesignerGaps(
            new HprpPage { SpacingMode = HprpSpacingModes.None, SpacingMm = 5 },
            marginLeftMm: 2);
        Assert.Equal(0, gaps.BelowMm);
        Assert.Equal(0, gaps.BesideMm);
    }

    [Fact]
    public void ResolveGaps_Margin_UsesMarginMm()
    {
        var gaps = HprpPageLayout.ResolveDesignerGaps(
            new HprpPage { SpacingMode = HprpSpacingModes.Margin, MarginMm = 4 },
            marginLeftMm: 2);
        Assert.Equal(4, gaps.BelowMm);
        Assert.Equal(4, gaps.BesideMm);
    }

    [Fact]
    public void ResolveGaps_Custom_SplitsBelowAndBeside()
    {
        var gaps = HprpPageLayout.ResolveDesignerGaps(
            new HprpPage
            {
                SpacingMode = HprpSpacingModes.Custom,
                SpacingMm = 2,
                SpacingBelowMm = 0,
                SpacingBesideMm = 3,
            },
            marginLeftMm: 2);
        Assert.Equal(0, gaps.BelowMm);
        Assert.Equal(3, gaps.BesideMm);
    }

    [Fact]
    public void Reflow_None_OverlapsBesideBorders()
    {
        var page = new HprpPage { SpacingMode = HprpSpacingModes.None };
        var els = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "a",
                Type = HprpDesignerElementTypes.BoxText,
                Place = "below",
                ManualWidth = true,
                Box = new HprpDesignerBox { WMm = 50, HMm = 10 },
            },
            new()
            {
                Id = "b",
                Type = HprpDesignerElementTypes.BoxText,
                Place = "beside",
                ManualWidth = true,
                Box = new HprpDesignerBox { WMm = 50, HMm = 10 },
            },
        };

        var flowed = HprpDesignerFlow.Reflow(page, els, contentWidthMm: 206);
        Assert.Equal(0, flowed[0].Box.XMm);
        Assert.Equal(50 - HprpDesignerGaps.BorderCollapseMm, flowed[1].Box.XMm, 3);
        Assert.Equal(0, flowed[1].Box.YMm);
    }

    [Fact]
    public void Resolve_Page_ExposesBesideAndBelow()
    {
        var resolved = HprpPageLayout.Resolve(
            new HprpPage
            {
                MarginMm = 2,
                SpacingMode = HprpSpacingModes.Custom,
                SpacingBelowMm = 1,
                SpacingBesideMm = 5,
            },
            HprpPageFallback.Uniform(2));
        Assert.Equal(1, resolved.SpacingBelowMm);
        Assert.Equal(5, resolved.SpacingBesideMm);
        Assert.Equal(1, resolved.SpacingMm);
    }

    [Fact]
    public void Reflow_TallContent_CreatesSecondPage_WithRepeatingHeader()
    {
        var els = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "hdr",
                Type = HprpDesignerElementTypes.Header,
                Band = HprpDesignerBands.Header,
                Box = new HprpDesignerBox { WMm = 200, HMm = 20 },
            },
            new()
            {
                Id = "a",
                Type = HprpDesignerElementTypes.BoxText,
                Band = HprpDesignerBands.Content,
                Box = new HprpDesignerBox { WMm = 200, HMm = 200 },
            },
            new()
            {
                Id = "b",
                Type = HprpDesignerElementTypes.BoxText,
                Band = HprpDesignerBands.Content,
                Box = new HprpDesignerBox { WMm = 200, HMm = 200 },
            },
        };

        var flow = HprpDesignerFlow.ReflowDetailed(
            new HprpPage { SpacingMm = 2 },
            els,
            contentWidthMm: 206,
            pageHeightMm: 297,
            marginTopMm: 2,
            marginBottomMm: 2);

        Assert.True(flow.PageCount >= 2);
        Assert.Equal(20, flow.HeaderHeightMm);
        Assert.Contains(flow.Pages[0].Elements, e => e.Id == "hdr");
        Assert.Contains(flow.Pages[1].Elements, e => e.Id == "hdr");
        Assert.Contains(flow.Pages[0].Elements, e => e.Id == "a");
        Assert.Contains(flow.Pages[1].Elements, e => e.Id == "b");
    }
}

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

        var flow = HprpDesignerFlow.ReflowDetailed(
            page,
            els,
            contentWidthMm: 206,
            pageHeightMm: 297,
            marginTopMm: 0,
            marginBottomMm: 0,
            marginLeftMm: 0);
        var flowed = flow.Pages[0].Elements;
        Assert.Equal(0, flowed[0].Box.XMm);
        Assert.Equal(50 - HprpDesignerGaps.BorderCollapseMm, flowed[1].Box.XMm, 3);
        Assert.Equal(0, flowed[1].Box.YMm);
    }

    [Fact]
    public void Reflow_Beside_KeepsIndependentHeights()
    {
        var els = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "short",
                Type = HprpDesignerElementTypes.ConfigTable,
                Place = "below",
                ManualWidth = true,
                Box = new HprpDesignerBox { WMm = 80, HMm = 20 },
            },
            new()
            {
                Id = "tall",
                Type = HprpDesignerElementTypes.ConfigTable,
                Place = "beside",
                ManualWidth = true,
                Box = new HprpDesignerBox { WMm = 120, HMm = 40 },
            },
        };

        var flow = HprpDesignerFlow.ReflowDetailed(
            new HprpPage { SpacingMm = 2 },
            els,
            contentWidthMm: 206,
            pageHeightMm: 297,
            marginTopMm: 0,
            marginBottomMm: 0,
            marginLeftMm: 0);
        var flowed = flow.Pages[0].Elements;
        Assert.Equal(20, flowed[0].Box.HMm);
        Assert.Equal(40, flowed[1].Box.HMm);
        Assert.Equal(0, flowed[0].Box.YMm);
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

    [Fact]
    public void Reflow_PageOf_SuperFooter_SitsOutsideMarginGuide()
    {
        var els = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "body",
                Type = HprpDesignerElementTypes.BoxText,
                Band = HprpDesignerBands.Content,
                Box = new HprpDesignerBox { WMm = 200, HMm = 40 },
            },
            new()
            {
                Id = "pg",
                Type = HprpDesignerElementTypes.PageOf,
                Band = HprpDesignerBands.SuperFooter,
                Text = "{current} / {total}",
                Box = new HprpDesignerBox { WMm = 200, HMm = 5 },
            },
        };

        var flow = HprpDesignerFlow.ReflowDetailed(
            new HprpPage { SpacingMm = 0, MarginMm = 2 },
            els,
            contentWidthMm: 206,
            pageHeightMm: 297,
            marginTopMm: 2,
            marginBottomMm: 2,
            marginLeftMm: 2);

        Assert.Equal(5, flow.SuperFooterHeightMm);
        Assert.Equal(Math.Max(2f, 5f), 297 - flow.GuideTopMm - flow.GuideHeightMm, 2);
        var pageOf = Assert.Single(flow.Pages[0].Elements, e => e.Id == "pg");
        Assert.Equal(flow.GuideTopMm + flow.GuideHeightMm, pageOf.Box.YMm, 2);
        Assert.True(pageOf.Box.YMm >= flow.GuideTopMm + flow.GuideHeightMm - 0.01f);
    }
}

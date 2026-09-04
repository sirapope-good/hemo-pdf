using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Tests;

public class HprpDesignerGroupFrameTests
{
    [Fact]
    public void Reflow_EmitsGroupFrameWhenChromeBorderOn()
    {
        var elements = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "frame",
                Type = HprpDesignerElementTypes.Group,
                Band = HprpDesignerBands.Content,
                Place = "below",
                Chrome = new HprpChrome { Border = "thin" },
                Children =
                [
                    new()
                    {
                        Id = "r1",
                        Type = HprpDesignerElementTypes.FieldRow,
                        Band = HprpDesignerBands.Content,
                        Place = "below",
                        Box = new HprpDesignerBox { WMm = 100, HMm = 8 },
                        Chrome = new HprpChrome { Border = "none" },
                        Segments =
                        [
                            new HprpFieldRowSegment
                            {
                                Kind = HprpFieldRowSegmentKinds.Text,
                                Label = "ที่อยู่",
                                Bind = "$.demographics.address",
                            },
                        ],
                    },
                ],
            },
        };

        var flow = HprpDesignerFlow.ReflowDetailed(
            page: null,
            elements,
            contentWidthMm: 194,
            pageHeightMm: 297,
            marginTopMm: 8,
            marginBottomMm: 8,
            marginLeftMm: 8);

        var page0 = Assert.Single(flow.Pages);
        var frameIndex = page0.Elements.ToList().FindIndex(e =>
            string.Equals(e.Type, HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase)
            && e.Id == "frame");
        var childIndex = page0.Elements.ToList().FindIndex(e => e.Id == "r1");
        Assert.True(frameIndex >= 0, "group frame should be emitted");
        Assert.True(childIndex >= 0, "child should be emitted");
        Assert.True(frameIndex > childIndex, "frame must paint after children so border is not covered");
    }

    [Fact]
    public void Reflow_OmitsGroupFrameWhenChromeBorderNone()
    {
        var elements = new List<HprpDesignerElement>
        {
            new()
            {
                Id = "stack",
                Type = HprpDesignerElementTypes.Group,
                Band = HprpDesignerBands.Content,
                Chrome = new HprpChrome { Border = "none" },
                Children =
                [
                    new()
                    {
                        Id = "r1",
                        Type = HprpDesignerElementTypes.BoxText,
                        Text = "x",
                        Box = new HprpDesignerBox { WMm = 100, HMm = 6 },
                    },
                ],
            },
        };

        var flow = HprpDesignerFlow.ReflowDetailed(
            page: null,
            elements,
            contentWidthMm: 194,
            pageHeightMm: 297,
            marginTopMm: 8,
            marginBottomMm: 8,
            marginLeftMm: 8);

        var page0 = Assert.Single(flow.Pages);
        Assert.DoesNotContain(
            page0.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(page0.Elements, e => e.Id == "r1");
    }
}

using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Tests;

public class HprpNarrativeParagraphsTests
{
    [Fact]
    public void Resolve_UsesPackWhenNoBind()
    {
        var el = new HprpDesignerElement
        {
            Id = "n1",
            Type = HprpDesignerElementTypes.Narrative,
            Paragraphs =
            [
                new HprpNarrativeParagraph { Text = "A", Role = "title" },
                new HprpNarrativeParagraph { Text = "B" },
            ],
        };

        var result = HprpNarrativeParagraphs.Resolve(el, null);
        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Text);
    }

    [Fact]
    public void Resolve_MergesPackTitleWithBoundBody()
    {
        var el = new HprpDesignerElement
        {
            Id = "n1",
            Type = HprpDesignerElementTypes.Narrative,
            BindParagraphs = "$.bodyParagraphs",
            Paragraphs =
            [
                new HprpNarrativeParagraph { Text = "Doc title", Role = "title", Align = "center" },
                new HprpNarrativeParagraph { Text = "Pack body ignored" },
            ],
        };

        using var doc = JsonDocument.Parse("""
            { "bodyParagraphs": [ { "text": "Bound 1", "sub": false }, { "text": "Bound 2", "sub": true } ] }
            """);

        var result = HprpNarrativeParagraphs.Resolve(el, doc.RootElement);
        Assert.Equal(3, result.Count);
        Assert.Equal("Doc title", result[0].Text);
        Assert.Equal("title", result[0].Role);
        Assert.Equal("Bound 1", result[1].Text);
        Assert.Equal("Bound 2", result[2].Text);
        Assert.True(result[2].Sub);
    }
}

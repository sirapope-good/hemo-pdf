using System.Text.Json;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Layouts.Header;

namespace Hemo.Pdf.Core.Tests;

public class ConfigurableBoxTextItemsTests
{
    [Fact]
    public void ResolveText_MultiItems_JoinsLabeledValues()
    {
        using var doc = JsonDocument.Parse("""
            {
              "meta": {
                "monthLabel": "ก.พ.",
                "yearBe": 2566,
                "epoName": "Eprex 4000",
                "needlesPerWeek": "2"
              }
            }
            """);

        var el = new HprpDesignerElement
        {
            Type = HprpDesignerElementTypes.BoxText,
            Items =
            [
                new HprpBoxTextItem
                {
                    Label = "เดือน",
                    Bind = "$.meta.monthLabel",
                    Label2 = "พ.ศ.",
                    Bind2 = "$.meta.yearBe",
                    Align = "left",
                },
                new HprpBoxTextItem
                {
                    Label = "ยา EPO",
                    Bind = "$.meta.epoName",
                    Align = "left",
                },
                new HprpBoxTextItem
                {
                    Label = "เข็ม/สัปดาห์",
                    Bind = "$.meta.needlesPerWeek",
                    Align = "right",
                },
            ],
        };

        var text = ConfigurableBoxTextComposer.ResolveText(el, doc.RootElement);
        Assert.Contains("เดือน", text);
        Assert.Contains("ก.พ.", text);
        Assert.Contains("2566", text);
        Assert.Contains("Eprex 4000", text);
        Assert.Contains("เข็ม/สัปดาห์", text);
        Assert.Contains("2", text);
    }

    [Fact]
    public void ResolveText_SingleBind_StillWorks()
    {
        using var doc = JsonDocument.Parse("""{ "coPayCriteria": { "title": "Banner" } }""");
        var el = new HprpDesignerElement
        {
            Type = HprpDesignerElementTypes.BoxText,
            Bind = "$.coPayCriteria.title",
            Text = "fallback",
        };

        Assert.Equal("Banner", ConfigurableBoxTextComposer.ResolveText(el, doc.RootElement));
    }
}

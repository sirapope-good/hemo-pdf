using System.Text.Json;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Core.Tests;

public class HprpDesignerOmitTests
{
    [Fact]
    public void OmitWhenEmpty_SkipsNotes_AndKeepsSinglePage()
    {
        var page = new HprpPage
        {
            Orientation = "landscape",
            MarginMm = 14,
            SpacingMode = HprpSpacingModes.Custom,
            SpacingMm = 2,
            SpacingBelowMm = 2,
        };

        var elements = ChecklistElements(notesHMm: 40);

        var emptyNotes = JsonDocument.Parse("""{"textNotes":[]}""").RootElement.Clone();
        var flowEmpty = HprpDesignerFlow.ReflowDetailed(
            page,
            elements,
            contentWidthMm: 269,
            pageHeightMm: 210,
            marginTopMm: 14,
            marginBottomMm: 14,
            marginLeftMm: 14,
            data: emptyNotes);

        Assert.Equal(1, flowEmpty.PageCount);
        Assert.DoesNotContain(
            flowEmpty.Pages.SelectMany(p => p.Elements),
            e => e.Id == "text-notes");

        var withNotes = JsonDocument.Parse(
            """{"textNotes":[{"monthLabel":"Dec","content":"note"}]}""").RootElement.Clone();
        var flowWith = HprpDesignerFlow.ReflowDetailed(
            page,
            elements,
            contentWidthMm: 269,
            pageHeightMm: 210,
            marginTopMm: 14,
            marginBottomMm: 14,
            marginLeftMm: 14,
            data: withNotes);

        Assert.True(flowWith.PageCount >= 2, $"Expected notes to need a second page, got {flowWith.PageCount}");
        Assert.Contains(
            flowWith.Pages.SelectMany(p => p.Elements),
            e => e.Id == "text-notes");
    }

    [Fact]
    public void ChecklistWidgetFallback_UsesBoundModelWhenNoOmitPath()
    {
        var notes = new HprpDesignerElement
        {
            Id = "text-notes",
            Type = HprpDesignerElementTypes.Dense,
            Widget = HprpWidgetIds.ClinicalChecklistTextNotes,
            Band = HprpDesignerBands.Content,
            Box = new HprpDesignerBox { WMm = 100, HMm = 20 },
        };

        Assert.False(HprpDesignerOmit.ShouldInclude(
            notes,
            data: null,
            boundModel: new Clinical05ProgressNoteChecklistReportViewModel()));

        Assert.True(HprpDesignerOmit.ShouldInclude(
            notes,
            data: null,
            boundModel: new Clinical05ProgressNoteChecklistReportViewModel
            {
                TextNotes =
                [
                    new Clinical05ProgressNoteChecklistTextNote { MonthLabel = "Dec", Content = "x" },
                ],
            }));
    }

    private static List<HprpDesignerElement> ChecklistElements(float notesHMm) =>
    [
        new()
        {
            Id = "hdr",
            Type = HprpDesignerElementTypes.Header,
            Band = HprpDesignerBands.Header,
            Box = new HprpDesignerBox { WMm = 269, HMm = 32.4f },
        },
        new()
        {
            Id = "range",
            Type = HprpDesignerElementTypes.BoxText,
            Band = HprpDesignerBands.Content,
            Box = new HprpDesignerBox { WMm = 269, HMm = 5 },
        },
        new()
        {
            Id = "grid",
            Type = HprpDesignerElementTypes.ConfigTable,
            Band = HprpDesignerBands.Content,
            Box = new HprpDesignerBox { WMm = 269, HMm = 130 },
        },
        new()
        {
            Id = "text-notes",
            Type = HprpDesignerElementTypes.Dense,
            Widget = HprpWidgetIds.ClinicalChecklistTextNotes,
            Band = HprpDesignerBands.Content,
            OmitWhenEmpty = "$.textNotes",
            Box = new HprpDesignerBox { WMm = 269, HMm = notesHMm },
        },
        new()
        {
            Id = "page-of",
            Type = HprpDesignerElementTypes.PageOf,
            Band = HprpDesignerBands.SuperFooter,
            Box = new HprpDesignerBox { WMm = 269, HMm = 5 },
        },
    ];
}

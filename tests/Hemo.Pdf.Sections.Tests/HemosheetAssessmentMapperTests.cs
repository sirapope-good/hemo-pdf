using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Sections.Tests;

public class HemosheetAssessmentMapperTests
{
    [Fact]
    public void MapPreReAssessmentMatrix_JoinsTopics_AndLeavesMissingUnchecked()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre =
                [
                    new() { Name = "pain", Checked = true, Text = "mild" },
                    new() { Name = "chest", Checked = false },
                ],
                Re =
                [
                    new() { Name = "pain", Checked = false },
                ],
            },
        };

        var block = HemosheetPreviewMappers.MapPreReAssessmentMatrix(vm);

        Assert.NotNull(block);
        Assert.Equal(ChecklistTablePreviewMapper.LayoutPreReMatrix, block!.Layout);
        Assert.Equal(2, block.Rows.Count);

        var pain = block.Rows[1]; // ordered: chest, pain
        var chest = block.Rows[0];
        Assert.Equal("chest", ((ChecklistTextCell)chest[0]).Text);
        Assert.False(((ChecklistCheckboxCell)chest[1]).Checked); // Pre Y
        Assert.True(((ChecklistCheckboxCell)chest[2]).Checked);  // Pre N
        Assert.False(((ChecklistCheckboxCell)chest[3]).Checked); // Re Y missing
        Assert.False(((ChecklistCheckboxCell)chest[4]).Checked); // Re N missing

        Assert.Equal("pain", ((ChecklistTextCell)pain[0]).Text);
        Assert.True(((ChecklistCheckboxCell)pain[1]).Checked);
        Assert.False(((ChecklistCheckboxCell)pain[2]).Checked);
        Assert.False(((ChecklistCheckboxCell)pain[3]).Checked);
        Assert.True(((ChecklistCheckboxCell)pain[4]).Checked);
        Assert.Equal("mild", ((ChecklistTextCell)pain[5]).Text);
    }

    [Fact]
    public void MapAssessment_ExpandsSelectedOptions_AsCheckedRows()
    {
        var block = HemosheetPreviewMappers.MapAssessment("Assessment (Post)",
        [
            new HemosheetAssessmentItemViewModel
            {
                Name = "complication",
                Checked = true,
                Text = "mild",
                SelectedOptions = ["Hypo-tension", "Muscle cramp"],
            },
        ]);

        Assert.NotNull(block);
        Assert.Equal(2, block!.Rows.Count);
        Assert.Equal("Hypo-tension", ((ChecklistTextCell)block.Rows[0][1]).Text);
        Assert.True(((ChecklistCheckboxCell)block.Rows[0][0]).Checked);
        Assert.Equal("mild", ((ChecklistTextCell)block.Rows[0][2]).Text);
        Assert.Equal("Muscle cramp", ((ChecklistTextCell)block.Rows[1][1]).Text);
    }

    [Fact]
    public void MapFooterChecklists_SupportsParentSelectedOptions_AndDottedNames()
    {
        var beShape = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Post =
                [
                    new()
                    {
                        Name = "complication",
                        Checked = true,
                        SelectedOptions = ["Hypo-tension"],
                    },
                ],
                Other =
                [
                    new()
                    {
                        Name = "medication",
                        Checked = true,
                        SelectedOptions = ["EPO"],
                    },
                ],
            },
        };

        var cluster = HemosheetPreviewMappers.MapFooterChecklists(beShape);
        Assert.NotNull(cluster);
        Assert.Contains(cluster!.Tables, t => t.Title == "Complication");
        Assert.Contains(cluster.Tables, t => t.Title == "Medication duration HD");

        var dotted = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Post =
                [
                    new() { Name = "complication.cramps", Checked = true, Text = "mild" },
                ],
            },
        };

        var dottedCluster = HemosheetPreviewMappers.MapFooterChecklists(dotted);
        Assert.NotNull(dottedCluster);
        var complication = Assert.Single(dottedCluster!.Tables, t => t.Title == "Complication");
        Assert.Equal("cramps", ((ChecklistTextCell)complication.Rows[0][1]).Text);
    }

    [Fact]
    public void MapTopLayoutRow_IncludesPreYnOnlyForThaiUr()
    {
        var defaultVm = BaseVm(HemosheetLayoutProfile.Default);
        var defaultRow = HemosheetPreviewMappers.MapTopLayoutRow(defaultVm, defaultVm.LayoutContext.Features);
        Assert.NotNull(defaultRow);
        Assert.DoesNotContain(
            Flatten(defaultRow!),
            b => b is ChecklistTableReportBlock c && c.Title == "อาการก่อนฟอก");

        var thaiUrVm = BaseVm(HemosheetLayoutProfile.ThaiUr);
        var thaiUrRow = HemosheetPreviewMappers.MapTopLayoutRow(thaiUrVm, thaiUrVm.LayoutContext.Features);
        Assert.Contains(
            Flatten(thaiUrRow!),
            b => b is ChecklistTableReportBlock c && c.Layout == ChecklistTablePreviewMapper.LayoutYnColumns);
    }

    private static HemosheetReportViewModel BaseVm(HemosheetLayoutProfile profile) =>
        new()
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new() { Name = "pain", Checked = true }],
            },
            Dehydration = new HemosheetDehydrationViewModel { PreWeight = 60 },
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = "HD" },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = profile,
                Features = new Dictionary<string, bool>(),
            },
        };

    private static IEnumerable<ReportBlock> Flatten(ReportBlock block) =>
        block switch
        {
            SectionRowReportBlock row => row.Blocks.SelectMany(Flatten),
            ColumnStackReportBlock stack => stack.Blocks.SelectMany(Flatten),
            _ => [block],
        };
}

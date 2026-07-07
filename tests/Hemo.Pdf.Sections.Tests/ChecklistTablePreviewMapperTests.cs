using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Sections.Tests;

public class ChecklistTablePreviewMapperTests
{
    [Fact]
    public void Map_ThreeColumns_WithTitle()
    {
        var block = ChecklistTablePreviewMapper.Map(new ChecklistTableModel
        {
            Title = "Assessment (Pre)",
            Items =
            [
                new ChecklistItem { Label = "pain", IsChecked = true, Notes = "mild" },
                new ChecklistItem { Label = "chest", IsChecked = false },
            ],
        });

        Assert.NotNull(block);
        Assert.Equal("Assessment (Pre)", block!.Title);
        Assert.Equal(["", "รายการ", "หมายเหตุ"], block.Columns);
        Assert.Equal(2, block.Rows.Count);
        Assert.IsType<ChecklistCheckboxCell>(block.Rows[0][0]);
        Assert.Equal("pain", ((ChecklistTextCell)block.Rows[0][1]).Text);
        Assert.Equal("mild", ((ChecklistTextCell)block.Rows[0][2]).Text);
        Assert.Equal("—", ((ChecklistTextCell)block.Rows[1][2]).Text);
    }

    [Fact]
    public void Map_EmptyItems_ReturnsNull()
    {
        var block = ChecklistTablePreviewMapper.Map(new ChecklistTableModel
        {
            Title = "Assessment (Pre)",
            Items = [],
        });

        Assert.Null(block);
    }
}

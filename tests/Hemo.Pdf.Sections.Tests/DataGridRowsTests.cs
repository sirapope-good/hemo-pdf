using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Tests;

public class DataGridRowsTests
{
    [Fact]
    public void IsSectionBand_WhenOnlyFirstCellHasText()
    {
        Assert.True(DataGridRows.IsSectionBand(["1 Month", "", ""]));
        Assert.False(DataGridRows.IsSectionBand(["Hb", "11.2", ""]));
        Assert.False(DataGridRows.IsSectionBand(["", "", ""]));
        Assert.False(DataGridRows.IsSectionBand(["1 Month"]));
    }

    [Fact]
    public void DisplayCell_UsesDashOnlyForNull()
    {
        Assert.Equal("—", DataGridRows.DisplayCell(null));
        Assert.Equal("", DataGridRows.DisplayCell(""));
        Assert.Equal("11.2", DataGridRows.DisplayCell("11.2"));
    }
}

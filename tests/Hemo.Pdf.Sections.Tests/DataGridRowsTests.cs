using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Sections.Tests;

public class DataGridRowsTests
{
    [Fact]
    public void IsSectionBand_RecognizesFrequencyTitlesOnly()
    {
        Assert.True(DataGridRows.IsSectionBand(["1 Month", "", ""]));
        Assert.True(DataGridRows.IsSectionBand(["3 Month", "", "", "", "", "", "", ""]));
        Assert.True(DataGridRows.IsSectionBand(["1 year", "", ""]));
        Assert.True(DataGridRows.IsSectionBand(["Other", "", ""]));
    }

    [Fact]
    public void IsSectionBand_RejectsLabNameWithEmptyDates()
    {
        Assert.False(DataGridRows.IsSectionBand(["PMN", "", "", "", "", "", "", ""]));
        Assert.False(DataGridRows.IsSectionBand(["Hb / Hct", "9.5", "", ""]));
        Assert.False(DataGridRows.IsSectionBand(["Hb", "11.2", ""]));
    }

    [Fact]
    public void IsSectionBand_RejectsEmptyOrPartialRows()
    {
        Assert.False(DataGridRows.IsSectionBand(["", "", ""]));
        Assert.False(DataGridRows.IsSectionBand(["1 Month"]));
        Assert.False(DataGridRows.IsSectionBand(["1 Month", "x", ""]));
    }

    [Fact]
    public void DisplayCell_UsesDashOnlyForNull()
    {
        Assert.Equal("—", DataGridRows.DisplayCell(null));
        Assert.Equal("", DataGridRows.DisplayCell(""));
        Assert.Equal("11.2", DataGridRows.DisplayCell("11.2"));
    }
}

using Hemo.Pdf.Sections.Helpers;

namespace Hemo.Pdf.Sections.Tests;

public class PdfCheckboxTests
{
    [Fact]
    public void GetSvg_Checked_ContainsRoundedRectAndMarkPath()
    {
        var svg = PdfCheckbox.GetSvg(true);

        Assert.Contains("rx=\"1.5\"", svg);
        Assert.Contains("stroke=\"#66B4E7\"", svg);
        Assert.Contains("M3 8.34375L5.4 12C6.6 9.09375 8.6 5.8125 12 3", svg);
        Assert.Contains("stroke=\"#367EB5\"", svg);
    }

    [Fact]
    public void GetSvg_Unchecked_HasRectOnly()
    {
        var svg = PdfCheckbox.GetSvg(false);

        Assert.Contains("rx=\"1.5\"", svg);
        Assert.Contains("stroke=\"#A3ADB4\"", svg);
        Assert.DoesNotContain("M3 8.34375", svg);
    }

    [Fact]
    public void SvgConstants_MatchAssetFiles()
    {
        var root = FindRepoRoot();
        var checkedAsset = File.ReadAllText(Path.Combine(root, "assets", "icons", "checkbox-checked.svg")).Trim();
        var uncheckedAsset = File.ReadAllText(Path.Combine(root, "assets", "icons", "checkbox-unchecked.svg")).Trim();

        Assert.Equal(Normalize(checkedAsset), Normalize(PdfCheckbox.CheckedSvg));
        Assert.Equal(Normalize(uncheckedAsset), Normalize(PdfCheckbox.UncheckedSvg));
    }

    private static string Normalize(string svg) =>
        string.Join('\n', svg.Replace("\r\n", "\n").Trim().Split('\n').Select(l => l.TrimEnd()));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "assets", "icons", "checkbox-checked.svg")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate hemo-pdf repo root with assets/icons.");
    }
}

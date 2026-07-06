namespace Hemo.Pdf.Core.Models;

public sealed class SimpleReportViewModel
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public IReadOnlyList<KeyValuePair<string, string?>> Rows { get; init; } = [];
}

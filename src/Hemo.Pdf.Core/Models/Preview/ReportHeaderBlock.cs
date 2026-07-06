namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportHeaderBlock
{
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public string? ReportCode { get; init; }
    public IReadOnlyList<string> MetadataLines { get; init; } = [];
}

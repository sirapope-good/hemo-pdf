namespace Hemo.Pdf.Core.Models;

public sealed class ReportMetadata
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public string? ReportCode { get; init; }
}

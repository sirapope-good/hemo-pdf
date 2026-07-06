namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportDocument
{
    public ReportDocumentMeta Meta { get; init; } = new();
    public ReportBranding Branding { get; init; } = new();
    public ReportHeaderBlock Header { get; init; } = new();
    public IReadOnlyList<ReportPage> Pages { get; init; } = [];
    public ReportFooterBlock Footer { get; init; } = new();
}

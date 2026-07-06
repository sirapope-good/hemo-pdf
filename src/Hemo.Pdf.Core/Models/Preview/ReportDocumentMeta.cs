namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportDocumentMeta
{
    public string TemplateId { get; init; } = "";
    public string Title { get; init; } = "";
    public string PageSize { get; init; } = "A4";
    public string? GeneratedAt { get; init; }
}

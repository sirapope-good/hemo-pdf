namespace Hemo.Pdf.Core.Models.Preview;

public sealed class ReportDocumentMeta
{
    public string TemplateId { get; init; } = "";
    public string Title { get; init; } = "";
    public string PageSize { get; init; } = "A4";
    public string? GeneratedAt { get; init; }

    /// <summary>
    /// Hint for FE: "dom" renders ReportDocument pages; "pdf" should call generate for preview.
    /// </summary>
    public string PreviewMode { get; init; } = "dom";

    public string? LayoutProfile { get; init; }
}

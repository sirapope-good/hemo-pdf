namespace Hemo.Pdf.Core.Models;

public sealed class ReportTemplateDefinition
{
    public required string Id { get; init; }
    public string DisplayName { get; init; } = "";
    public bool RequiresSignature { get; init; }
}

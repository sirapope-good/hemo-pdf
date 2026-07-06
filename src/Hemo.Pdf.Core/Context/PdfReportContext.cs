using System.Text.Json;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Context;

public sealed class PdfReportContext
{
    public Guid GenerationId { get; init; } = Guid.NewGuid();
    public required string ReportTemplateId { get; init; }
    public required string TenantCode { get; init; }
    public string? EntityId { get; init; }
    public CustomerBrandingProfile? Branding { get; init; }
    public ReportMetadata Metadata { get; init; } = new();
    public ReportSignatureContext? Signatures { get; init; }
    public JsonElement? Data { get; init; }
    public IDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}

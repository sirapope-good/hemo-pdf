using System.Text.Json;

namespace Hemo.Pdf.Core.Models;

public sealed class GeneratePdfRequest
{
    public required string ReportTemplateId { get; init; }
    public required string TenantCode { get; init; }
    public string? EntityId { get; init; }

    /// <summary>
    /// Client-supplied DTO. Optional when HemoPdf:UseServerFetch is enabled (server loads report-data).
    /// </summary>
    public JsonElement Data { get; init; }

    public ReportSignatureContext? Signatures { get; init; }
    public Dictionary<string, object?>? Parameters { get; init; }
}

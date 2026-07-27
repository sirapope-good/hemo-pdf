using System.Text.Json;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Application;

/// <summary>
/// Resolves Hemo-PDF engine template id from Web.Api report-data
/// (<c>layoutContext.hemoPdfTemplateId</c> ← HemoAdmin Hemosheet template via catalog).
/// </summary>
public static class HemosheetTemplateIdReader
{
    /// <summary>Document-type aliases Hemopro may send instead of an engine template id.</summary>
    private static readonly HashSet<string> DocumentTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "hemosheet",
    };

    public static string? ReadHemoPdfTemplateId(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            return null;

        if (!data.TryGetProperty("layoutContext", out var layoutContext)
            && !data.TryGetProperty("LayoutContext", out layoutContext))
        {
            return null;
        }

        if (layoutContext.ValueKind != JsonValueKind.Object)
            return null;

        if (!layoutContext.TryGetProperty("hemoPdfTemplateId", out var templateId)
            && !layoutContext.TryGetProperty("HemoPdfTemplateId", out templateId))
        {
            return null;
        }

        if (templateId.ValueKind != JsonValueKind.String)
            return null;

        var value = templateId.GetString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// Maps Hemopro document-type keys (e.g. <c>hemosheet</c>) to engine ids.
    /// Known <c>template-*</c> ids pass through unchanged.
    /// </summary>
    public static string NormalizeReportTemplateId(string? reportTemplateId)
    {
        var trimmed = reportTemplateId?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ReportTemplates.Hemosheet;
        }

        if (DocumentTypeAliases.Contains(trimmed))
        {
            return ReportTemplates.Hemosheet;
        }

        return trimmed;
    }

    public static string Resolve(string? requestTemplateId, JsonElement data) =>
        ReadHemoPdfTemplateId(data) ?? NormalizeReportTemplateId(requestTemplateId);
}

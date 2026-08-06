using System.Text.Json;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Application;

/// <summary>
/// Resolves Hemo-PDF engine template id from Web.Api report-data
/// (<c>layoutContext.hemoPdfTemplateId</c>) or request aliases.
/// </summary>
public static class HemosheetTemplateIdReader
{
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
        return string.IsNullOrEmpty(value) ? null : ClinicalReportCatalog.ResolveEngineTemplateId(value);
    }

    /// <summary>
    /// Maps legacy aliases (<c>hemosheet</c>, <c>template-04-hemosheet</c>) to
    /// <see cref="ClinicalReportCatalog.HemodialysisRecord"/>. Known <c>clinical-*</c> ids pass through.
    /// </summary>
    public static string NormalizeReportTemplateId(string? reportTemplateId)
    {
        var trimmed = reportTemplateId?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ClinicalReportCatalog.HemodialysisRecord;
        }

        return ClinicalReportCatalog.ResolveEngineTemplateId(trimmed);
    }

    public static string Resolve(string? requestTemplateId, JsonElement data) =>
        ReadHemoPdfTemplateId(data) ?? NormalizeReportTemplateId(requestTemplateId);
}

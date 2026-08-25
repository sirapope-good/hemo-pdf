using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Layouts.Hprp;

public static class HprpLabelResolver
{
    public static IReadOnlyDictionary<string, string> Resolve(
        IHprpTemplateStore? store,
        PdfReportContext context,
        string? language = null)
    {
        var templateId = ClinicalReportCatalog.ResolveEngineTemplateId(context.ReportTemplateId);
        var package = context.LayoutPackage ?? store?.TryGetCached(context.TenantCode, templateId);
        var lang = language
            ?? package?.Manifest.Language
            ?? (ClinicalReportCatalog.IsConsentReport(context.ReportTemplateId)
                && string.Equals(context.ReportTemplateId, ClinicalReportCatalog.ConsentEn, StringComparison.OrdinalIgnoreCase)
                    ? "en"
                    : "th");
        return HprpLabels.FromPackage(package, lang);
    }
}

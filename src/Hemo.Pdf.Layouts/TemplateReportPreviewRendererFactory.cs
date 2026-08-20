using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Preview.Generic;
using Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts;

public static class TemplateReportPreviewRendererFactory
{
    private static readonly IReadOnlyDictionary<string, Type> DedicatedRenderers =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [ClinicalReportCatalog.HemodialysisRecord] = typeof(HemosheetReportPreviewRenderer),
            [ClinicalReportCatalog.LegacyEngineAlias] = typeof(HemosheetReportPreviewRenderer),
        };

    public static Type ResolveRendererType(string reportTemplateId)
    {
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(reportTemplateId);

        if (DedicatedRenderers.TryGetValue(reportTemplateId, out var dedicated)
            || DedicatedRenderers.TryGetValue(engineId, out dedicated))
        {
            return dedicated;
        }

        // Form reports (known pack or future .hprp packages) use HprpBinder preview.
        return typeof(HprpReportPreviewRenderer);
    }

    public static IReadOnlyList<(string ReportTemplateId, Type RendererType)> CreateRegistrations()
    {
        var registrations = new List<(string, Type)>();

        foreach (var templateId in TemplateRegistration.AllTemplateIds)
        {
            registrations.Add((templateId, ResolveRendererType(templateId)));
        }

        registrations.Add((ClinicalReportCatalog.LegacyEngineAlias, typeof(HemosheetReportPreviewRenderer)));

        return registrations;
    }
}

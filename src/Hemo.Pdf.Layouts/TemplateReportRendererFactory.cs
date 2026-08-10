using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts;

public static class TemplateReportRendererFactory
{
    private static readonly IReadOnlyDictionary<string, Type> DedicatedRenderers =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [ClinicalReportCatalog.HctEpo] = typeof(Clinical01HctEpoReportRenderer),
            [ClinicalReportCatalog.HemodialysisRecord] = typeof(HemosheetReportRenderer),
            [ClinicalReportCatalog.LegacyEngineAlias] = typeof(HemosheetReportRenderer),
        };

    public static Type ResolveRendererType(string reportTemplateId)
    {
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(reportTemplateId);

        if (DedicatedRenderers.TryGetValue(engineId, out var dedicated)
            || DedicatedRenderers.TryGetValue(reportTemplateId, out dedicated))
        {
            return dedicated;
        }

        if (ClinicalReportCatalog.IsKnown(reportTemplateId)
            && !ClinicalReportCatalog.IsHemodialysisRecord(reportTemplateId))
        {
            return typeof(Clinical.ClinicalDefaultReportRenderer);
        }

        return TemplateRegistration.FallbackRendererType;
    }

    public static IReadOnlyList<(string ReportTemplateId, Type RendererType)> CreateRegistrations()
    {
        var registrations = new List<(string, Type)>();

        foreach (var templateId in TemplateRegistration.AllTemplateIds)
        {
            registrations.Add((templateId, ResolveRendererType(templateId)));
        }

        registrations.Add((ClinicalReportCatalog.LegacyEngineAlias, typeof(HemosheetReportRenderer)));

        return registrations;
    }
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Generic;
using Hemo.Pdf.Layouts.Template01_DialysisSession;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts;

public static class TemplateReportRendererFactory
{
    private static readonly IReadOnlyDictionary<string, Type> DedicatedRenderers =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [ReportTemplates.DialysisSession] = typeof(DialysisSessionReportRenderer),
            [ReportTemplates.Hemosheet] = typeof(HemosheetReportRenderer),
        };

    public static Type ResolveRendererType(string reportTemplateId)
    {
        if (DedicatedRenderers.TryGetValue(reportTemplateId, out var dedicated))
        {
            return dedicated;
        }

        if (ReportTemplates.IsKnown(reportTemplateId))
        {
            return typeof(GenericTemplateReportRenderer);
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

        return registrations;
    }
}

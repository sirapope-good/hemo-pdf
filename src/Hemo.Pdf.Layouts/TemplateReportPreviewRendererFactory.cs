using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Preview.Generic;
using Hemo.Pdf.Layouts.Preview.Template01_DialysisSession;
using Hemo.Pdf.Layouts.Preview.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts;

public static class TemplateReportPreviewRendererFactory
{
    private static readonly IReadOnlyDictionary<string, Type> DedicatedRenderers =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [ReportTemplates.DialysisSession] = typeof(DialysisSessionReportPreviewRenderer),
            [ReportTemplates.Hemosheet] = typeof(HemosheetReportPreviewRenderer),
        };

    public static Type ResolveRendererType(string reportTemplateId)
    {
        if (DedicatedRenderers.TryGetValue(reportTemplateId, out var dedicated))
        {
            return dedicated;
        }

        if (ReportTemplates.IsKnown(reportTemplateId))
        {
            return typeof(GenericReportPreviewRenderer);
        }

        return typeof(GenericReportPreviewRenderer);
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

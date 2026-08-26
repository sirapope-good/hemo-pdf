using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Layouts;

public static class TemplateReportRendererFactory
{
    private const string MedicinePreparationRound = "medicine-preparation-round";

    private static readonly IReadOnlyDictionary<string, Type> DedicatedRenderers =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            [ClinicalReportCatalog.HctEpo] = typeof(Clinical01HctEpoReportRenderer),
            [ClinicalReportCatalog.EpoDrug] = typeof(Clinical02EpoDrugReportRenderer),
            [ClinicalReportCatalog.ProgressNote] = typeof(Clinical05ProgressNoteReportRenderer),
            [ClinicalReportCatalog.ProgressNoteChecklist] = typeof(Clinical05ProgressNoteChecklistReportRenderer),
            [ClinicalReportCatalog.ConsentTh] = typeof(ConsentReportRenderer),
            [ClinicalReportCatalog.ConsentEn] = typeof(ConsentReportRenderer),
            [ClinicalReportCatalog.HemodialysisRecord] = typeof(HemosheetReportRenderer),
            [ClinicalReportCatalog.LegacyEngineAlias] = typeof(HemosheetReportRenderer),
            [MedicinePreparationRound] = typeof(MedicinePrep.MedicinePreparationRoundReportRenderer),
        };


    public static Type ResolveRendererType(string reportTemplateId)
    {
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(reportTemplateId);

        if (DedicatedRenderers.TryGetValue(engineId, out var dedicated)
            || DedicatedRenderers.TryGetValue(reportTemplateId, out dedicated))
        {
            return dedicated;
        }

        // Form reports (known pack or future .hprp packages) use HprpBinder pipeline.
        return typeof(Clinical.ClinicalDefaultReportRenderer);
    }

    public static IReadOnlyList<(string ReportTemplateId, Type RendererType)> CreateRegistrations()
    {
        var registrations = new List<(string, Type)>();

        foreach (var templateId in TemplateRegistration.AllTemplateIds)
        {
            registrations.Add((templateId, ResolveRendererType(templateId)));
        }

        registrations.Add((ClinicalReportCatalog.LegacyEngineAlias, typeof(HemosheetReportRenderer)));
        registrations.Add((
            MedicinePreparationRound,
            typeof(MedicinePrep.MedicinePreparationRoundReportRenderer)));

        return registrations;
    }
}

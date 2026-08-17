using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Application;

/// <summary>How <see cref="ReportDataResolver"/> loads trusted JSON for a template.</summary>
public enum ReportDataFetchKind
{
    HemosheetRecordOrTemplate,
    Clinical01HctEpoPatientYear,
    Clinical02EpoDrugPatientMonthMed,
    Clinical05ProgressNotePatientMonth,
    MedicinePreparationRound,
    ConsentPatientTemplateOrRecord,
}

/// <summary>
/// Maps report template ids to server-fetch strategies so clinical forms do not
/// fall through to hemosheet record fetch.
/// </summary>
public static class ReportDataFetchRegistry
{
    public const string MedicinePreparationRound = "medicine-preparation-round";

    public static ReportDataFetchKind Resolve(string? templateId)
    {
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(templateId ?? string.Empty);

        if (string.Equals(engineId, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical01HctEpoPatientYear;

        if (string.Equals(engineId, ClinicalReportCatalog.EpoDrug, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical02EpoDrugPatientMonthMed;

        if (string.Equals(engineId, ClinicalReportCatalog.ProgressNote, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical05ProgressNotePatientMonth;

        if (string.Equals(engineId, MedicinePreparationRound, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.MedicinePreparationRound;

        if (ClinicalReportCatalog.IsConsentReport(engineId))
            return ReportDataFetchKind.ConsentPatientTemplateOrRecord;

        return ReportDataFetchKind.HemosheetRecordOrTemplate;
    }
}

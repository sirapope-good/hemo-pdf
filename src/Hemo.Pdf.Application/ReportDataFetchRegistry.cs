using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Application;

/// <summary>How <see cref="ReportDataResolver"/> loads trusted JSON for a template.</summary>
public enum ReportDataFetchKind
{
    HemosheetRecordOrTemplate,
    Clinical01HctEpoPatientYear,
    Clinical02EpoDrugPatientMonthMed,
    Clinical05ProgressNotePatientMonth,
    Clinical05ProgressNoteChecklistPatientMonthRange,
    Clinical07LabPatient,
    MedicinePreparationRound,
    ConsentPatientTemplateOrRecord,
    /// <summary>
    /// Patient-scoped form: Web.Api path from manifest <c>ui.reportDataPath</c>
    /// (default <c>api/Patients/{patientId}/reports/{templateId}/report-data</c>).
    /// </summary>
    FormPatientByAdapter,
    UnsupportedClinicalForm,
}

/// <summary>
/// Maps report template ids to server-fetch strategies so clinical forms do not
/// fall through to hemosheet record fetch.
/// </summary>
public static class ReportDataFetchRegistry
{
    public const string MedicinePreparationRound = "medicine-preparation-round";

    public const string DefaultFormReportDataPath =
        "api/Patients/{patientId}/reports/{templateId}/report-data";

    public static ReportDataFetchKind Resolve(string? templateId, HprpManifest? manifest = null)
    {
        var engineId = ClinicalReportCatalog.ResolveEngineTemplateId(templateId ?? string.Empty);

        if (string.Equals(engineId, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical01HctEpoPatientYear;

        if (string.Equals(engineId, ClinicalReportCatalog.EpoDrug, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical02EpoDrugPatientMonthMed;

        if (string.Equals(engineId, ClinicalReportCatalog.ProgressNote, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical05ProgressNotePatientMonth;

        if (string.Equals(engineId, ClinicalReportCatalog.ProgressNoteChecklist, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical05ProgressNoteChecklistPatientMonthRange;

        if (string.Equals(engineId, ClinicalReportCatalog.Lab, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.Clinical07LabPatient;

        if (string.Equals(engineId, MedicinePreparationRound, StringComparison.OrdinalIgnoreCase))
            return ReportDataFetchKind.MedicinePreparationRound;

        if (ClinicalReportCatalog.IsConsentReport(engineId))
            return ReportDataFetchKind.ConsentPatientTemplateOrRecord;

        // Pack forms with convention / dedicated Web.Api report-data (04, 06, 07, 10–16).
        if (ClinicalReportCatalog.IsFormPatientReportData(engineId))
            return ReportDataFetchKind.FormPatientByAdapter;

        if (!string.IsNullOrWhiteSpace(manifest?.Ui?.ReportDataPath))
            return ReportDataFetchKind.FormPatientByAdapter;

        // Future form packages (not in ClinicalReportCatalog) with flatten-dto.
        if (manifest is not null
            && !ClinicalReportCatalog.IsKnown(engineId)
            && string.Equals(
                manifest.DataAdapter,
                HprpDataAdapterIds.FlattenDto,
                StringComparison.OrdinalIgnoreCase))
        {
            return ReportDataFetchKind.FormPatientByAdapter;
        }

        return ReportDataFetchKind.HemosheetRecordOrTemplate;
    }

    public static string ResolveFormReportDataPath(HprpManifest? manifest, string templateId)
    {
        var path = manifest?.Ui?.ReportDataPath;
        if (string.IsNullOrWhiteSpace(path))
            path = DefaultFormReportDataPath;

        return path
            .Replace("{templateId}", templateId, StringComparison.OrdinalIgnoreCase)
            .Replace("{TemplateId}", templateId, StringComparison.OrdinalIgnoreCase);
    }
}

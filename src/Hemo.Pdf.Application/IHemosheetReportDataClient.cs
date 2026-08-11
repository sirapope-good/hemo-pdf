using System.Text.Json;

namespace Hemo.Pdf.Application;

public interface IHemosheetReportDataClient
{
    Task<JsonElement> GetRecordReportDataAsync(
        string hemoId,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetTemplateReportDataAsync(
        int unitId,
        string templateMode,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetClinical01HctEpoReportDataAsync(
        string patientId,
        int year,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetClinical02EpoDrugReportDataAsync(
        string patientId,
        string month,
        int medicineId,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetMedicinePreparationRoundReportDataAsync(
        int unitId,
        string date,
        int sectionId,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetConsentReportDataAsync(
        string consentId,
        string language,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);

    Task<JsonElement> GetConsentTemplateReportDataAsync(
        string patientId,
        string consentType,
        string language,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken);
}

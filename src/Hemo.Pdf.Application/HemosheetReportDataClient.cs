using System.Net.Http.Headers;
using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application;

public sealed class HemosheetReportDataClient : IHemosheetReportDataClient
{
    private readonly HttpClient _httpClient;
    private readonly HemoPdfOptions _options;

    public HemosheetReportDataClient(HttpClient httpClient, IOptions<HemoPdfOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<JsonElement> GetRecordReportDataAsync(
        string hemoId,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var path =
            $"api/Hemodialysis/records/{Uri.EscapeDataString(hemoId)}/report-data?tcvUsePercent={tcvUsePercent.ToString().ToLowerInvariant()}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetTemplateReportDataAsync(
        int unitId,
        string templateMode,
        bool tcvUsePercent,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var mode = string.IsNullOrWhiteSpace(templateMode) ? "hd" : templateMode.Trim();
        var path =
            $"api/Hemodialysis/report-data/template?unitId={unitId}&templateMode={Uri.EscapeDataString(mode)}&tcvUsePercent={tcvUsePercent.ToString().ToLowerInvariant()}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetClinical01HctEpoReportDataAsync(
        string patientId,
        int year,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var path =
            $"api/Patients/{Uri.EscapeDataString(patientId)}/reports/{ClinicalReportCatalog.HctEpo}/report-data?year={year.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetConsentReportDataAsync(
        string consentId,
        string language,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "th" : language.Trim().ToLowerInvariant();
        var path =
            $"api/Patients/consent/{Uri.EscapeDataString(consentId)}/report-data?lang={Uri.EscapeDataString(lang)}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetConsentTemplateReportDataAsync(
        string patientId,
        string consentType,
        string language,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "th" : language.Trim().ToLowerInvariant();
        var type = string.IsNullOrWhiteSpace(consentType) ? "Treatment" : consentType.Trim();
        var path =
            $"api/Patients/{Uri.EscapeDataString(patientId)}/consent/report-data/template?type={Uri.EscapeDataString(type)}&lang={Uri.EscapeDataString(lang)}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetClinical02EpoDrugReportDataAsync(
        string patientId,
        string month,
        int medicineId,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var path =
            $"api/Patients/{Uri.EscapeDataString(patientId)}/reports/{ClinicalReportCatalog.EpoDrug}/report-data" +
            $"?month={Uri.EscapeDataString(month)}" +
            $"&medicineId={medicineId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    public Task<JsonElement> GetMedicinePreparationRoundReportDataAsync(
        int unitId,
        string date,
        int sectionId,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var path =
            $"api/hd-treatment/reports/{ReportDataFetchRegistry.MedicinePreparationRound}/report-data" +
            $"?unitId={unitId.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&date={Uri.EscapeDataString(date)}" +
            $"&sectionId={sectionId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return SendAsync(path, authorizationHeader, tenantCode, cancellationToken);
    }

    private async Task<JsonElement> SendAsync(
        string relativePath,
        string? authorizationHeader,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        EnsureBaseAddress();

        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        }

        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Code", tenantCode);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new PdfGenerationBadRequestException("Report data was not found for the given entity.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new PdfGenerationBadRequestException(
                $"Web.Api rejected report-data request (400): {Truncate(body)}");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden
            || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new PdfGenerationForbiddenException(
                "Web.Api denied access while fetching report-data (forwarded credentials).");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Web.Api report-data failed with {(int)response.StatusCode}: {Truncate(body)}");
        }

        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return document.RootElement.Clone();
    }

    private void EnsureBaseAddress()
    {
        if (_httpClient.BaseAddress is not null)
            return;

        var baseUrl = _options.WebApi.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("HemoPdf:WebApi:BaseUrl is required when UseServerFetch is enabled.");
        }

        _httpClient.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
    }

    private static string Truncate(string value) =>
        value.Length <= 240 ? value : value[..240] + "…";
}

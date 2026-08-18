using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application;

/// <summary>
/// Resolves trusted report JSON for generate/preview (client payload or Web.Api S2S fetch).
/// Short-lived cache avoids ThaiUr preview→generate double-fetch of the same report-data.
/// </summary>
public sealed class ReportDataResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private readonly HemoPdfOptions _options;
    private readonly IHemosheetReportDataClient _reportDataClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public ReportDataResolver(
        IOptions<HemoPdfOptions> options,
        IHemosheetReportDataClient reportDataClient,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _options = options.Value;
        _reportDataClient = reportDataClient;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    public async Task<GeneratePdfRequest> ResolveAsync(
        GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.UseServerFetch)
        {
            if (request.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                throw new PdfGenerationBadRequestException(
                    "data is required when HemoPdf:UseServerFetch is disabled.");
            }

            // Prefer layoutContext.hemoPdfTemplateId from payload (HemoAdmin → Web.Api catalog).
            return WithResolvedTemplate(request, request.Data, request.Signatures, request.Parameters);
        }

        if (string.IsNullOrWhiteSpace(request.EntityId))
        {
            throw new PdfGenerationBadRequestException("entityId is required for server fetch.");
        }

        // Frontend must not send unresolved Angular route tokens (e.g. ":hemosheetId").
        if (request.EntityId.StartsWith(":", StringComparison.Ordinal))
        {
            throw new PdfGenerationBadRequestException(
                $"entityId is unresolved ('{request.EntityId}'). Refresh the report page.");
        }

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var tenantCode = request.TenantCode;
        var parameters = request.Parameters ?? new Dictionary<string, object?>();
        var templateId = HemosheetTemplateIdReader.NormalizeReportTemplateId(request.ReportTemplateId);
        var fetchKind = ReportDataFetchRegistry.Resolve(templateId);

        JsonElement data = fetchKind switch
        {
            ReportDataFetchKind.Clinical01HctEpoPatientYear =>
                await FetchClinical01Async(request, parameters, authorization, tenantCode, cancellationToken),
            ReportDataFetchKind.Clinical02EpoDrugPatientMonthMed =>
                await FetchClinical02Async(request, parameters, authorization, tenantCode, cancellationToken),
            ReportDataFetchKind.Clinical05ProgressNotePatientMonth =>
                await FetchClinical05Async(request, parameters, authorization, tenantCode, cancellationToken),
            ReportDataFetchKind.MedicinePreparationRound =>
                await FetchMedicinePreparationRoundAsync(request, parameters, authorization, tenantCode, cancellationToken),
            ReportDataFetchKind.ConsentPatientTemplateOrRecord =>
                await FetchConsentAsync(request, parameters, templateId, authorization, tenantCode, cancellationToken),
            _ => await FetchHemosheetAsync(request, authorization, tenantCode, cancellationToken),
        };

        // Ignore client Data/Signatures when server fetch is on (trusted SoT).
        return WithResolvedTemplate(request, data, signatures: null, parameters);
    }

    private async Task<JsonElement> FetchHemosheetAsync(
        GeneratePdfRequest request,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var spec = HemosheetFetchSpec.FromRequest(request);
        var cacheKey = BuildHemosheetCacheKey(tenantCode, request.EntityId, authorization, spec);

        if (_cache.TryGetValue(cacheKey, out JsonElement cached))
        {
            return cached;
        }

        var data = spec.IsTemplate
            ? await _reportDataClient.GetTemplateReportDataAsync(
                spec.UnitId!.Value,
                spec.TemplateMode,
                spec.TcvUsePercent,
                authorization,
                tenantCode,
                cancellationToken)
            : await _reportDataClient.GetRecordReportDataAsync(
                request.EntityId,
                spec.TcvUsePercent,
                authorization,
                tenantCode,
                cancellationToken);

        _cache.Set(cacheKey, data, CacheDuration);
        return data;
    }

    private async Task<JsonElement> FetchClinical01Async(
        GeneratePdfRequest request,
        Dictionary<string, object?> parameters,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var patientId = HemosheetFetchSpec.ReadString(parameters, "patientId") ?? request.EntityId;
        if (string.IsNullOrWhiteSpace(patientId))
        {
            throw new PdfGenerationBadRequestException("patientId is required for clinical-01 report-data.");
        }

        var year = HemosheetFetchSpec.ReadInt(parameters, "year")
            ?? DateTime.UtcNow.Year;

        var cacheKey = string.Join(
            '|',
            "report-data",
            ClinicalReportCatalog.HctEpo,
            tenantCode.Trim().ToLowerInvariant(),
            patientId.Trim().ToLowerInvariant(),
            year.ToString(CultureInfo.InvariantCulture),
            AuthFingerprint(authorization));

        if (_cache.TryGetValue(cacheKey, out JsonElement cached))
        {
            return cached;
        }

        var data = await _reportDataClient.GetClinical01HctEpoReportDataAsync(
            patientId,
            year,
            authorization,
            tenantCode,
            cancellationToken);

        _cache.Set(cacheKey, data, CacheDuration);
        return data;
    }

    private async Task<JsonElement> FetchClinical02Async(
        GeneratePdfRequest request,
        Dictionary<string, object?> parameters,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var patientId = HemosheetFetchSpec.ReadString(parameters, "patientId") ?? request.EntityId;
        if (string.IsNullOrWhiteSpace(patientId))
        {
            throw new PdfGenerationBadRequestException("patientId is required for clinical-02 report-data.");
        }

        var month = HemosheetFetchSpec.ReadString(parameters, "month")
            ?? HemosheetFetchSpec.ReadString(parameters, "period");
        if (string.IsNullOrWhiteSpace(month))
        {
            throw new PdfGenerationBadRequestException("month (yyyy-MM) is required for clinical-02 report-data.");
        }

        var medicineId = HemosheetFetchSpec.ReadInt(parameters, "medicineId");
        // Seed medicines use negative ids (e.g. Eprex = -233). Only missing / 0 are invalid.
        if (medicineId is null or 0)
        {
            throw new PdfGenerationBadRequestException("medicineId is required for clinical-02 report-data.");
        }

        var monthKey = NormalizeMonthKey(month);
        var cacheKey = string.Join(
            '|',
            "report-data",
            ClinicalReportCatalog.EpoDrug,
            tenantCode.Trim().ToLowerInvariant(),
            patientId.Trim().ToLowerInvariant(),
            monthKey,
            medicineId.Value.ToString(CultureInfo.InvariantCulture),
            AuthFingerprint(authorization));

        if (_cache.TryGetValue(cacheKey, out JsonElement cached))
        {
            return cached;
        }

        var data = await _reportDataClient.GetClinical02EpoDrugReportDataAsync(
            patientId,
            monthKey,
            medicineId.Value,
            authorization,
            tenantCode,
            cancellationToken);

        _cache.Set(cacheKey, data, CacheDuration);
        return data;
    }

    private async Task<JsonElement> FetchClinical05Async(
        GeneratePdfRequest request,
        Dictionary<string, object?> parameters,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var patientId = HemosheetFetchSpec.ReadString(parameters, "patientId") ?? request.EntityId;
        if (string.IsNullOrWhiteSpace(patientId))
        {
            throw new PdfGenerationBadRequestException("patientId is required for clinical-05 report-data.");
        }

        var month = HemosheetFetchSpec.ReadString(parameters, "month")
            ?? HemosheetFetchSpec.ReadString(parameters, "period");
        var monthKey = string.IsNullOrWhiteSpace(month) ? string.Empty : NormalizeMonthKey(month);

        var cacheKey = string.Join(
            '|',
            "report-data",
            ClinicalReportCatalog.ProgressNote,
            tenantCode.Trim().ToLowerInvariant(),
            patientId.Trim().ToLowerInvariant(),
            string.IsNullOrEmpty(monthKey) ? "current" : monthKey,
            AuthFingerprint(authorization));

        if (_cache.TryGetValue(cacheKey, out JsonElement cached))
        {
            return cached;
        }

        var data = await _reportDataClient.GetClinical05ProgressNoteReportDataAsync(
            patientId,
            monthKey,
            authorization,
            tenantCode,
            cancellationToken);

        _cache.Set(cacheKey, data, CacheDuration);
        return data;
    }

    private async Task<JsonElement> FetchMedicinePreparationRoundAsync(
        GeneratePdfRequest request,
        Dictionary<string, object?> parameters,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var unitId = HemosheetFetchSpec.ReadInt(parameters, "unitId");
        if (unitId is null || (unitId == 0 || unitId < -1))
        {
            throw new PdfGenerationBadRequestException(
                "unitId is required for medicine-preparation-round (valid: >0 or -1).");
        }

        // Blank printable form — same header chrome, no patient names.
        if (HemosheetFetchSpec.ReadBool(parameters, HemosheetFetchSpec.TemplateKey))
        {
            return BuildMedicinePreparationBlankTemplate(unitId.Value);
        }

        var date = HemosheetFetchSpec.ReadString(parameters, "date");
        if (string.IsNullOrWhiteSpace(date))
        {
            throw new PdfGenerationBadRequestException("date (yyyy-MM-dd) is required for medicine-preparation-round.");
        }

        var sectionId = HemosheetFetchSpec.ReadInt(parameters, "sectionId");
        if (sectionId is null or <= 0)
        {
            throw new PdfGenerationBadRequestException("sectionId is required for medicine-preparation-round.");
        }

        var cacheKey = string.Join(
            '|',
            "report-data",
            ReportDataFetchRegistry.MedicinePreparationRound,
            tenantCode.Trim().ToLowerInvariant(),
            unitId.Value.ToString(CultureInfo.InvariantCulture),
            date.Trim(),
            sectionId.Value.ToString(CultureInfo.InvariantCulture),
            AuthFingerprint(authorization));

        if (_cache.TryGetValue(cacheKey, out JsonElement cached))
        {
            return cached;
        }

        var data = await _reportDataClient.GetMedicinePreparationRoundReportDataAsync(
            unitId.Value,
            date.Trim(),
            sectionId.Value,
            authorization,
            tenantCode,
            cancellationToken);

        _cache.Set(cacheKey, data, CacheDuration);
        return data;
    }

    /// <summary>
    /// Anonymous blank sheet: reuses the same header structure without patient identity.
    /// </summary>
    private static JsonElement BuildMedicinePreparationBlankTemplate(int unitId)
    {
        const int blankRows = 12;
        var patients = Enumerable.Range(1, blankRows)
            .Select(i => new
            {
                patientId = $"blank-{i}",
                orderNumber = (int?)i,
                hospitalNumber = "",
                name = "",
                birthDate = (string?)null,
                allergies = "",
                coverage = "",
                medicines = new[]
                {
                    new
                    {
                        prescriptionId = Guid.Empty,
                        medicineId = 0,
                        medicineName = "",
                        medicineCode = "",
                        dose = "",
                        frequency = "",
                        route = "",
                        executedByName = "",
                        cosignedByName = "",
                        signatureNames = Array.Empty<string>(),
                    },
                },
            })
            .ToArray();

        var payload = new
        {
            title = "Medicine Preparation Round",
            reportCode = "MED-PRESC-RP-001",
            isTemplate = true,
            header = new
            {
                unitId,
                unitName = "",
                date = (string?)null,
                sectionId = 0,
                roundName = "",
                startTime = (string?)null,
                endTime = (string?)null,
                dateTimeDisplay = "",
            },
            patients,
            layoutContext = new
            {
                hemoPdfTemplateId = ReportDataFetchRegistry.MedicinePreparationRound,
            },
        };

        return JsonSerializer.SerializeToElement(payload);
    }

    /// <summary>Accepts <c>yyyy-MM</c> or legacy <c>MM-yyyy</c>.</summary>
    internal static string NormalizeMonthKey(string month)
    {
        var trimmed = month.Trim();
        if (DateOnly.TryParseExact(
                trimmed + "-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var yyyyMm))
        {
            return $"{yyyyMm.Year:D4}-{yyyyMm.Month:D2}";
        }

        if (DateOnly.TryParseExact(
                "01-" + trimmed,
                "dd-MM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var mmYyyy))
        {
            return $"{mmYyyy.Year:D4}-{mmYyyy.Month:D2}";
        }

        throw new PdfGenerationBadRequestException("month must be yyyy-MM (or MM-yyyy).");
    }

    private async Task<JsonElement> FetchConsentAsync(
        GeneratePdfRequest request,
        Dictionary<string, object?> parameters,
        string templateId,
        string? authorization,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        var lang = HemosheetFetchSpec.ReadString(parameters, "lang")
            ?? (string.Equals(templateId, ClinicalReportCatalog.ConsentEn, StringComparison.OrdinalIgnoreCase)
                ? "en"
                : "th");

        var isTemplate = HemosheetFetchSpec.ReadBool(parameters, HemosheetFetchSpec.TemplateKey);
        if (isTemplate)
        {
            var patientId = HemosheetFetchSpec.ReadString(parameters, "patientId") ?? request.EntityId;
            if (string.IsNullOrWhiteSpace(patientId))
            {
                throw new PdfGenerationBadRequestException("patientId is required for consent template report-data.");
            }

            var consentType = HemosheetFetchSpec.ReadString(parameters, "type") ?? "Treatment";
            var cacheKey = string.Join(
                '|',
                "report-data",
                "consent-template",
                tenantCode.Trim().ToLowerInvariant(),
                patientId.Trim().ToLowerInvariant(),
                consentType.Trim().ToLowerInvariant(),
                lang.Trim().ToLowerInvariant(),
                AuthFingerprint(authorization));

            if (_cache.TryGetValue(cacheKey, out JsonElement cachedTemplate))
            {
                return ConsentDraftOverlay.Apply(cachedTemplate, parameters);
            }

            var templateData = await _reportDataClient.GetConsentTemplateReportDataAsync(
                patientId,
                consentType,
                lang,
                authorization,
                tenantCode,
                cancellationToken);

            _cache.Set(cacheKey, templateData, CacheDuration);
            return ConsentDraftOverlay.Apply(templateData, parameters);
        }

        var consentId = HemosheetFetchSpec.ReadString(parameters, "consentId") ?? request.EntityId;
        if (string.IsNullOrWhiteSpace(consentId))
        {
            throw new PdfGenerationBadRequestException("consentId is required for consent report-data.");
        }

        var cacheKeyRecord = string.Join(
            '|',
            "report-data",
            "consent",
            tenantCode.Trim().ToLowerInvariant(),
            consentId.Trim().ToLowerInvariant(),
            lang.Trim().ToLowerInvariant(),
            AuthFingerprint(authorization));

        if (_cache.TryGetValue(cacheKeyRecord, out JsonElement cached))
        {
            return ConsentDraftOverlay.Apply(cached, parameters);
        }

        var data = await _reportDataClient.GetConsentReportDataAsync(
            consentId,
            lang,
            authorization,
            tenantCode,
            cancellationToken);

        _cache.Set(cacheKeyRecord, data, CacheDuration);
        return ConsentDraftOverlay.Apply(data, parameters);
    }


    private static GeneratePdfRequest WithResolvedTemplate(
        GeneratePdfRequest request,
        JsonElement data,
        ReportSignatureContext? signatures,
        Dictionary<string, object?>? parameters) =>
        new()
        {
            ReportTemplateId = HemosheetTemplateIdReader.Resolve(request.ReportTemplateId, data),
            TenantCode = request.TenantCode,
            EntityId = request.EntityId,
            Data = data,
            Signatures = signatures,
            Parameters = parameters,
        };

    private static string BuildHemosheetCacheKey(
        string tenantCode,
        string entityId,
        string? authorization,
        HemosheetFetchSpec spec)
    {
        return string.Join(
            '|',
            "report-data",
            tenantCode.Trim().ToLowerInvariant(),
            entityId.Trim().ToLowerInvariant(),
            AuthFingerprint(authorization),
            spec.IsTemplate ? "t" : "r",
            spec.UnitId?.ToString(CultureInfo.InvariantCulture) ?? "-",
            spec.TemplateMode,
            spec.TcvUsePercent ? "1" : "0");
    }

    private static string AuthFingerprint(string? authorization) =>
        string.IsNullOrWhiteSpace(authorization)
            ? "anon"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization)))[..16];
}

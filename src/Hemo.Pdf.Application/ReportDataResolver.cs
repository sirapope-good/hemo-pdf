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

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var tenantCode = request.TenantCode;
        var parameters = request.Parameters ?? new Dictionary<string, object?>();
        var templateId = HemosheetTemplateIdReader.NormalizeReportTemplateId(request.ReportTemplateId);

        JsonElement data;
        if (string.Equals(templateId, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase))
        {
            data = await FetchClinical01Async(request, parameters, authorization, tenantCode, cancellationToken);
        }
        else if (ClinicalReportCatalog.IsConsentReport(templateId))
        {
            data = await FetchConsentAsync(request, parameters, templateId, authorization, tenantCode, cancellationToken);
        }
        else
        {
            var spec = HemosheetFetchSpec.FromRequest(request);
            var cacheKey = BuildHemosheetCacheKey(tenantCode, request.EntityId, authorization, spec);

            if (!_cache.TryGetValue(cacheKey, out data))
            {
                data = spec.IsTemplate
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
            }
        }

        // Ignore client Data/Signatures when server fetch is on (trusted SoT).
        return WithResolvedTemplate(request, data, signatures: null, parameters);
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
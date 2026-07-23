using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

            return request;
        }

        if (string.IsNullOrWhiteSpace(request.EntityId))
        {
            throw new PdfGenerationBadRequestException("entityId is required for server fetch.");
        }

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var tenantCode = request.TenantCode;
        var parameters = request.Parameters ?? new Dictionary<string, object?>();
        var spec = HemosheetFetchSpec.FromRequest(request);
        var cacheKey = BuildCacheKey(tenantCode, request.EntityId, authorization, spec);

        if (!_cache.TryGetValue(cacheKey, out JsonElement data))
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

        // Ignore client Data/Signatures when server fetch is on (trusted SoT).
        // Keep caller's EntityId for template correlation (FE uses template-{unitId});
        // validator skips id match when IsTemplate.
        return new GeneratePdfRequest
        {
            ReportTemplateId = request.ReportTemplateId,
            TenantCode = request.TenantCode,
            EntityId = request.EntityId,
            Data = data,
            Signatures = null,
            Parameters = parameters,
        };
    }

    private static string BuildCacheKey(
        string tenantCode,
        string entityId,
        string? authorization,
        HemosheetFetchSpec spec)
    {
        var authFingerprint = string.IsNullOrWhiteSpace(authorization)
            ? "anon"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization)))[..16];

        return string.Join(
            '|',
            "report-data",
            tenantCode.Trim().ToLowerInvariant(),
            entityId.Trim().ToLowerInvariant(),
            authFingerprint,
            spec.IsTemplate ? "t" : "r",
            spec.UnitId?.ToString(CultureInfo.InvariantCulture) ?? "-",
            spec.TemplateMode,
            spec.TcvUsePercent ? "1" : "0");
    }
}

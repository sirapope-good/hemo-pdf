using System.Text.Json;
using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Core.Tests;

public class ReportDataResolverCacheTests
{
    [Fact]
    public async Task ServerFetch_ReusesCachedPayload_AcrossCalls()
    {
        var client = new CountingReportDataClient();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var http = new DefaultHttpContext();
        http.Request.Headers.Authorization = "Bearer token-1";
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = Options.Create(new HemoPdfOptions
        {
            UseServerFetch = true,
            WebApi = new WebApiOptions { BaseUrl = "http://localhost:8200" },
        });

        var resolver = new ReportDataResolver(options, client, accessor, cache);
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = "clinical-03-hemodialysis-record",
            TenantCode = "local",
            EntityId = "hemo-1",
            Data = default,
            Parameters = new Dictionary<string, object?> { ["tcvUsePercent"] = false },
        };

        await resolver.ResolveAsync(request, CancellationToken.None);
        await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Equal(1, client.RecordCalls);
    }

    private sealed class CountingReportDataClient : IHemosheetReportDataClient
    {
        public int RecordCalls { get; private set; }

        public Task<JsonElement> GetRecordReportDataAsync(
            string hemoId,
            bool tcvUsePercent,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken)
        {
            RecordCalls++;
            var json = """{"id":"hemo-1","layoutContext":{"hemoPdfTemplateId":"clinical-03-hemodialysis-record","layoutProfile":"ThaiUr"}}""";
            return Task.FromResult(JsonDocument.Parse(json).RootElement.Clone());
        }

        public Task<JsonElement> GetTemplateReportDataAsync(
            int unitId,
            string templateMode,
            bool tcvUsePercent,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetClinical01HctEpoReportDataAsync(
            string patientId,
            int year,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetConsentReportDataAsync(
            string consentId,
            string language,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetConsentTemplateReportDataAsync(
            string patientId,
            string consentType,
            string language,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetClinical02EpoDrugReportDataAsync(
            string patientId,
            string month,
            int medicineId,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetClinical05ProgressNoteReportDataAsync(
            string patientId,
            string month,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<JsonElement> GetMedicinePreparationRoundReportDataAsync(
            int unitId,
            string date,
            int sectionId,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}

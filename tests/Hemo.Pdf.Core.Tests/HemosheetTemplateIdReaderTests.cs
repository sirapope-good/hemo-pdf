using System.Text.Json;
using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetTemplateIdReaderTests
{
    [Fact]
    public void ReadHemoPdfTemplateId_FromCamelCaseLayoutContext()
    {
        var data = JsonDocument.Parse(
            """{"id":"hemo-1","layoutContext":{"hemoPdfTemplateId":"clinical-03-hemodialysis-record","layoutProfile":"ThaiUr"}}""")
            .RootElement;

        Assert.Equal(ClinicalReportCatalog.HemodialysisRecord, HemosheetTemplateIdReader.ReadHemoPdfTemplateId(data));
    }

    [Fact]
    public void Normalize_DocumentTypeAlias_MapsToHemosheetEngine()
    {
        Assert.Equal(ClinicalReportCatalog.HemodialysisRecord, HemosheetTemplateIdReader.NormalizeReportTemplateId("hemosheet"));
        Assert.Equal(ClinicalReportCatalog.HemodialysisRecord, HemosheetTemplateIdReader.NormalizeReportTemplateId("Hemosheet"));
        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            HemosheetTemplateIdReader.NormalizeReportTemplateId(ClinicalReportCatalog.HemodialysisRecord));
    }

    [Fact]
    public void Normalize_LegacyEngineAlias_MapsToClinical03()
    {
        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            HemosheetTemplateIdReader.NormalizeReportTemplateId(ClinicalReportCatalog.LegacyEngineAlias));
    }

    [Fact]
    public void Resolve_PrefersLayoutContextOverRequestAlias()
    {
        var data = JsonDocument.Parse(
            """{"layoutContext":{"hemoPdfTemplateId":"clinical-03-hemodialysis-record"}}""")
            .RootElement;

        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            HemosheetTemplateIdReader.Resolve("hemosheet", data));
    }
}

public class ReportDataResolverTemplateIdTests
{
    [Fact]
    public async Task ServerFetch_UsesHemoPdfTemplateId_FromReportData()
    {
        var client = new FixedReportDataClient(
            """{"id":"hemo-1","layoutContext":{"hemoPdfTemplateId":"clinical-03-hemodialysis-record","layoutProfile":"ThaiUr"}}""");
        var resolver = CreateResolver(client, useServerFetch: true);
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = "hemosheet",
            TenantCode = "local",
            EntityId = "hemo-1",
            Data = default,
            Parameters = new Dictionary<string, object?> { ["tcvUsePercent"] = false },
        };

        var resolved = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Equal(ClinicalReportCatalog.HemodialysisRecord, resolved.ReportTemplateId);
        Assert.Equal(JsonValueKind.Object, resolved.Data.ValueKind);
    }

    [Fact]
    public async Task ClientPayload_UsesHemoPdfTemplateId_FromData()
    {
        var data = JsonDocument.Parse(
            """{"id":"hemo-1","layoutContext":{"hemoPdfTemplateId":"clinical-03-hemodialysis-record"}}""")
            .RootElement.Clone();
        var resolver = CreateResolver(new CountingUnusedClient(), useServerFetch: false);
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = "hemosheet",
            TenantCode = "local",
            EntityId = "hemo-1",
            Data = data,
        };

        var resolved = await resolver.ResolveAsync(request, CancellationToken.None);

        Assert.Equal(ClinicalReportCatalog.HemodialysisRecord, resolved.ReportTemplateId);
    }

    private static ReportDataResolver CreateResolver(IHemosheetReportDataClient client, bool useServerFetch)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var http = new DefaultHttpContext();
        http.Request.Headers.Authorization = "Bearer token-1";
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = Options.Create(new HemoPdfOptions
        {
            UseServerFetch = useServerFetch,
            WebApi = new WebApiOptions { BaseUrl = "http://localhost:8200" },
        });
        return new ReportDataResolver(options, client, accessor, cache);
    }

    private sealed class FixedReportDataClient(string json) : IHemosheetReportDataClient
    {
        public Task<JsonElement> GetRecordReportDataAsync(
            string hemoId,
            bool tcvUsePercent,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(JsonDocument.Parse(json).RootElement.Clone());

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

        public Task<JsonElement> GetClinical07LabReportDataAsync(
            string patientId,
            string? from,
            string? to,
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

    private sealed class CountingUnusedClient : IHemosheetReportDataClient
    {
        public Task<JsonElement> GetRecordReportDataAsync(
            string hemoId,
            bool tcvUsePercent,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetTemplateReportDataAsync(
            int unitId,
            string templateMode,
            bool tcvUsePercent,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetClinical01HctEpoReportDataAsync(
            string patientId,
            int year,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetConsentReportDataAsync(
            string consentId,
            string language,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetConsentTemplateReportDataAsync(
            string patientId,
            string consentType,
            string language,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetClinical02EpoDrugReportDataAsync(
            string patientId,
            string month,
            int medicineId,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetClinical05ProgressNoteReportDataAsync(
            string patientId,
            string month,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetClinical07LabReportDataAsync(
            string patientId,
            string? from,
            string? to,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");

        public Task<JsonElement> GetMedicinePreparationRoundReportDataAsync(
            int unitId,
            string date,
            int sectionId,
            string? authorizationHeader,
            string tenantCode,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not fetch when UseServerFetch is false.");
    }
}

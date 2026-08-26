using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpStudioPreviewRequest
{
    public required string TemplateId { get; init; }
    public string? Variant { get; init; }
    public HprpStudioPackageDto? Package { get; init; }
    public JsonElement Data { get; init; }

    /// <summary>
    /// When set, preview uses the same server-fetch path as <c>POST /api/pdf/generate</c>
    /// (real report-data), then applies the Studio package overlay — WYSIWYG with print.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>Optional sample scenario id (e.g. <c>hdf</c> → sample.hdf.json). Ignored when EntityId is set.</summary>
    public string? SampleScenario { get; init; }
}

public sealed class HprpStudioPreviewService
{
    private readonly IBrandingResolver _branding;
    private readonly IReportSignatureResolver _signatures;
    private readonly IReportRendererFactory _renderers;
    private readonly IHprpTemplateStore _store;
    private readonly ITenantContextAccessor _tenant;
    private readonly ReportRequestPipeline _requestPipeline;
    private readonly string _templatesRoot;

    public HprpStudioPreviewService(
        IBrandingResolver branding,
        IReportSignatureResolver signatures,
        IReportRendererFactory renderers,
        IHprpTemplateStore store,
        ITenantContextAccessor tenant,
        ReportRequestPipeline requestPipeline,
        IOptions<HprpTemplateOptions> options)
    {
        _branding = branding;
        _signatures = signatures;
        _renderers = renderers;
        _store = store;
        _tenant = tenant;
        _requestPipeline = requestPipeline;
        _templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
    }

    public async Task<byte[]> PreviewAsync(HprpStudioPreviewRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
            throw new PdfGenerationBadRequestException("templateId is required.");

        var tenant = _tenant.TenantCode;
        var templateId = request.TemplateId.Trim();

        HprpPackage? overlay;
        if (request.Package is not null)
        {
            overlay = request.Package.ToPackage();
            var validation = HprpValidator.Validate(overlay);
            if (!validation.IsValid)
                throw new PdfGenerationBadRequestException(string.Join(" ", validation.Errors));
        }
        else
        {
            overlay = _store.TryGetCached(tenant, templateId, request.Variant);
        }

        var variant = request.Variant ?? overlay?.Manifest.Variant;
        JsonElement previewData;

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            // Same fetch + validate path as /api/pdf/generate (print).
            var prepared = await _requestPipeline.PrepareAsync(
                new GeneratePdfRequest
                {
                    ReportTemplateId = templateId,
                    TenantCode = tenant,
                    EntityId = request.EntityId.Trim(),
                },
                cancellationToken);
            previewData = prepared.Data;
        }
        else if (IsObject(request.Data))
        {
            previewData = request.Data;
        }
        else
        {
            var sample = HprpStudioSamplePayloads.TryLoad(
                _templatesRoot,
                templateId,
                variant,
                request.SampleScenario);
            if (sample is null || !IsObject(sample.Value))
            {
                throw new PdfGenerationBadRequestException(
                    "No sample payload for this template. Add reports/{id}/sample.json or send data / entityId.");
            }

            previewData = sample.Value;
        }

        if (ClinicalReportCatalog.IsHemodialysisRecord(templateId))
        {
            previewData = HprpStudioSamplePayloads.ApplyHemosheetPreviewContext(
                previewData,
                overlay,
                variant);
        }

        var generate = new GeneratePdfRequest
        {
            ReportTemplateId = templateId,
            TenantCode = tenant,
            EntityId = string.IsNullOrWhiteSpace(request.EntityId) ? null : request.EntityId.Trim(),
            Data = previewData,
        };

        var context = await ReportPipeline.BuildContextAsync(
            generate,
            _branding,
            _signatures,
            cancellationToken,
            _store,
            overlay);

        var renderer = _renderers.Create(generate.ReportTemplateId);
        return await renderer.RenderReportAsync(context, cancellationToken);
    }

    private static bool IsObject(JsonElement data) =>
        data.ValueKind == JsonValueKind.Object;
}

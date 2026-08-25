using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
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
}

public sealed class HprpStudioPreviewService
{
    private readonly IBrandingResolver _branding;
    private readonly IReportSignatureResolver _signatures;
    private readonly IReportRendererFactory _renderers;
    private readonly IHprpTemplateStore _store;
    private readonly ITenantContextAccessor _tenant;
    private readonly string _templatesRoot;

    public HprpStudioPreviewService(
        IBrandingResolver branding,
        IReportSignatureResolver signatures,
        IReportRendererFactory renderers,
        IHprpTemplateStore store,
        ITenantContextAccessor tenant,
        IOptions<HprpTemplateOptions> options)
    {
        _branding = branding;
        _signatures = signatures;
        _renderers = renderers;
        _store = store;
        _tenant = tenant;
        _templatesRoot = HprpDiskPaths.ResolveExistingOrConfigured(options.Value.RootPath);
    }

    public async Task<byte[]> PreviewAsync(HprpStudioPreviewRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
            throw new PdfGenerationBadRequestException("templateId is required.");

        var tenant = _tenant.TenantCode;
        var data = IsObject(request.Data)
            ? request.Data
            : HprpStudioSamplePayloads.TryLoad(_templatesRoot, request.TemplateId);

        if (data is null || !IsObject(data.Value))
        {
            throw new PdfGenerationBadRequestException(
                "No sample payload for this template. Add reports/{id}/sample.json or send data.");
        }

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
            overlay = _store.TryGetCached(tenant, request.TemplateId, request.Variant);
        }

        var generate = new GeneratePdfRequest
        {
            ReportTemplateId = request.TemplateId.Trim(),
            TenantCode = tenant,
            EntityId = null,
            Data = data.Value,
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

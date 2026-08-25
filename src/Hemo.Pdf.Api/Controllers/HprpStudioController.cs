using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Hprp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/hprp")]
public sealed class HprpStudioController : ControllerBase
{
    private readonly HprpPackService _pack;
    private readonly IHprpTemplateStore _store;
    private readonly ITenantContextAccessor _tenant;
    private readonly HprpTemplateOptions _options;
    private readonly HprpStudioPreviewService _preview;

    public HprpStudioController(
        HprpPackService pack,
        IHprpTemplateStore store,
        ITenantContextAccessor tenant,
        IOptions<HprpTemplateOptions> options,
        HprpStudioPreviewService preview)
    {
        _pack = pack;
        _store = store;
        _tenant = tenant;
        _options = options.Value;
        _preview = preview;
    }

    [HttpGet("catalog")]
    public IActionResult Catalog() => Ok(HprpStudioCatalog.Describe());

    [HttpGet("packages")]
    public IActionResult List()
    {
        var items = _store.ListCachedPackages()
            .Select(p => new HprpStudioListItemDto
            {
                Id = p.Manifest.Id,
                Variant = p.Manifest.Variant,
                DisplayName = p.Manifest.DisplayName,
                SourcePath = p.SourcePath,
                Packed = p.SourcePath.EndsWith(HprpEngine.FileExtension, StringComparison.OrdinalIgnoreCase),
            })
            .ToList();
        return Ok(items);
    }

    [HttpGet("packages/{templateId}")]
    public IActionResult Get(string templateId, [FromQuery] string? variant)
    {
        var package = _store.TryGetCached(_tenant.TenantCode, templateId, variant)
            ?? _pack.ReadPackedFile(templateId, variant);
        if (package is null)
            return NotFound();

        return Ok(HprpStudioPackageDto.FromPackage(package));
    }

    [HttpPut("packages/{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<IActionResult> Save(
        string templateId,
        [FromQuery] string? variant,
        [FromBody] HprpStudioPackageDto body,
        CancellationToken cancellationToken)
    {
        EnsureWritesEnabled();
        if (body.Manifest is null || string.IsNullOrWhiteSpace(body.Manifest.Id))
            throw new PdfGenerationBadRequestException("manifest.id is required.");
        if (!string.Equals(body.Manifest.Id, templateId, StringComparison.OrdinalIgnoreCase))
            throw new PdfGenerationBadRequestException("manifest.id must match the URL templateId.");

        var resolvedVariant = string.IsNullOrWhiteSpace(variant)
            ? body.Manifest.Variant
            : variant;
        var package = body.ToPackage();
        var validation = _pack.Validate(package);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var includeVariant = ShouldUseVariantFileName(templateId, resolvedVariant);
        var output = _pack.PackageOutputPath(templateId, resolvedVariant, includeVariant);
        var result = await _pack.WritePackageAsync(package, output, cancellationToken);
        return Ok(result);
    }

    private bool ShouldUseVariantFileName(string templateId, string? variant)
    {
        if (!HprpTemplatePaths.IsDefaultVariant(variant))
            return true;

        var withVariant = _pack.PackageOutputPath(templateId, variant, includeVariantSegment: true);
        return System.IO.File.Exists(withVariant);
    }

    [HttpPost("preview")]
    [EnableRateLimiting("PdfGeneration")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Preview(
        [FromBody] HprpStudioPreviewRequest body,
        CancellationToken cancellationToken)
    {
        var pdfBytes = await _preview.PreviewAsync(body, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(pdfBytes, "application/pdf");
    }

    [HttpPost("validate")]
    public IActionResult Validate([FromBody] HprpStudioPackageDto body)
    {
        var validation = _pack.Validate(body.ToPackage());
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });
        return Ok(new { valid = true });
    }

    [HttpPost("pack-from-templates")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<IActionResult> PackAll(CancellationToken cancellationToken)
    {
        EnsureWritesEnabled();
        var packed = await _pack.PackAllFromTemplatesAsync(cancellationToken);
        return Ok(packed);
    }

    [HttpPost("pack-from-templates/{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<IActionResult> PackOne(string templateId, CancellationToken cancellationToken)
    {
        EnsureWritesEnabled();
        var packed = await _pack.PackTemplateIdAsync(templateId, cancellationToken);
        return Ok(packed);
    }

    private void EnsureWritesEnabled()
    {
        if (!_options.EnableHprpStudioWrite)
        {
            throw new PdfGenerationForbiddenException(
                "HPRP Studio writes are disabled. Set HemoPdf:EnableHprpStudioWrite=true (Development).");
        }
    }
}

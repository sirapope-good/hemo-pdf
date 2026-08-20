using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Hprp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hemo.Pdf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public sealed class HprpTemplateController : ControllerBase
{
    private readonly IHprpTemplateStore _store;
    private readonly ITenantContextAccessor _tenant;

    public HprpTemplateController(IHprpTemplateStore store, ITenantContextAccessor tenant)
    {
        _store = store;
        _tenant = tenant;
    }

    [HttpGet]
    public IActionResult List()
    {
        var tenant = _tenant.TenantCode;
        var items = _store.ListDefaultManifests().Select(m => new
        {
            m.Id,
            m.DisplayName,
            m.RequiresSignature,
            m.DataAdapter,
            m.EngineVersion,
            hasTenantOverride = _store.HasTenantOverride(tenant, m.Id),
        });
        return Ok(items);
    }

    [HttpGet("{templateId}")]
    public IActionResult Get(string templateId)
    {
        var package = _store.TryGetCached(_tenant.TenantCode, templateId);
        if (package is null)
            return NotFound();

        return Ok(new
        {
            package.Manifest,
            hasTenantOverride = _store.HasTenantOverride(_tenant.TenantCode, templateId),
            sourcePath = package.SourcePath,
        });
    }

    [HttpPost("{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    [Consumes("application/octet-stream", "application/zip", "multipart/form-data")]
    public async Task<IActionResult> Upload(string templateId, CancellationToken cancellationToken)
    {
        Stream stream;
        if (Request.HasFormContentType && Request.Form.Files.Count > 0)
        {
            stream = Request.Form.Files[0].OpenReadStream();
        }
        else
        {
            stream = Request.Body;
        }

        await using (stream)
        {
            await _store.SaveTenantOverrideAsync(_tenant.TenantCode, templateId, stream, cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<IActionResult> Delete(string templateId, CancellationToken cancellationToken)
    {
        await _store.DeleteTenantOverrideAsync(_tenant.TenantCode, templateId, cancellationToken);
        return NoContent();
    }
}

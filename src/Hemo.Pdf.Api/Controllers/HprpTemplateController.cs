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
    public IActionResult List([FromQuery] string? role)
    {
        var manifests = string.IsNullOrWhiteSpace(role)
            ? _store.ListDefaultManifests()
            : _store.ListLayoutProfiles(role);

        return Ok(manifests.Select(MapItem));
    }

    [HttpGet("{templateId}")]
    public IActionResult Get(string templateId, [FromQuery] string? variant)
    {
        var package = _store.TryGetCached(_tenant.TenantCode, templateId, variant);
        if (package is null)
            return NotFound();

        return Ok(new
        {
            package.Manifest,
            hasTenantOverride = false,
            sourcePath = package.SourcePath,
        });
    }

    [HttpPost("{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    public IActionResult Upload(string templateId) =>
        StatusCode(StatusCodes.Status410Gone, new
        {
            message = "Tenant .hprp uploads are disabled. Add a variant folder under assets/templates/reports/.",
        });

    [HttpDelete("{templateId}")]
    [EnableRateLimiting("PdfGeneration")]
    public IActionResult Delete(string templateId) =>
        StatusCode(StatusCodes.Status410Gone, new
        {
            message = "Tenant .hprp uploads are disabled. Add a variant folder under assets/templates/reports/.",
        });

    private static object MapItem(HprpManifest m) => new
    {
        m.Id,
        m.DisplayName,
        m.Variant,
        m.LayoutKind,
        m.LayoutProfile,
        profileLabel = m.Ui?.ProfileLabel,
        role = m.Ui?.Role,
        m.RequiresSignature,
        m.DataAdapter,
        m.EngineVersion,
        hasTenantOverride = false,
    };
}

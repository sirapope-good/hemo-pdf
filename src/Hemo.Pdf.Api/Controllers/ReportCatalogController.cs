using Hemo.Pdf.Application.Catalog;
using Hemo.Pdf.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hemo.Pdf.Api.Controllers;

/// <summary>
/// FE menu / picker catalog — merges .hprp manifests with C# fetch/renderer capability.
/// </summary>
[ApiController]
[Authorize]
[Route("api/report-catalog")]
public sealed class ReportCatalogController : ControllerBase
{
    private readonly IReportCatalogService _catalog;
    private readonly ITenantContextAccessor _tenant;

    public ReportCatalogController(IReportCatalogService catalog, ITenantContextAccessor tenant)
    {
        _catalog = catalog;
        _tenant = tenant;
    }

    /// <summary>
    /// Returns report templates available for this tenant.
    /// Use <paramref name="menuOnly"/>=true for Reports accordion (visibleInMenu filter).
    /// </summary>
    [HttpGet]
    public IActionResult Get([FromQuery] bool menuOnly = false)
    {
        var items = _catalog.GetCatalog(_tenant.TenantCode, menuOnly);
        return Ok(items);
    }
}

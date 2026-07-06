using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hemo.Pdf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/report")]
public sealed class ReportPreviewController : ControllerBase
{
    private readonly IReportPreviewService _reportPreviewService;

    public ReportPreviewController(IReportPreviewService reportPreviewService)
    {
        _reportPreviewService = reportPreviewService;
    }

    [HttpPost("preview")]
    [EnableRateLimiting("PdfGeneration")]
    public async Task<IActionResult> Preview(
        [FromBody] GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        var document = await _reportPreviewService.PreviewAsync(request, cancellationToken);
        return Ok(document);
    }
}

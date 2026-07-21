using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hemo.Pdf.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pdf")]
[Consumes("application/json")]
public sealed class PdfController : ControllerBase
{
    private readonly IPdfGenerationService _pdfGenerationService;

    public PdfController(IPdfGenerationService pdfGenerationService)
    {
        _pdfGenerationService = pdfGenerationService;
    }

    [HttpPost("generate")]
    [EnableRateLimiting("PdfGeneration")]
    [Produces("application/pdf")]
    public async Task<IActionResult> Generate(
        [FromBody] GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        var pdfBytes = await _pdfGenerationService.GenerateAsync(request, cancellationToken);
        var fileName = $"report-{request.EntityId ?? "export"}.pdf";
        Response.Headers.CacheControl = "no-store";
        return File(pdfBytes, "application/pdf", fileName);
    }
}

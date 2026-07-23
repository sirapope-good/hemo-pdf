using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Models;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application;

/// <summary>
/// Shared validate → resolve-data → re-validate steps for preview and generate.
/// </summary>
public sealed class ReportRequestPipeline
{
    private readonly ReportDataResolver _reportDataResolver;
    private readonly ITenantContextAccessor _tenantContext;
    private readonly HemoPdfOptions _options;

    public ReportRequestPipeline(
        ReportDataResolver reportDataResolver,
        ITenantContextAccessor tenantContext,
        IOptions<HemoPdfOptions> options)
    {
        _reportDataResolver = reportDataResolver;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public async Task<GeneratePdfRequest> PrepareAsync(
        GeneratePdfRequest request,
        CancellationToken cancellationToken)
    {
        GeneratePdfRequestValidator.Validate(
            request,
            _tenantContext,
            allowMissingData: _options.UseServerFetch);

        request = await _reportDataResolver.ResolveAsync(request, cancellationToken);

        GeneratePdfRequestValidator.Validate(
            request,
            _tenantContext,
            allowMissingData: false);

        return request;
    }
}

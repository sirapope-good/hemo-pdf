using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Layouts.Placeholder;

public sealed class PlaceholderDataProvider : IReportDataProvider
{
    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new SimpleReportViewModel
        {
            Title = context.Metadata.Title,
            Subtitle = context.Metadata.Subtitle,
            Rows =
            [
                new("Template", context.ReportTemplateId),
                new("Tenant", context.TenantCode),
                new("Entity", context.EntityId ?? "—"),
            ],
        };

        return Task.FromResult<object>(viewModel);
    }
}

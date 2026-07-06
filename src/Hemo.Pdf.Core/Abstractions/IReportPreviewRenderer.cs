using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Abstractions;

public interface IReportPreviewRenderer
{
    Task<ReportDocument> RenderPreviewAsync(PdfReportContext context, CancellationToken cancellationToken);
}

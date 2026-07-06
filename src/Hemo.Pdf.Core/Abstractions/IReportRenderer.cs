using Hemo.Pdf.Core.Context;

namespace Hemo.Pdf.Core.Abstractions;

public interface IReportRenderer
{
    Task<byte[]> RenderReportAsync(PdfReportContext context, CancellationToken cancellationToken);
}

using Hemo.Pdf.Core.Context;

namespace Hemo.Pdf.Core.Abstractions;

public interface IReportDataProvider
{
    Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken);
}

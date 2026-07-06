using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Application;

public interface IReportPreviewService
{
    Task<ReportDocument> PreviewAsync(GeneratePdfRequest request, CancellationToken cancellationToken);
}

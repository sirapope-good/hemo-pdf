using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Abstractions;

public interface IReportDocumentComposer
{
    ReportDocument Compose(object dataModel, PdfReportContext context);
}

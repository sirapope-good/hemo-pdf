using Hemo.Pdf.Core.Context;

namespace Hemo.Pdf.Core.Abstractions;

public interface ILayoutComposer
{
    object Compose(object dataModel, PdfReportContext context);
}

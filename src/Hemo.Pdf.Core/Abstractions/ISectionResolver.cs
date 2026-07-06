using Hemo.Pdf.Core.Context;

namespace Hemo.Pdf.Core.Abstractions;

public interface ISectionResolver<T>
    where T : notnull
{
    T Resolve(PdfReportContext context);
}

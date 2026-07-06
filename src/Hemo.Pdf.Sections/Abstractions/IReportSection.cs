using Hemo.Pdf.Core.Context;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Abstractions;

public interface IReportSection
{
    void Compose(IContainer container, object viewModel, PdfReportContext context);
}

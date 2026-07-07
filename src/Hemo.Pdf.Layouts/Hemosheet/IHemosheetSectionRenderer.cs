using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hemosheet;

public interface IHemosheetSectionRenderer
{
    HemosheetSectionId SectionId { get; }

    IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context);

    void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context);
}

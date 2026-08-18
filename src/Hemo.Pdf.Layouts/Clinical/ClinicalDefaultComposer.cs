using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical;

public sealed class ClinicalDefaultComposer : BaseReportComposer<HprpBoundViewModel>
{
    public ClinicalDefaultComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
    }

    protected override void ComposeContent(
        IContainer container,
        HprpBoundViewModel viewModel,
        PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            foreach (var block in viewModel.Blocks)
            {
                col.Item().Element(c => ReportBlockPdfComposer.Compose(c, block, context));
            }
        });
    }
}

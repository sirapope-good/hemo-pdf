using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Placeholder;

public sealed class PlaceholderComposer : BaseReportComposer<SimpleReportViewModel>
{
    private readonly KeyValueTableSection _keyValueTable = new();

    public PlaceholderComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
    }

    protected override void ComposeContent(IContainer container, SimpleReportViewModel viewModel, PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            col.Item().Text("Placeholder Report")
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                .SemiBold();

            col.Item().Element(c => _keyValueTable.Compose(c, viewModel, context));
        });
    }
}

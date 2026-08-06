using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical;

/// <summary>
/// Default structure shell for clinical pack reports (01–02, 04–16).
/// Uses shared Configurable header/footer; body is a foundation placeholder.
/// </summary>
public sealed class ClinicalDefaultComposer : BaseReportComposer<SimpleReportViewModel>
{
    private readonly KeyValueTableSection _keyValueTable = new();

    public ClinicalDefaultComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
    }

    protected override void ComposeContent(
        IContainer container,
        SimpleReportViewModel viewModel,
        PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            col.Item().Text(viewModel.Title ?? "Clinical Report")
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                .SemiBold();

            if (!string.IsNullOrWhiteSpace(viewModel.Subtitle))
            {
                col.Item().Text(viewModel.Subtitle!)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(9);
            }

            col.Item().Element(c => _keyValueTable.Compose(c, viewModel, context));
        });
    }
}

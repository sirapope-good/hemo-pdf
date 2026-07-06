using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Template01_DialysisSession;

public sealed class DialysisSessionComposer : BaseReportComposer<DialysisSessionViewModel>
{
    private readonly PatientInfoSection _patientInfo = new();
    private readonly KeyValueTableSection _keyValueTable = new();
    private readonly DataGridSection _dataGrid = new();
    private readonly SignatureBlockSection _signatureBlock = new();

    public DialysisSessionComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
    }

    protected override void ComposeContent(IContainer container, DialysisSessionViewModel viewModel, PdfReportContext context)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            col.Item().Element(c => _patientInfo.Compose(c, viewModel, context));
            col.Item().Element(c => _keyValueTable.Compose(c, viewModel, context));
            col.Item().Element(c => _dataGrid.Compose(c, viewModel, context));
            col.Item().Element(c => _signatureBlock.Compose(c, viewModel, context));
        });
    }
}

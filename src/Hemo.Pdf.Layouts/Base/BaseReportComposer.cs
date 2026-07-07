using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Base;

public abstract class BaseReportComposer<TViewModel> : ILayoutComposer
{
    private readonly ISectionResolver<IReportHeaderSection> _headerResolver;
    private readonly ISectionResolver<IReportFooterSection> _footerResolver;

    protected BaseReportComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
    {
        _headerResolver = headerResolver;
        _footerResolver = footerResolver;
    }

    public object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (TViewModel)dataModel;
        PrepareContext(context, viewModel);

        return new QuestLayout
        {
            MarginMillimeters = ReportPageLayout.MarginHorizontalMm,
            MarginTop = ReportPageLayout.MarginTopMm,
            MarginBottom = ReportPageLayout.MarginBottomMm,
            MarginLeft = ReportPageLayout.MarginHorizontalMm,
            MarginRight = ReportPageLayout.MarginHorizontalMm,
            Header = c => _headerResolver.Resolve(context).Compose(c, viewModel, context),
            Content = c => ComposeContent(c, viewModel, context),
            Footer = c => _footerResolver.Resolve(context).Compose(c, viewModel, context),
        };
    }

    protected abstract void ComposeContent(IContainer container, TViewModel viewModel, PdfReportContext context);

    protected virtual void PrepareContext(PdfReportContext context, TViewModel viewModel) { }
}

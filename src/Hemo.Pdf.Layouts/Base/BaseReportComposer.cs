using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Layouts.Hprp;
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

    public virtual object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (TViewModel)dataModel;
        PrepareContext(context, viewModel);

        var page = HprpPageLayout.FromPackage(context.LayoutPackage, ReportPageFallback);
        return HprpQuestPages.Create(
            page,
            c => _headerResolver.Resolve(context).Compose(c, viewModel, context),
            c => ComposeContent(c, viewModel, context),
            c => _footerResolver.Resolve(context).Compose(c, viewModel, context));
    }

    protected static HprpPageFallback ReportPageFallback => new()
    {
        Top = ReportPageLayout.MarginTopMm,
        Right = ReportPageLayout.MarginHorizontalMm,
        Bottom = ReportPageLayout.MarginBottomMm,
        Left = ReportPageLayout.MarginHorizontalMm,
        SpacingMm = 6,
    };

    protected abstract void ComposeContent(IContainer container, TViewModel viewModel, PdfReportContext context);

    protected virtual void PrepareContext(PdfReportContext context, TViewModel viewModel) { }
}

using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Application;

public sealed class ReportPreviewService : IReportPreviewService
{
    private readonly IBrandingResolver _brandingResolver;
    private readonly IReportSignatureResolver _signatureResolver;
    private readonly IReportPreviewRendererFactory _rendererFactory;
    private readonly ReportRequestPipeline _requestPipeline;

    public ReportPreviewService(
        IBrandingResolver brandingResolver,
        IReportSignatureResolver signatureResolver,
        IReportPreviewRendererFactory rendererFactory,
        ReportRequestPipeline requestPipeline)
    {
        _brandingResolver = brandingResolver;
        _signatureResolver = signatureResolver;
        _rendererFactory = rendererFactory;
        _requestPipeline = requestPipeline;
    }

    public async Task<ReportDocument> PreviewAsync(GeneratePdfRequest request, CancellationToken cancellationToken)
    {
        // Preview must not enforce fully-signed — drafts should still render.
        request = await _requestPipeline.PrepareAsync(request, cancellationToken);

        var layoutProfile = HemosheetLayoutProfileReader.ReadLayoutProfile(request.Data) ?? "Default";

        // ThaiUr uses PDF-as-preview (DOM planner does not mirror that composer).
        // Skip branding/signature context build — FE will call generate next (cached report-data).
        if (HemosheetLayoutProfileReader.IsThaiUr(request.Data))
        {
            return BuildThaiUrPdfModeDocument(request, layoutProfile);
        }

        var context = await ReportPipeline.BuildContextAsync(
            request,
            _brandingResolver,
            _signatureResolver,
            cancellationToken);

        var renderer = _rendererFactory.Create(request.ReportTemplateId);
        var document = await renderer.RenderPreviewAsync(context, cancellationToken);
        return WithPreviewMode(document, "dom", layoutProfile);
    }

    private static ReportDocument BuildThaiUrPdfModeDocument(GeneratePdfRequest request, string layoutProfile)
    {
        ReportTemplates.TryGetDefinition(request.ReportTemplateId, out var templateDefinition);
        var title = templateDefinition?.DisplayName ?? request.ReportTemplateId;

        return new ReportDocument
        {
            Meta = new ReportDocumentMeta
            {
                TemplateId = request.ReportTemplateId,
                Title = title,
                PageSize = "A4",
                GeneratedAt = DateTime.UtcNow.ToString("o"),
                PreviewMode = "pdf",
                LayoutProfile = layoutProfile,
            },
            Branding = new ReportBranding(),
            Header = new ReportHeaderBlock(),
            Pages = [],
            Footer = new ReportFooterBlock(),
        };
    }

    private static ReportDocument WithPreviewMode(ReportDocument document, string previewMode, string? layoutProfile)
    {
        return new ReportDocument
        {
            Meta = new ReportDocumentMeta
            {
                TemplateId = document.Meta.TemplateId,
                Title = document.Meta.Title,
                PageSize = document.Meta.PageSize,
                GeneratedAt = document.Meta.GeneratedAt,
                PreviewMode = previewMode,
                LayoutProfile = layoutProfile ?? document.Meta.LayoutProfile,
            },
            Branding = document.Branding,
            Header = document.Header,
            Pages = document.Pages,
            Footer = document.Footer,
        };
    }
}

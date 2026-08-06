using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Sections.Preview;

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

        // Dense hemosheet forms (Default CICM + ThaiUr purple) have no DOM planner mirror.
        if (UsesHemosheetFormPdfPreview(request, layoutProfile))
        {
            return BuildHemosheetFormPdfModeDocument(request, layoutProfile);
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

    private static bool UsesHemosheetFormPdfPreview(GeneratePdfRequest request, string layoutProfileName)
    {
        if (!ClinicalReportCatalog.IsHemodialysisRecord(request.ReportTemplateId))
            return false;

        if (!Enum.TryParse(layoutProfileName, ignoreCase: true, out HemosheetLayoutProfile profile))
            profile = HemosheetLayoutProfile.Default;

        return ClinicalReportLayoutResolver.UsesHemosheetForm(request.ReportTemplateId, profile);
    }

    private static ReportDocument BuildHemosheetFormPdfModeDocument(GeneratePdfRequest request, string layoutProfile)
    {
        var title = ResolveDisplayName(request.ReportTemplateId);

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

    private static string ResolveDisplayName(string reportTemplateId)
    {
        if (ClinicalReportCatalog.TryGetDefinition(reportTemplateId, out var clinical))
            return clinical!.DisplayName;
        return reportTemplateId;
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

using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hemosheet;

public sealed class HemosheetSectionRendererRegistry
{
    private readonly IReadOnlyDictionary<HemosheetSectionId, IHemosheetSectionRenderer> _renderers;

    public HemosheetSectionRendererRegistry(IEnumerable<IHemosheetSectionRenderer> renderers)
    {
        _renderers = renderers.ToDictionary(r => r.SectionId);
    }

    public void ComposePdf(
        IContainer container,
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        if (!_renderers.TryGetValue(plan.SectionId, out var renderer))
        {
            return;
        }

        renderer.ComposePdf(container, plan, viewModel, context);
    }

    public IReadOnlyList<ReportBlock> MapToPreview(
        HemosheetSectionPlan plan,
        HemosheetReportViewModel viewModel,
        PdfReportContext context)
    {
        if (!_renderers.TryGetValue(plan.SectionId, out var renderer))
        {
            return [];
        }

        return renderer.MapToPreview(plan, viewModel, context);
    }
}

using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical05ProgressNoteChecklistSmokeTests
{
    static Clinical05ProgressNoteChecklistSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task DataProvider_DeserializesTrustedPayload()
    {
        var root = HprpTestAssets.TemplatesRoot();
        var sample = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.ProgressNoteChecklist);
        Assert.True(sample.HasValue);

        var provider = new Clinical05ProgressNoteChecklistDataProvider();
        var model = await provider.GetDataAsync(
            new PdfReportContext
            {
                ReportTemplateId = ClinicalReportCatalog.ProgressNoteChecklist,
                TenantCode = "local",
                Data = sample!.Value.Clone(),
            },
            CancellationToken.None);

        var vm = Assert.IsType<Clinical05ProgressNoteChecklistReportViewModel>(model);
        Assert.Equal(12, vm.Columns.Count);
        Assert.Equal(7, vm.ChecklistItems.Count);
        Assert.Equal("DOC-PROG-NOTE-RP-001", vm.ReportCode);
    }

    [Fact]
    public async Task Renderer_ProducesPdfBytes()
    {
        var root = HprpTestAssets.TemplatesRoot();
        var sample = HprpStudioSamplePayloads.TryLoad(root, ClinicalReportCatalog.ProgressNoteChecklist);
        Assert.True(sample.HasValue);

        var renderer = new Clinical05ProgressNoteChecklistReportRenderer(
            new Clinical05ProgressNoteChecklistDataProvider(),
            new Clinical05ProgressNoteChecklistComposer(),
            new QuestPdfRenderer());

        var bytes = await renderer.RenderReportAsync(
            new PdfReportContext
            {
                ReportTemplateId = ClinicalReportCatalog.ProgressNoteChecklist,
                TenantCode = "local",
                Metadata = new ReportMetadata { Title = Clinical05ProgressNoteChecklistDataProvider.ReportTitle },
                Data = sample!.Value.Clone(),
            },
            CancellationToken.None);

        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
    }
}

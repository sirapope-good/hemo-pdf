using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Layouts.Template04_Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.ThaiUr;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

/// <summary>
/// Studio preview requires <c>reports/{id}/sample.json</c> for every clinical pack template.
/// </summary>
public class StudioSamplePayloadsSmokeTests
{
    static StudioSamplePayloadsSmokeTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static IEnumerable<object[]> ClinicalTemplateIds() =>
        ClinicalReportCatalog.All.Select(d => new object[] { d.Id });

    [Theory]
    [MemberData(nameof(ClinicalTemplateIds))]
    public void SampleJson_ExistsForEveryClinicalTemplate(string templateId)
    {
        var root = HprpTestAssets.TemplatesRoot();
        var sample = HprpStudioSamplePayloads.TryLoad(root, templateId);
        Assert.True(sample.HasValue, $"Missing sample.json for {templateId}");
        Assert.Equal(JsonValueKind.Object, sample!.Value.ValueKind);
        Assert.Contains(templateId, HprpStudioSamplePayloads.KnownTemplateIds);
    }

    [Theory]
    [MemberData(nameof(ClinicalTemplateIds))]
    public async Task SampleJson_RendersPdfBytes(string templateId)
    {
        var root = HprpTestAssets.TemplatesRoot();
        var sample = ClinicalReportCatalog.IsHemodialysisRecord(templateId)
            ? HprpStudioSamplePayloads.TryLoad(root, templateId, "thaiur")
            : HprpStudioSamplePayloads.TryLoad(root, templateId);
        Assert.True(sample.HasValue);

        var store = new FileHprpTemplateStore(Options.Create(new HprpTemplateOptions
        {
            RootPath = root,
            PackagesRootPath = Path.Combine(root, "_no-packages"),
        }));

        var layoutVariant = ClinicalReportCatalog.IsHemodialysisRecord(templateId) ? "thaiur" : null;
        var context = new PdfReportContext
        {
            ReportTemplateId = templateId,
            TenantCode = "local",
            Metadata = new ReportMetadata
            {
                Title = ClinicalReportCatalog.TryGetDefinition(templateId, out var def)
                    ? def!.DisplayName
                    : templateId,
            },
            Data = sample!.Value.Clone(),
            LayoutPackage = store.TryGetCached("local", templateId, layoutVariant),
        };

        var bytes = await RenderAsync(templateId, store, context);
        Assert.True(bytes.Length > 100, templateId);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    private static Task<byte[]> RenderAsync(
        string templateId,
        FileHprpTemplateStore store,
        PdfReportContext context)
    {
        var engine = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var quest = new QuestPdfRenderer();

        if (string.Equals(engine, ClinicalReportCatalog.HctEpo, StringComparison.OrdinalIgnoreCase))
        {
            return new Clinical01HctEpoReportRenderer(
                new Clinical01HctEpoDataProvider(),
                new Clinical01HctEpoComposer(store),
                quest).RenderReportAsync(context, CancellationToken.None);
        }

        if (string.Equals(engine, ClinicalReportCatalog.EpoDrug, StringComparison.OrdinalIgnoreCase))
        {
            return new Clinical02EpoDrugReportRenderer(
                new Clinical02EpoDrugDataProvider(),
                new Clinical02EpoDrugComposer(store),
                quest).RenderReportAsync(context, CancellationToken.None);
        }

        if (string.Equals(engine, ClinicalReportCatalog.ProgressNote, StringComparison.OrdinalIgnoreCase))
        {
            return new Clinical05ProgressNoteReportRenderer(
                new Clinical05ProgressNoteDataProvider(),
                new Clinical05ProgressNoteComposer(store),
                quest).RenderReportAsync(context, CancellationToken.None);
        }

        if (ClinicalReportCatalog.IsConsentReport(engine))
        {
            return new ConsentReportRenderer(
                new ConsentReportDataProvider(),
                new ConsentReportComposer(),
                quest).RenderReportAsync(context, CancellationToken.None);
        }

        if (ClinicalReportCatalog.IsHemodialysisRecord(engine))
        {
            return RenderHemosheetThaiUrAsync(context, quest);
        }

        var composer = new ClinicalDefaultComposer(
            new FixedSectionResolver<IReportHeaderSection>(new EmptyHeaderSection()),
            new FixedSectionResolver<IReportFooterSection>(new EmptyFooterSection()));

        return new ClinicalDefaultReportRenderer(
            new ClinicalDefaultDataProvider(store),
            composer,
            quest).RenderReportAsync(context, CancellationToken.None);
    }

    private static async Task<byte[]> RenderHemosheetThaiUrAsync(
        PdfReportContext context,
        QuestPdfRenderer quest)
    {
        var package = context.LayoutPackage
            ?? throw new InvalidOperationException("clinical-03 package required");
        var model = await new HemosheetDataProvider().GetDataAsync(context, CancellationToken.None);
        var vm = (Hemo.Pdf.Core.Models.Hemosheet.HemosheetReportViewModel)model;
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        var form = new ThaiUrHemosheetForm();
        var layout = new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Content = c => form.Compose(c, vm, context, package),
            Footer = null,
        };
        return await quest.RenderAsync(layout, CancellationToken.None);
    }

    private sealed class FixedSectionResolver<T>(T section) : ISectionResolver<T>
        where T : notnull
    {
        public T Resolve(PdfReportContext context) => section;
    }

    private sealed class EmptyHeaderSection : IReportHeaderSection
    {
        public void Compose(IContainer container, object data, PdfReportContext context) { }
    }

    private sealed class EmptyFooterSection : IReportFooterSection
    {
        public void Compose(IContainer container, object data, PdfReportContext context) { }
    }
}

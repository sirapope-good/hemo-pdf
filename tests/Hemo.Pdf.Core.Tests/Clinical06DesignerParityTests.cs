using System.Text.Json;
using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Core.Tests;

public class Clinical06DesignerParityTests
{
    static Clinical06DesignerParityTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Clinical06DesignerPackage_ValidatesAndRendersPdf()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var package = LoadClinical06Package(templatesRoot);

        var validation = HprpValidator.Validate(package);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.Equal(HprpLayoutModes.Designer, package.Manifest.LayoutMode);
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.DataGrid, StringComparison.OrdinalIgnoreCase)
                && e.Chrome?.RowHeightMm is > 0);

        var sample = HprpStudioSamplePayloads.TryLoad(templatesRoot, ClinicalReportCatalog.Medication);
        Assert.NotNull(sample);

        Assert.True(sample.Value.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array);
        Assert.True(rowsEl.GetArrayLength() >= 22 + 2, "sample should pad medication slots then Nurse/signer footer");
        var lastTwo = rowsEl.EnumerateArray().TakeLast(2).Select(r => r[0].GetString()).ToArray();
        Assert.Equal("Hemodialysis Nurse", lastTwo[0]);
        Assert.True(
            lastTwo[1] is "Nephrologist" or "Pharmacist",
            "second signer label should be Nephrologist or Pharmacist");
        Assert.True(sample.Value.TryGetProperty("reviewNote", out var reviewEl));
        Assert.StartsWith("**Review Med", reviewEl.GetString());
        Assert.Contains(
            package.Layout.Elements,
            e => string.Equals(e.Type, HprpDesignerElementTypes.BoxText, StringComparison.OrdinalIgnoreCase)
                && (e.Bind?.Contains("reviewNote", StringComparison.OrdinalIgnoreCase) == true
                    || (e.Text?.Contains("Review Med", StringComparison.OrdinalIgnoreCase) == true)));

        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templatesRoot,
            PackagesRootPath = Path.Combine(templatesRoot, "_no-packages"),
        });
        var store = new FileHprpTemplateStore(options);
        var presetStore = new HprpTablePresetStore(options);
        var presets = new HprpTablePresetCatalog(presetStore);
        var headerStore = new HprpHeaderPresetStore(options);
        var headers = new HprpHeaderPresetCatalog(headerStore);
        var renderer = CreateDesignerRenderer(store, presets, headers);

        var context = new PdfReportContext
        {
            ReportTemplateId = ClinicalReportCatalog.Medication,
            TenantCode = "local",
            Metadata = new ReportMetadata { Title = package.Manifest.DisplayName },
            Data = sample.Value.Clone(),
            LayoutPackage = package,
        };

        var bytes = await renderer.RenderReportAsync(context, CancellationToken.None);
        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public async Task PackClinical06_WritesPackageFile()
    {
        var templatesRoot = HprpTestAssets.TemplatesRoot();
        var packages = FindRepoPackagesRoot(templatesRoot);

        Directory.CreateDirectory(packages);
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templatesRoot,
            PackagesRootPath = packages,
            PackagesWritePath = packages,
            EnableHprpStudioWrite = true,
        });
        var store = new FileHprpTemplateStore(options);
        var pack = new HprpPackService(options, store);
        var packed = await pack.PackTemplateIdAsync(ClinicalReportCatalog.Medication);
        Assert.Single(packed);
        Assert.True(File.Exists(packed[0].OutputPath));
        Assert.EndsWith("clinical-06-medication.hprp", packed[0].OutputPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoPackagesRoot(string templatesRoot)
    {
        var dir = new DirectoryInfo(templatesRoot);
        DirectoryInfo? binFallback = null;
        while (dir is not null)
        {
            var packages = Path.Combine(dir.FullName, "packages");
            var hasClinical01 = File.Exists(Path.Combine(packages, "clinical-01-hct-epo.hprp"));
            var hasClinical07 = File.Exists(Path.Combine(packages, "clinical-07-lab.hprp"));
            if (hasClinical01 || hasClinical07)
            {
                var isUnderBin = packages.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase);
                if (!isUnderBin)
                    return packages;
                binFallback ??= new DirectoryInfo(packages);
            }

            dir = dir.Parent;
        }

        if (binFallback is not null)
            return binFallback.FullName;

        throw new DirectoryNotFoundException("repo packages/ folder not found from " + templatesRoot);
    }

    private static HprpPackage LoadClinical06Package(string templatesRoot)
    {
        var dir = Path.Combine(templatesRoot, "reports", ClinicalReportCatalog.Medication);
        return new HprpPackage
        {
            Manifest = JsonSerializer.Deserialize<HprpManifest>(
                File.ReadAllText(Path.Combine(dir, HprpPackageReader.ManifestFileName)),
                HprpJson.Options)!,
            Layout = JsonSerializer.Deserialize<HprpLayout>(
                File.ReadAllText(Path.Combine(dir, HprpPackageReader.LayoutFileName)),
                HprpJson.Options)!,
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["th"] = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(Path.Combine(dir, "labels.th.json")),
                    HprpJson.Options)!,
            },
        };
    }

    private static ClinicalDefaultReportRenderer CreateDesignerRenderer(
        FileHprpTemplateStore store,
        HprpTablePresetCatalog presets,
        HprpHeaderPresetCatalog headers)
    {
        var composer = new ClinicalDefaultComposer(
            new FixedSectionResolver<IReportHeaderSection>(new EmptyHeaderSection()),
            new FixedSectionResolver<IReportFooterSection>(new EmptyFooterSection()));

        return new ClinicalDefaultReportRenderer(
            new ClinicalDefaultDataProvider(store, new Clinical01HctEpoDataProvider(), presets, headers),
            composer,
            new QuestPdfRenderer());
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

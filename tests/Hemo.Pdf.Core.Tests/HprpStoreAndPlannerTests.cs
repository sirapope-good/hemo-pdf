using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts;
using Hemo.Pdf.Layouts.Hemosheet;
using Hemo.Pdf.Layouts.Preview.Generic;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Core.Tests;

public class HprpStoreAndPlannerTests
{
    [Fact]
    public void FileStore_LoadsAllClinicalDefaults()
    {
        var store = CreateStore();
        Assert.Equal(16, store.ListDefaultManifests().Count);
        Assert.NotNull(store.TryGetCached("local", ClinicalReportCatalog.Lab));
        Assert.NotNull(store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord));
        Assert.False(store.HasTenantOverride("local", ClinicalReportCatalog.Lab));
    }

    [Fact]
    public async Task FileStore_TenantOverride_WinsOverDefault()
    {
        var source = TemplatesRoot();
        var root = Path.Combine(Path.GetTempPath(), "hprp-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(source, root);
        var store = new FileHprpTemplateStore(Options.Create(new HprpTemplateOptions { RootPath = root }));

        var original = store.TryGetCached("tenant-demo-a", ClinicalReportCatalog.Lab)!;
        var overlay = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.Lab,
                DisplayName = "Lab (tenant)",
                DataAdapter = HprpDataAdapterIds.FlattenDto,
            },
            Layout = original.Layout,
            LabelsByLanguage = original.LabelsByLanguage,
        };

        using var zip = new MemoryStream();
        await HprpPackageReader.WriteZipAsync(overlay, zip, CancellationToken.None);
        zip.Position = 0;
        await store.SaveTenantOverrideAsync("tenant-demo-a", ClinicalReportCatalog.Lab, zip);

        var loaded = store.TryGetCached("tenant-demo-a", ClinicalReportCatalog.Lab);
        Assert.Equal("Lab (tenant)", loaded!.Manifest.DisplayName);
        Assert.True(store.HasTenantOverride("tenant-demo-a", ClinicalReportCatalog.Lab));
        Assert.Equal("Laboratory Record", store.TryGetCached("other", ClinicalReportCatalog.Lab)!.Manifest.DisplayName);
    }

    [Fact]
    public void Planner_FromHprp_MatchesBuiltinForHdAv()
    {
        var store = CreateStore();
        var builtin = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry());
        var fromFile = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry(), store, null);

        var vm = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel { Diagnosis = "CKD stage 5" },
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new HemosheetAssessmentItemViewModel { Name = "pain", Checked = true }],
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = HemosheetLayoutProfile.Default,
                Features = new Dictionary<string, bool>
                {
                    ["showAvPanel"] = true,
                    ["showCathPanel"] = false,
                    ["showHdfColumns"] = false,
                },
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel(),
                },
            },
        };

        var expected = builtin.Plan(vm).Select(p => (p.SectionId, p.Variant)).ToList();
        var actual = fromFile.Plan(vm).Select(p => (p.SectionId, p.Variant)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PreviewFactory_ScaffoldUsesHprpRenderer()
    {
        Assert.Equal(
            typeof(HprpReportPreviewRenderer),
            TemplateReportPreviewRendererFactory.ResolveRendererType(ClinicalReportCatalog.Lab));
        Assert.Equal(
            "HemosheetReportPreviewRenderer",
            TemplateReportPreviewRendererFactory.ResolveRendererType(ClinicalReportCatalog.HemodialysisRecord).Name);
    }

    private static FileHprpTemplateStore CreateStore() =>
        new(Options.Create(new HprpTemplateOptions { RootPath = TemplatesRoot() }));

    private static string TemplatesRoot()
    {
        var rooted = Path.Combine(AppContext.BaseDirectory, "assets", "templates");
        if (Directory.Exists(rooted))
            return rooted;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "templates"));
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, target));
        }

        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var dest = file.Replace(source, target);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}

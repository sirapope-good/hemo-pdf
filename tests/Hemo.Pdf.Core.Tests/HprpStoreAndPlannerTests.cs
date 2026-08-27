using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts;
using Hemo.Pdf.Layouts.Clinical;
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
        var manifests = store.ListDefaultManifests();
        foreach (var definition in ClinicalReportCatalog.All)
            Assert.Contains(manifests, m => string.Equals(m.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(store.TryGetCached("local", ClinicalReportCatalog.Lab));
        Assert.NotNull(store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord));
        Assert.False(store.HasTenantOverride("local", ClinicalReportCatalog.Lab));
    }

    [Fact]
    public void FileStore_LooksUpHemosheetByVariant()
    {
        var store = CreateStore();
        var def = store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord, "default");
        var rama = store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord, "rama");
        var thaiur = store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord, "thaiur");

        Assert.Equal("default", def!.Manifest.Variant);
        Assert.Equal(HprpLayoutKinds.DefaultForm, def.Manifest.LayoutKind);
        Assert.Equal("rama", rama!.Manifest.Variant);
        Assert.Equal(HprpLayoutKinds.UniquePlanner, rama.Manifest.LayoutKind);
        Assert.Equal("thaiur", thaiur!.Manifest.Variant);
        Assert.Equal(HprpLayoutKinds.ThaiUrForm, thaiur.Manifest.LayoutKind);
        Assert.Contains(rama.Layout.Sections, s => s.Widget == HprpWidgetIds.HemosheetConsent);
        Assert.DoesNotContain(thaiur.Layout.Sections, s => s.Widget == HprpWidgetIds.HemosheetConsent);
    }

    [Fact]
    public void FileStore_ListLayoutProfiles_ReturnsHemosheetVariants()
    {
        var store = CreateStore();
        var profiles = store.ListLayoutProfiles(HprpManifestUi.RoleHemosheetLayoutProfile);
        Assert.Equal(3, profiles.Count);
        Assert.Contains(profiles, m => m.Variant == "default" && m.LayoutProfile == "Default");
        Assert.Contains(profiles, m => m.Variant == "rama" && m.Ui?.ProfileLabel == "RAMA");
        Assert.Contains(profiles, m => m.Variant == "thaiur" && m.Ui?.ProfileLabel == "Thai UR");
    }

    [Fact]
    public async Task FileStore_TenantOverride_IsDisabled()
    {
        var store = CreateStore();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.SaveTenantOverrideAsync("local", ClinicalReportCatalog.Lab, new MemoryStream()));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.DeleteTenantOverrideAsync("local", ClinicalReportCatalog.Lab));
    }

    [Fact]
    public void FileStore_ReloadsWhenLayoutChanges()
    {
        var source = HprpTestAssets.TemplatesRoot();
        var root = Path.Combine(Path.GetTempPath(), "hprp-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(source, root);
        var store = new FileHprpTemplateStore(Options.Create(new HprpTemplateOptions { RootPath = root, PackagesRootPath = Path.Combine(root, "_no-packages") }));

        var original = store.TryGetCached("local", ClinicalReportCatalog.Lab);
        Assert.Equal("Laboratory Record", original!.Manifest.DisplayName);

        var manifestPath = Path.Combine(
            HprpTemplatePaths.ReportsRoot(root),
            ClinicalReportCatalog.Lab,
            HprpPackageReader.ManifestFileName);
        var json = File.ReadAllText(manifestPath).Replace("Laboratory Record", "Lab (reloaded)");
        File.WriteAllText(manifestPath, json);
        File.SetLastWriteTimeUtc(manifestPath, DateTime.UtcNow.AddSeconds(2));

        var reloaded = store.TryGetCached("local", ClinicalReportCatalog.Lab);
        Assert.Equal("Lab (reloaded)", reloaded!.Manifest.DisplayName);
    }

    [Fact]
    public void Planner_FromHprp_MatchesBuiltinForHdAv()
    {
        var store = CreateStore();
        var builtin = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry());
        var fromFile = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry(), store, null);

        var vm = CreateVm(HemosheetLayoutProfile.Default, showAv: true);

        var expected = builtin.Plan(vm).Select(p => (p.SectionId, p.Variant)).ToList();
        var actual = fromFile.Plan(vm).Select(p => (p.SectionId, p.Variant)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Planner_FromHprp_ThaiUrUsesAssessmentReNotPreRe()
    {
        var store = CreateStore();
        var package = store.TryGetCached("local", ClinicalReportCatalog.HemodialysisRecord, "thaiur");
        Assert.NotNull(package);
        Assert.NotEmpty(package!.Layout.Sections);

        var planner = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry(), store, null);
        var vm = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel { Diagnosis = "CKD stage 5" },
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new HemosheetAssessmentItemViewModel { Name = "pain", Checked = true }],
                Re = [new HemosheetAssessmentItemViewModel { Name = "re-check", Checked = true }],
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = HemosheetLayoutProfile.ThaiUr,
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

        var ids = planner.Plan(vm).Select(p => p.SectionId).ToList();
        Assert.Contains(HemosheetSectionId.AssessmentRe, ids);
        Assert.DoesNotContain(HemosheetSectionId.AssessmentPreRe, ids);
    }

    [Fact]
    public void Planner_FromHprp_RamaIncludesConsentWhenFeatureOn()
    {
        var store = CreateStore();
        var planner = new HemosheetLayoutPlanner(new HemosheetLayoutProfileRegistry(), store, null);
        var vm = CreateVm(HemosheetLayoutProfile.Rama, showAv: true, consent: true);

        var ids = planner.Plan(vm).Select(p => p.SectionId).ToList();
        Assert.Contains(HemosheetSectionId.Consent, ids);
    }

    [Fact]
    public void LayoutResolver_UsesManifestLayoutKind()
    {
        var kind = ClinicalReportLayoutResolver.Resolve(
            ClinicalReportCatalog.HemodialysisRecord,
            HemosheetLayoutProfile.Default,
            new HprpManifest { LayoutKind = HprpLayoutKinds.ThaiUrForm });
        Assert.Equal(ClinicalLayoutKind.ThaiUrForm, kind);
    }

    private static HemosheetReportViewModel CreateVm(
        HemosheetLayoutProfile profile,
        bool showAv,
        bool consent = false) =>
        new()
        {
            Patient = new HemosheetPatientViewModel { Diagnosis = "CKD stage 5" },
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new HemosheetAssessmentItemViewModel { Name = "pain", Checked = true }],
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = profile,
                Features = new Dictionary<string, bool>
                {
                    ["showAvPanel"] = showAv,
                    ["showCathPanel"] = !showAv,
                    ["showHdfColumns"] = false,
                    ["showConsentBlock"] = consent,
                },
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel(),
                },
            },
        };

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
        new(Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = Path.Combine(Path.GetTempPath(), "hprp-none-" + Guid.NewGuid().ToString("N")),
        }));

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

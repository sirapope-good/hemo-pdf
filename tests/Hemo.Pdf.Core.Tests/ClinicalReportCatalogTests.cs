using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Clinical;

namespace Hemo.Pdf.Core.Tests;

public class ClinicalReportCatalogTests
{
    [Fact]
    public void All_ContainsSixteenReports()
    {
        Assert.Equal(16, ClinicalReportCatalog.All.Count);
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, true)]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, true)]
    [InlineData(ClinicalReportCatalog.LegacyDocumentTypeAlias, true)]
    [InlineData(ClinicalReportCatalog.HctEpo, true)]
    [InlineData("unknown", false)]
    public void IsKnown_RecognizesPackAndAliases(string id, bool expected)
    {
        Assert.Equal(expected, ClinicalReportCatalog.IsKnown(id));
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, true)]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, true)]
    [InlineData(ClinicalReportCatalog.Lab, false)]
    public void IsHemodialysisRecord_OnlyNumber03(string id, bool expected)
    {
        Assert.Equal(expected, ClinicalReportCatalog.IsHemodialysisRecord(id));
    }

    [Fact]
    public void ResolveEngineTemplateId_CollapsesAliasesToClinical03()
    {
        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            ClinicalReportCatalog.ResolveEngineTemplateId(ClinicalReportCatalog.HemodialysisRecord));
        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            ClinicalReportCatalog.ResolveEngineTemplateId(ClinicalReportCatalog.LegacyEngineAlias));
        Assert.Equal(
            ClinicalReportCatalog.HemodialysisRecord,
            ClinicalReportCatalog.ResolveEngineTemplateId(ClinicalReportCatalog.LegacyDocumentTypeAlias));
        Assert.Equal(
            ClinicalReportCatalog.Lab,
            ClinicalReportCatalog.ResolveEngineTemplateId(ClinicalReportCatalog.Lab));
    }

    [Fact]
    public void DefaultScaffoldIds_ExcludesDedicatedEngines()
    {
        var ids = ClinicalReportCatalog.DefaultScaffoldIds.ToList();
        Assert.Equal(10, ids.Count);
        Assert.DoesNotContain(ClinicalReportCatalog.HemodialysisRecord, ids);
        Assert.DoesNotContain(ClinicalReportCatalog.HctEpo, ids);
        Assert.DoesNotContain(ClinicalReportCatalog.EpoDrug, ids);
        Assert.DoesNotContain(ClinicalReportCatalog.ProgressNote, ids);
        Assert.DoesNotContain(ClinicalReportCatalog.ConsentTh, ids);
        Assert.DoesNotContain(ClinicalReportCatalog.ConsentEn, ids);
    }

    [Theory]
    [InlineData(ClinicalReportCatalog.HctEpo, true)]
    [InlineData(ClinicalReportCatalog.EpoDrug, true)]
    [InlineData(ClinicalReportCatalog.ProgressNote, true)]
    [InlineData("medicine-preparation-round", true)]
    [InlineData(ClinicalReportCatalog.ConsentTh, true)]
    [InlineData(ClinicalReportCatalog.ConsentEn, true)]
    [InlineData(ClinicalReportCatalog.Lab, false)]
    public void UsesDensePdfPreview_DedicatedEngines(string id, bool expected)
    {
        Assert.Equal(expected, ClinicalReportCatalog.UsesDensePdfPreview(id));
    }
}

public class ClinicalReportLayoutResolverTests
{
    [Theory]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, HemosheetLayoutProfile.Default, ClinicalLayoutKind.DefaultForm)]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, HemosheetLayoutProfile.ThaiUr, ClinicalLayoutKind.ThaiUrForm)]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, HemosheetLayoutProfile.ThaiUr, ClinicalLayoutKind.ThaiUrForm)]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, HemosheetLayoutProfile.Default, ClinicalLayoutKind.DefaultForm)]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, HemosheetLayoutProfile.Rama, ClinicalLayoutKind.UniquePlanner)]
    [InlineData(ClinicalReportCatalog.Lab, HemosheetLayoutProfile.ThaiUr, ClinicalLayoutKind.UniquePlanner)]
    [InlineData(ClinicalReportCatalog.Lab, HemosheetLayoutProfile.Default, ClinicalLayoutKind.UniquePlanner)]
    public void Resolve_DefaultAndThaiUrUseDenseForms_RamaUsesPlanner(
        string reportId,
        HemosheetLayoutProfile profile,
        ClinicalLayoutKind expected)
    {
        Assert.Equal(expected, ClinicalReportLayoutResolver.Resolve(reportId, profile));
    }
}

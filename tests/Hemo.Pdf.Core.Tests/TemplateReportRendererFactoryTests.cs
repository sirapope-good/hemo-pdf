using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Clinical.Clinical08_Consent;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class TemplateReportRendererFactoryTests
{
    [Theory]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, nameof(HemosheetReportRenderer))]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, nameof(HemosheetReportRenderer))]
    [InlineData(ClinicalReportCatalog.HctEpo, nameof(Clinical01HctEpoReportRenderer))]
    [InlineData(ClinicalReportCatalog.EpoDrug, "Clinical02EpoDrugReportRenderer")]
    [InlineData(ClinicalReportCatalog.ProgressNote, "Clinical05ProgressNoteReportRenderer")]
    [InlineData("medicine-preparation-round", "MedicinePreparationRoundReportRenderer")]
    [InlineData(ClinicalReportCatalog.Lab, nameof(ClinicalDefaultReportRenderer))]
    [InlineData(ClinicalReportCatalog.ConsentEn, nameof(ConsentReportRenderer))]
    [InlineData(ClinicalReportCatalog.ConsentTh, nameof(ConsentReportRenderer))]
    public void ResolveRendererType_ClinicalPack(string templateId, string expectedTypeName)
    {
        var type = TemplateReportRendererFactory.ResolveRendererType(templateId);
        Assert.Equal(expectedTypeName, type.Name);
    }

    [Fact]
    public void CreateRegistrations_IncludesClinicalIds()
    {
        var regs = TemplateReportRendererFactory.CreateRegistrations();
        Assert.Contains(regs, r => r.ReportTemplateId == ClinicalReportCatalog.Lab);
        Assert.Contains(regs, r => r.ReportTemplateId == ClinicalReportCatalog.HemodialysisRecord);
        Assert.Contains(regs, r => r.ReportTemplateId == ClinicalReportCatalog.HctEpo);
        Assert.Contains(regs, r => r.ReportTemplateId == ClinicalReportCatalog.EpoDrug);
        Assert.Contains(regs, r => r.ReportTemplateId == ClinicalReportCatalog.ProgressNote);
        Assert.Contains(regs, r => r.ReportTemplateId == "medicine-preparation-round");
        Assert.Equal(
            typeof(HemosheetReportRenderer),
            regs.First(r => r.ReportTemplateId == ClinicalReportCatalog.HemodialysisRecord).RendererType);
        Assert.Equal(
            typeof(Clinical01HctEpoReportRenderer),
            regs.First(r => r.ReportTemplateId == ClinicalReportCatalog.HctEpo).RendererType);
        Assert.Equal(
            typeof(ClinicalDefaultReportRenderer),
            regs.First(r => r.ReportTemplateId == ClinicalReportCatalog.Lab).RendererType);
    }
}

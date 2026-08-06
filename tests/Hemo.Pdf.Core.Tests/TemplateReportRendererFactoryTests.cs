using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Layouts;
using Hemo.Pdf.Layouts.Clinical;
using Hemo.Pdf.Layouts.Template04_Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class TemplateReportRendererFactoryTests
{
    [Theory]
    [InlineData(ClinicalReportCatalog.LegacyEngineAlias, nameof(HemosheetReportRenderer))]
    [InlineData(ClinicalReportCatalog.HemodialysisRecord, nameof(HemosheetReportRenderer))]
    [InlineData(ClinicalReportCatalog.Lab, nameof(ClinicalDefaultReportRenderer))]
    [InlineData(ClinicalReportCatalog.ConsentEn, nameof(ClinicalDefaultReportRenderer))]
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
        Assert.Equal(
            typeof(HemosheetReportRenderer),
            regs.First(r => r.ReportTemplateId == ClinicalReportCatalog.HemodialysisRecord).RendererType);
    }
}

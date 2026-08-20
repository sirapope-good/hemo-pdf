using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Core.Tests;

public class ReportDataFetchRegistryTests
{
    [Theory]
    [InlineData(ClinicalReportCatalog.HctEpo, ReportDataFetchKind.Clinical01HctEpoPatientYear)]
    [InlineData(ClinicalReportCatalog.EpoDrug, ReportDataFetchKind.Clinical02EpoDrugPatientMonthMed)]
    [InlineData(ClinicalReportCatalog.ProgressNote, ReportDataFetchKind.Clinical05ProgressNotePatientMonth)]
    [InlineData(ClinicalReportCatalog.Lab, ReportDataFetchKind.Clinical07LabPatient)]
    [InlineData(ClinicalReportCatalog.ConsentTh, ReportDataFetchKind.ConsentPatientTemplateOrRecord)]
    [InlineData("unknown-template", ReportDataFetchKind.HemosheetRecordOrTemplate)]
    public void Resolve_MapsClinicalTemplates(string templateId, ReportDataFetchKind expected)
    {
        Assert.Equal(expected, ReportDataFetchRegistry.Resolve(templateId));
    }
}

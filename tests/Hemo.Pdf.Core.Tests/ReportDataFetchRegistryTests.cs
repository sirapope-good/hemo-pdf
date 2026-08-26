using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Core.Tests;

public class ReportDataFetchRegistryTests
{
    [Theory]
    [InlineData(ClinicalReportCatalog.HctEpo, ReportDataFetchKind.Clinical01HctEpoPatientYear)]
    [InlineData(ClinicalReportCatalog.EpoDrug, ReportDataFetchKind.Clinical02EpoDrugPatientMonthMed)]
    [InlineData(ClinicalReportCatalog.ProgressNote, ReportDataFetchKind.Clinical05ProgressNotePatientMonth)]
    [InlineData(ClinicalReportCatalog.ProgressNoteChecklist, ReportDataFetchKind.Clinical05ProgressNoteChecklistPatientMonthRange)]
    [InlineData(ClinicalReportCatalog.Lab, ReportDataFetchKind.Clinical07LabPatient)]
    [InlineData(ClinicalReportCatalog.Prescription, ReportDataFetchKind.FormPatientByAdapter)]
    [InlineData(ClinicalReportCatalog.Medication, ReportDataFetchKind.FormPatientByAdapter)]
    [InlineData(ClinicalReportCatalog.PatientData, ReportDataFetchKind.FormPatientByAdapter)]
    [InlineData(ClinicalReportCatalog.MarMonth, ReportDataFetchKind.FormPatientByAdapter)]
    [InlineData(ClinicalReportCatalog.ConsentTh, ReportDataFetchKind.ConsentPatientTemplateOrRecord)]
    [InlineData("unknown-template", ReportDataFetchKind.HemosheetRecordOrTemplate)]
    public void Resolve_MapsClinicalTemplates(string templateId, ReportDataFetchKind expected)
    {
        Assert.Equal(expected, ReportDataFetchRegistry.Resolve(templateId));
    }

    [Fact]
    public void Resolve_NewFormPackage_UsesConventionPath()
    {
        var manifest = new Hemo.Pdf.Core.Hprp.HprpManifest
        {
            Id = "clinical-99-test-report",
            DisplayName = "Test",
            DataAdapter = "flatten-dto",
            Ui = new Hemo.Pdf.Core.Hprp.HprpManifestUi
            {
                EntryMode = "patient",
                ReportDataPath = "api/Patients/{patientId}/reports/{templateId}/report-data",
            },
        };

        Assert.Equal(
            ReportDataFetchKind.FormPatientByAdapter,
            ReportDataFetchRegistry.Resolve(manifest.Id, manifest));
    }
}

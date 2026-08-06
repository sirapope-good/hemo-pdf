using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Sections.Tests;

public class HemosheetPreviewMapperTests
{
    [Fact]
    public void MapSubHeaderBar_IncludesDiagnosisAndAllergies()
    {
        var vm = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel
            {
                Diagnosis = "CKD stage 5",
                Allergies = ["heparin"],
            },
        };

        var block = HemosheetPreviewMappers.MapSubHeaderBar(vm);

        Assert.NotNull(block);
        Assert.Equal(2, block!.Fields.Count);
        Assert.Contains(block.Fields, f => f.Label == "Diagnosis" && f.Value == "CKD stage 5");
    }

    [Fact]
    public void MapTopLayoutRow_ReturnsSectionRowWithColumnStack()
    {
        var vm = new HemosheetReportViewModel
        {
            PreVital = new HemosheetVitalSignViewModel { Bps = 120, Bpd = 80 },
            Dehydration = new HemosheetDehydrationViewModel { PreWeight = 68f },
            DialysisPrescription = new HemosheetPrescriptionViewModel { Mode = "HD" },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool>(),
            },
        };

        var block = HemosheetPreviewMappers.MapTopLayoutRow(vm, vm.LayoutContext.Features);

        Assert.NotNull(block);
        Assert.IsType<SectionRowReportBlock>(block);
        Assert.Equal(2, ((SectionRowReportBlock)block!).Blocks.Count);
        Assert.IsType<ColumnStackReportBlock>(((SectionRowReportBlock)block).Blocks[0]);
    }

    [Fact]
    public void ResolveDialysisColumnWeights_NoteColumn_IsWiderThanNumericColumns()
    {
        var columns = new[] { "เวลา", "BP", "HR", "หมายเหตุ" };
        var weights = HemosheetPreviewMappers.ResolveDialysisColumnWeights(columns);

        var noteWeight = weights[^1];
        var bpWeight = weights[1];

        Assert.True(noteWeight > bpWeight);
        Assert.Equal(3.5f, noteWeight);
    }

    [Fact]
    public void HeaderPreviewMapper_IncludesPatientMetadataLines()
    {
        var vm = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel
            {
                Name = "ทดสอบ ผู้ป่วย",
                Hn = "HN-001",
                BirthDate = new DateTime(1965, 4, 12),
                Sex = "ชาย",
            },
            Unit = new HemosheetUnitViewModel { FullName = "หน่วยไตเทียม 1" },
            TreatmentNo = 7,
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool>(),
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    FixedLines = new HemosheetFixedLinesViewModel(),
                },
            },
        };

        var context = new PdfReportContext
        {
            ReportTemplateId = "clinical-03-hemodialysis-record",
            TenantCode = "tenant-demo-a",
            Metadata = new ReportMetadata { Title = "Hemodialysis Record", ReportCode = "HS-001" },
            Branding = new CustomerBrandingProfile { DisplayName = "Demo Hospital" },
        };

        var header = HemosheetHeaderPreviewMapper.Map(vm, context);

        Assert.Equal("Hemodialysis Record", header.Title);
        Assert.Contains(header.MetadataLines, line => line.Contains("ทดสอบ ผู้ป่วย"));
        Assert.Contains(header.MetadataLines, line => line.Contains("HN-001"));
        Assert.Contains(header.MetadataLines, line => line.Contains("Treatment No."));
    }
}

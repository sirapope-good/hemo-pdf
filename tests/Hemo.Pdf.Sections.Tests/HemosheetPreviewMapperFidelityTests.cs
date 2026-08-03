using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetPreviewMapperFidelityTests
{
    [Fact]
    public void MapPatient_IncludesClinicalFields_InTwoColumns()
    {
        var vm = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel
            {
                Name = "สมชาย",
                Hn = "HN-1",
                IdentityNumber = "1",
                BirthDate = new DateTime(1968, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                Age = 58,
                Sex = "Male",
                DoctorName = "นพ. วิชัย",
                Allergies = ["heparin"],
                Coverage = "UC",
                Diagnosis = "CKD 5",
                Underlying = "DM",
            },
            Unit = new HemosheetUnitViewModel { Id = 1, FullName = "Unit A" },
        };

        var block = HemosheetPreviewMappers.MapPatient(vm);

        Assert.NotNull(block);
        Assert.Equal(2, block!.Columns.Count);
        var flat = block.Columns.SelectMany(c => c).ToDictionary(x => x.Label, x => x.Value);
        Assert.Equal("นพ. วิชัย", flat["แพทย์"]);
        Assert.Equal("heparin", flat["แพ้ยา"]);
        Assert.Equal("UC", flat["สิทธิ์"]);
        Assert.Equal("58", flat["อายุ"]);
        Assert.Equal("CKD 5", flat["Diagnosis"]);
    }

    [Fact]
    public void MapDehydration_UsesThreeColumns_AndShowsFlushWhenPresent()
    {
        var vm = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel
            {
                PreWeight = 68.5f,
                FlushNss = 100,
            },
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                Features = new Dictionary<string, bool>(),
            },
        };

        var block = HemosheetPreviewMappers.MapDehydration(vm, vm.LayoutContext.Features);

        Assert.NotNull(block);
        Assert.Equal(3, block!.Columns);
        Assert.Contains(block.Fields, f => f.Label == "Flush NSS");
    }

    [Fact]
    public void MapPrescription_FallsBackToAcFields_AndSpansNote()
    {
        var features = new Dictionary<string, bool>();
        var vm = new HemosheetReportViewModel
        {
            DialysisPrescription = new HemosheetPrescriptionViewModel
            {
                Mode = "HD",
                DurationHours = 4,
                Anticoagulant = "Heparin",
                InitialAmount = 2000,
                Note = "watch BP",
            },
        };

        var block = HemosheetPreviewMappers.MapPrescription(vm, features);

        Assert.NotNull(block);
        Assert.Equal(3, block!.Columns);
        Assert.Contains(block.Fields, f => f.Label == "Anticoagulant" && f.Value == "Heparin");
        Assert.Contains(block.Fields, f => f.Label == "Duration (hr)");
        var note = Assert.Single(block.Fields, f => f.Label == "Note");
        Assert.Equal(3, note.ColumnSpan);
    }

    [Fact]
    public void MapVascularAccess_AvIncludesRouteAndNeedles()
    {
        var vm = new HemosheetReportViewModel
        {
            DialysisPrescription = new HemosheetPrescriptionViewModel
            {
                BloodAccessRoute = "AV Fistula L",
            },
            AvShunt = new HemosheetAvShuntViewModel
            {
                ShuntSite = "L forearm",
                ANeedleSize = 16,
                VNeedleSize = 16,
                CatheterType = 0,
            },
        };

        var block = HemosheetPreviewMappers.MapVascularAccess(vm, "av-fistula");

        Assert.NotNull(block);
        Assert.Contains(block!.Rows, r => r.Label == "Route" && r.Value == "AV Fistula L");
        Assert.Contains(block.Rows, r => r.Label == "A Needle");
    }
}

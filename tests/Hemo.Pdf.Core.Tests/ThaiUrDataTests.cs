using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Template04_Hemosheet.ThaiUr;

namespace Hemo.Pdf.Core.Tests;

public class ThaiUrDataTests
{
    [Fact]
    public void Num_FormatsOrDash()
    {
        Assert.Equal("12.5", ThaiUrData.Num(12.5f));
        Assert.Equal("3", ThaiUrData.Num(3));
        Assert.Equal("-", ThaiUrData.Num((float?)null));
        Assert.Equal("-", ThaiUrData.Num((int?)null));
    }

    [Fact]
    public void Kg_RequiresPositive()
    {
        Assert.Equal("60 Kg", ThaiUrData.Kg(60f));
        Assert.Equal("-", ThaiUrData.Kg(0f));
        Assert.Equal("-", ThaiUrData.Kg(null));
    }

    [Fact]
    public void WeightGain_UsesPreMinusDry()
    {
        var vm = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel { PreWeight = 62 },
            DialysisPrescription = new HemosheetPrescriptionViewModel { DryWeight = 60 },
        };

        Assert.Equal("2 Kg", ThaiUrData.WeightGain(vm));
    }

    [Fact]
    public void WeightGain_MissingWeights_IsNA()
    {
        var vm = new HemosheetReportViewModel();
        Assert.Equal("N/A", ThaiUrData.WeightGain(vm));
    }

    [Fact]
    public void ExtraFluidMl_TreatsSmallValuesAsLiters()
    {
        var liters = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel { ExtraFluid = 1.5f },
        };
        Assert.Equal(1500f, ThaiUrData.ExtraFluidMl(liters));

        var ml = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel { ExtraFluid = 250f },
        };
        Assert.Equal(250f, ThaiUrData.ExtraFluidMl(ml));
    }

    [Fact]
    public void NssMl_PrefersSessionTotal_ThenRowSum()
    {
        var fromTotal = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel { FlushNssTotal = 100 },
            DialysisRecords = [new() { Nss = 50 }],
        };
        Assert.Equal(100f, ThaiUrData.NssMl(fromTotal));

        var fromRows = new HemosheetReportViewModel
        {
            DialysisRecords = [new() { Nss = 30 }, new() { Nss = 20 }],
        };
        Assert.Equal(50f, ThaiUrData.NssMl(fromRows));
    }

    [Fact]
    public void NetFluidBalanceMl_SubtractsNssAndExtra()
    {
        var vm = new HemosheetReportViewModel
        {
            Dehydration = new HemosheetDehydrationViewModel
            {
                TotalUf = 2.5f,
                FlushNssTotal = 200,
                ExtraFluid = 100,
            },
        };

        Assert.Equal(2200f, ThaiUrData.NetFluidBalanceMl(vm));
    }

    [Fact]
    public void Allergies_DefaultThaiCopyWhenEmpty()
    {
        var empty = new HemosheetReportViewModel();
        Assert.Equal("ไม่มีแพ้ยา", ThaiUrData.Allergies(empty));

        var listed = new HemosheetReportViewModel
        {
            Patient = new HemosheetPatientViewModel { Allergies = ["Penicillin", "NSAID"] },
        };
        Assert.Equal("Penicillin, NSAID", ThaiUrData.Allergies(listed));
    }

    [Fact]
    public void PreState_MatchesAnyKey_CaseInsensitive()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new() { Name = "vas:av:thrill", Checked = true }],
            },
        };

        Assert.True(ThaiUrData.PreState(vm, "thrill", "vas:av:thrill"));
        Assert.Null(ThaiUrData.PreState(vm, "bruit"));
    }

    [Fact]
    public void Checked_MatchesLabelOnPostOrOther()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Post = [new() { Name = "complication.hypotension", Text = "Hypotension", Checked = true }],
                Other = [new() { Name = "health.nutrition", Text = "Nutrition", Checked = false }],
            },
        };

        Assert.True(ThaiUrData.Checked(vm, "Hypotension"));
        Assert.False(ThaiUrData.Checked(vm, "Nutrition"));
        Assert.False(ThaiUrData.Checked(vm, "Fever"));
    }

    [Fact]
    public void Bp_FormatsSysDia()
    {
        Assert.Equal("120/80", ThaiUrData.Bp(120, 80));
        Assert.Equal("-/-", ThaiUrData.Bp(null, null));
    }
}

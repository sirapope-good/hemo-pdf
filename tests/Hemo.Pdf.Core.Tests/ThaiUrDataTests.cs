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


    [Theory]
    [InlineData("pale", "pale", true)]
    [InlineData("vas:edema", "edema", true)]
    [InlineData("vas:av:thrill", "thrill", true)]
    [InlineData("inflame", "inflame", true)]
    [InlineData("crep", "crep", true)]
    [InlineData("inf", "inf", true)]
    [InlineData("head", "headache", false)]
    [InlineData("bruit", "thrill", false)]
    public void NameMatches_SupportsSeedAndAliasKeys(string name, string key, bool expected)
    {
        Assert.Equal(expected, ThaiUrData.NameMatches(name, key));
    }

    [Fact]
    public void PreState_MatchesThaiUrShortKeysAndSeedNames()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre =
                [
                    new() { Name = "head", Checked = true },
                    new() { Name = "inflame", Checked = false },
                    new() { Name = "pale", Checked = true },
                    new() { Name = "crep", Checked = true },
                    new() { Name = "dys", Checked = false },
                    new() { Name = "pbleed", Checked = true },
                    new() { Name = "inf", Checked = true },
                ],
            },
        };

        Assert.True(ThaiUrData.PreState(vm, "head", "headache"));
        Assert.False(ThaiUrData.PreState(vm, "inflame", "inflamation"));
        Assert.True(ThaiUrData.PreState(vm, "pale"));
        Assert.True(ThaiUrData.PreState(vm, "crep", "crepitatic"));
        Assert.False(ThaiUrData.PreState(vm, "dyspnea", "dys"));
        Assert.True(ThaiUrData.PreState(vm, "pbleed", "bleeding"));
        Assert.True(ThaiUrData.PreState(vm, "inf", "inflame"));
        Assert.Null(ThaiUrData.PreState(vm, "urine"));
    }

    [Fact]
    public void PreOrOtherState_ReadsUrineFromOtherBucket()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Other = [new() { Name = "urine", Checked = true }],
            },
        };

        Assert.Null(ThaiUrData.PreState(vm, "urine"));
        Assert.True(ThaiUrData.PreOrOtherState(vm, "urine"));
    }

    [Fact]
    public void PreOrOtherText_ReadsPainScoreFromPre_AndUrineMlFromOther()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Pre = [new() { Name = "pain", Checked = true, Text = "3" }],
                Other = [new() { Name = "urine", Checked = true, Text = "800" }],
            },
        };

        Assert.Equal("3", ThaiUrData.PreOrOtherText(vm, "pain"));
        Assert.True(ThaiUrData.PreOrOtherState(vm, "pain"));
        Assert.Equal("800", ThaiUrData.PreOrOtherText(vm, "urine"));
    }

    [Fact]
    public void Checked_MatchesSelectedOptionsDisplayNames_FromLiveBeShape()
    {
        var vm = new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Post =
                [
                    new()
                    {
                        Name = "complication",
                        Checked = true,
                        SelectedOptions = ["Hypo-tension", "Fever", "No complication"],
                    },
                    new()
                    {
                        Name = "nursing",
                        Checked = true,
                        SelectedOptions = ["Monitor vital signs", "Psychological support"],
                    },
                    new()
                    {
                        Name = "health",
                        Checked = true,
                        SelectedOptions = ["Personal hygiene", "KT preparation"],
                    },
                ],
            },
        };

        Assert.True(ThaiUrData.Checked(vm, "Hypotension"));
        Assert.True(ThaiUrData.Checked(vm, "Fever"));
        Assert.True(ThaiUrData.Checked(vm, "No complication"));
        Assert.False(ThaiUrData.Checked(vm, "Hypertension"));
        Assert.True(ThaiUrData.Checked(vm, "Monitor V/S"));
        Assert.True(ThaiUrData.Checked(vm, "Phycho support"));
        Assert.True(ThaiUrData.Checked(vm, "Personal hygine"));
        Assert.True(ThaiUrData.Checked(vm, "KT"));
        Assert.False(ThaiUrData.Checked(vm, "Nutrition"));
    }

    [Fact]
    public void TokenEquals_NormalizesHypoTensionSpelling()
    {
        Assert.True(ThaiUrData.TokenEquals("Hypo-tension", "Hypotension"));
        Assert.True(ThaiUrData.TokenEquals("Hypo-tension", "Hypo-tension")); // exact
        Assert.True(ThaiUrData.Checked(new HemosheetReportViewModel
        {
            Assessments = new HemosheetAssessmentsViewModel
            {
                Post = [new() { Name = "complication", Checked = true, SelectedOptions = ["Nausea/Vomit"] }],
            },
        }, "Nausea / Vomitting")); // alias table bridges spelling
        Assert.False(ThaiUrData.TokenEquals("Monitor vital signs", "Monitor V/S"));
    }


    [Fact]
    public void NursingPlanRows_MapsFocusInterventionEvaluation_AndExpandsNewlines()
    {
        var vm = new HemosheetReportViewModel
        {
            ProgressNotes =
            [
                new()
                {
                    Focus = "Diagnosis A",
                    I = "Intervene 1\nIntervene 2",
                    E = "Outcome 1",
                },
                new()
                {
                    Focus = "Diagnosis B",
                    I = "Intervene B",
                    E = "Outcome B1\nOutcome B2",
                },
            ],
        };

        var rows = ThaiUrData.NursingPlanRows(vm);
        Assert.Equal(4, rows.Count);
        Assert.Equal(("Diagnosis A", "Intervene 1", "Outcome 1"), rows[0]);
        Assert.Equal(("", "Intervene 2", ""), rows[1]);
        Assert.Equal(("Diagnosis B", "Intervene B", "Outcome B1"), rows[2]);
        Assert.Equal(("", "", "Outcome B2"), rows[3]);
    }

    [Fact]
    public void NursingPlanRows_EmptyNotes_YieldsBlankPlaceholderRow()
    {
        var rows = ThaiUrData.NursingPlanRows(new HemosheetReportViewModel());
        Assert.Single(rows);
        Assert.Equal(("", "", ""), rows[0]);
    }
}

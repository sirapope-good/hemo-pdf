using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Layouts.Hprp;

namespace Hemo.Pdf.Core.Tests;

public class HprpLayoutPlanTests
{
    [Fact]
    public void ResolveWidgetOrder_NullPackage_ReturnsDefault()
    {
        var order = HprpLayoutPlan.ResolveWidgetOrder(
            null,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        Assert.Equal(HprpClinicalWidgetSets.Clinical01DefaultOrder, order);
    }

    [Fact]
    public void ResolveWidgetOrder_ReadsHeaderThenBody_Clinical01()
    {
        var package = Package(
            "clinical-01-hct-epo",
            HprpDataAdapterIds.Clinical01HctEpo,
            header: HprpWidgetIds.ThaiUrHeader,
            body:
            [
                HprpWidgetIds.ClinicalHctEpoCopay,
                HprpWidgetIds.ClinicalHctEpoAnnualTable,
            ]);

        var order = HprpLayoutPlan.ResolveWidgetOrder(
            package,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        Assert.Equal(
            new[]
            {
                HprpWidgetIds.ThaiUrHeader,
                HprpWidgetIds.ClinicalHctEpoCopay,
                HprpWidgetIds.ClinicalHctEpoAnnualTable,
            },
            order);
    }

    [Fact]
    public void ResolveWidgetOrder_IgnoresWidgetsOutsideAllowList()
    {
        var package = Package(
            "clinical-02-epo-drug",
            HprpDataAdapterIds.Clinical02EpoDrug,
            header: HprpWidgetIds.ThaiUrHeader,
            body:
            [
                HprpWidgetIds.ClinicalSoapTable,
                HprpWidgetIds.ClinicalEpoDrugTable,
                HprpWidgetIds.ClinicalHctEpoCopay,
            ]);

        var order = HprpLayoutPlan.ResolveWidgetOrder(
            package,
            HprpClinicalWidgetSets.Clinical02DefaultOrder,
            HprpClinicalWidgetSets.Clinical02Allowed);

        Assert.Equal(
            new[]
            {
                HprpWidgetIds.ThaiUrHeader,
                HprpWidgetIds.ClinicalEpoDrugTable,
                HprpWidgetIds.ClinicalHctEpoCopay,
            },
            order);
    }

    [Fact]
    public void ResolveBodyWidgets_Clinical05_SoapOnly()
    {
        var package = Package(
            "clinical-05-progress-note",
            HprpDataAdapterIds.Clinical05ProgressNote,
            header: HprpWidgetIds.ThaiUrHeader,
            body: [HprpWidgetIds.ClinicalSoapTable]);

        var body = HprpLayoutPlan.ResolveBodyWidgets(
            package,
            HprpClinicalWidgetSets.Clinical05BodyDefault,
            HprpClinicalWidgetSets.Clinical05BodyAllowed);

        Assert.Equal(HprpClinicalWidgetSets.Clinical05BodyDefault, body);
    }

    [Fact]
    public void Bind_RecognizesDenseClinicalWidgets()
    {
        var package = Package(
            "clinical-01-hct-epo",
            HprpDataAdapterIds.Clinical01HctEpo,
            header: null,
            body:
            [
                HprpWidgetIds.ClinicalHctEpoAnnualTable,
                HprpWidgetIds.ClinicalHctEpoCopay,
                HprpWidgetIds.ClinicalEpoDrugTable,
                HprpWidgetIds.ClinicalSoapTable,
                HprpWidgetIds.ClinicalConsentNarrative,
            ]);

        var blocks = HprpBinder.Bind(package, data: null);
        Assert.Equal(5, blocks.Count);
        Assert.All(blocks, b => Assert.IsType<Hemo.Pdf.Core.Models.Preview.TextReportBlock>(b));
    }

    [Fact]
    public void ResolveNodes_KeepsGenericBlocksBetweenWidgets()
    {
        var package = Package(
            "clinical-01-hct-epo",
            HprpDataAdapterIds.Clinical01HctEpo,
            header: HprpWidgetIds.ThaiUrHeader,
            body:
            [
                HprpWidgetIds.ClinicalHctEpoAnnualTable,
                HprpWidgetIds.ClinicalHctEpoCopay,
            ]);
        package = WithBody(package,
        [
            new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoAnnualTable },
            new HprpLayoutNode { Type = "key-value-table" },
            new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoCopay },
        ]);

        var nodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        Assert.Equal(4, nodes.Count);
        Assert.Equal(HprpWidgetIds.ThaiUrHeader, nodes[0].Widget);
        Assert.Equal(HprpWidgetIds.ClinicalHctEpoAnnualTable, nodes[1].Widget);
        Assert.Equal("key-value-table", nodes[2].Type);
        Assert.Equal(HprpWidgetIds.ClinicalHctEpoCopay, nodes[3].Widget);
    }

    [Fact]
    public void ResolveNodes_DropsDisallowedWidgetEvenIfTypeIsSet()
    {
        var package = Package(
            "clinical-01-hct-epo",
            HprpDataAdapterIds.Clinical01HctEpo,
            header: HprpWidgetIds.ThaiUrHeader,
            body: [HprpWidgetIds.ClinicalHctEpoAnnualTable]);
        package = WithBody(package,
        [
            new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalSoapTable, Type = "text" },
            new HprpLayoutNode { Widget = HprpWidgetIds.ClinicalHctEpoCopay },
        ]);

        var nodes = HprpLayoutPlan.ResolveNodes(
            package,
            HprpClinicalWidgetSets.Clinical01DefaultOrder,
            HprpClinicalWidgetSets.Clinical01Allowed);

        Assert.Equal(2, nodes.Count);
        Assert.Equal(HprpWidgetIds.ThaiUrHeader, nodes[0].Widget);
        Assert.Equal(HprpWidgetIds.ClinicalHctEpoCopay, nodes[1].Widget);
    }

    private static HprpPackage WithBody(HprpPackage package, IReadOnlyList<HprpLayoutNode> body) =>
        new()
        {
            Manifest = package.Manifest,
            Layout = new HprpLayout
            {
                Header = package.Layout.Header,
                Body = body,
            },
            LabelsByLanguage = package.LabelsByLanguage,
        };

    private static HprpPackage Package(
        string id,
        string adapter,
        string? header,
        IReadOnlyList<string> body) =>
        new()
        {
            Manifest = new HprpManifest
            {
                Id = id,
                DisplayName = id,
                DataAdapter = adapter,
            },
            Layout = new HprpLayout
            {
                Header = header is null ? null : new HprpLayoutNode { Widget = header },
                Body = body.Select(w => new HprpLayoutNode { Widget = w }).ToList(),
            },
            LabelsByLanguage = new Dictionary<string, IReadOnlyDictionary<string, string>>(),
        };
}

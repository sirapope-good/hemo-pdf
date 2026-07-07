using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Core.Tests;

public class HemosheetReportViewModelDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };

    [Fact]
    public void Deserialize_LayoutContext_AcceptsNumericEnumsFromHemopro()
    {
        const string json = """
            {
              "layoutContext": {
                "layoutProfile": 0,
                "dialysisMode": "HD",
                "vascularAccess": 1,
                "features": { "showAvPanel": true }
              }
            }
            """;

        var vm = JsonSerializer.Deserialize<HemosheetReportViewModel>(json, JsonOptions);

        Assert.NotNull(vm);
        Assert.Equal(HemosheetLayoutProfile.Default, vm!.LayoutContext.LayoutProfile);
        Assert.Equal(VascularAccessKind.AvFistula, vm.LayoutContext.VascularAccess);
        Assert.True(vm.LayoutContext.Features["showAvPanel"]);
    }

    [Fact]
    public void Deserialize_LayoutContext_AcceptsStringEnumsFromMockJson()
    {
        const string json = """
            {
              "layoutContext": {
                "layoutProfile": "Rama",
                "dialysisMode": "HD",
                "vascularAccess": "PermCath"
              }
            }
            """;

        var vm = JsonSerializer.Deserialize<HemosheetReportViewModel>(json, JsonOptions);

        Assert.NotNull(vm);
        Assert.Equal(HemosheetLayoutProfile.Rama, vm!.LayoutContext.LayoutProfile);
        Assert.Equal(VascularAccessKind.PermCath, vm.LayoutContext.VascularAccess);
    }
}

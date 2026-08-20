using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Template04_Hemosheet;

public sealed class HemosheetDataProvider : IReportDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };

    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Data is not JsonElement json || json.ValueKind != JsonValueKind.Object)
        {
            return Task.FromResult<object>(new HemosheetReportViewModel());
        }

        var viewModel = JsonSerializer.Deserialize<HemosheetReportViewModel>(json.GetRawText(), JsonOptions)
            ?? new HemosheetReportViewModel();

        if (viewModel.LayoutContext.Features.Count == 0)
        {
            viewModel.LayoutContext = HemosheetLayoutContextFallback.Build(viewModel);
        }

        return Task.FromResult<object>(viewModel);
    }
}

internal static class HemosheetLayoutContextFallback
{
    public static HemosheetLayoutContextViewModel Build(HemosheetReportViewModel vm)
    {
        var isHdf = string.Equals(vm.DialysisPrescription.Mode, "HDF", StringComparison.OrdinalIgnoreCase);
        var catheterType = vm.AvShunt.CatheterType;
        var route = vm.DialysisPrescription.BloodAccessRoute ?? "";
        var isAv = catheterType is < 2 || route.Contains("AV", StringComparison.OrdinalIgnoreCase);
        var profile = ResolveProfile(vm);

        return new HemosheetLayoutContextViewModel
        {
            LayoutProfile = profile,
            DialysisMode = isHdf ? "HDF" : "HD",
            VascularAccess = isAv ? VascularAccessKind.AvFistula : VascularAccessKind.PermCath,
            ReportSettings = vm.LayoutContext.ReportSettings,
            Features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["showHdfColumns"] = isHdf,
                ["showAvPanel"] = isAv,
                ["showCathPanel"] = !isAv,
                ["showAcFields"] = !vm.IsAcNotUsed,
                ["showProgressNote"] = vm.ProgressNotes.Count > 0,
                ["showNurseInShift"] = !string.IsNullOrWhiteSpace(vm.NursesInShift),
                ["showNurseInShiftNonPn"] = profile == HemosheetLayoutProfile.ThaiUr,
                ["showConsentBlock"] = profile == HemosheetLayoutProfile.Rama && vm.IsConsent,
            },
        };
    }

    private static HemosheetLayoutProfile ResolveProfile(HemosheetReportViewModel vm)
    {
        if (vm.LayoutContext.LayoutProfile != HemosheetLayoutProfile.Default)
            return vm.LayoutContext.LayoutProfile;

        var settings = vm.LayoutContext.ReportSettings;
        if (!string.IsNullOrWhiteSpace(settings.LayoutProfile)
            && Enum.TryParse(settings.LayoutProfile, ignoreCase: true, out HemosheetLayoutProfile fromSetting))
        {
            return fromSetting;
        }

        var template = settings.HemosheetTemplate ?? "";
        if (template.Contains("ThaiUR", StringComparison.OrdinalIgnoreCase))
            return HemosheetLayoutProfile.ThaiUr;
        if (template.Contains("RAMA", StringComparison.OrdinalIgnoreCase))
            return HemosheetLayoutProfile.Rama;

        return HemosheetLayoutProfile.Default;
    }
}

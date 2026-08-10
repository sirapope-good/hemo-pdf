using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Deserializes trusted clinical-01 report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical01HctEpoDataProvider : IReportDataProvider
{
    /// <summary>Keep in sync with back <c>HctEpoLayoutConstants.ReportTitle</c> / catalog DisplayName.</summary>
    public static string ReportTitle => ClinicalReportCatalog.TryGetDefinition(ClinicalReportCatalog.HctEpo, out var def)
        ? def!.DisplayName
        : "Hemodialysis Review Hct and EPO";

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
            throw new InvalidOperationException(
                $"{ClinicalReportCatalog.HctEpo} requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<HctEpoWireModel>(json.GetRawText(), JsonOptions)
            ?? new HctEpoWireModel();

        var header = ApplyClinical01HeaderSettings(wire.Header ?? new HemosheetReportViewModel());

        var months = HctEpoMonthLabels.EnsureTwelve(
            (wire.Months ?? [])
                .Select(ToMonthRow)
                .ToList());

        var result = new HctEpoReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            Year = wire.Year,
            Header = header,
            Months = months,
            // API is source of truth; local CreateDefault is fallback only.
            CoPayCriteria = wire.CoPayCriteria ?? HctEpoCoPayCriteria.CreateDefault(),
        };

        return Task.FromResult<object>(result);
    }

    private static HemosheetReportViewModel ApplyClinical01HeaderSettings(HemosheetReportViewModel source) =>
        new()
        {
            LogoBase64 = source.LogoBase64,
            Patient = source.Patient,
            Unit = source.Unit,
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = source.LayoutContext.LayoutProfile,
                DialysisMode = source.LayoutContext.DialysisMode,
                VascularAccess = source.LayoutContext.VascularAccess,
                Features = source.LayoutContext.Features,
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    ShowDateAndHdNo = false,
                    ShowHdPerWeek = true,
                    HemosheetTemplate = source.LayoutContext.ReportSettings.HemosheetTemplate,
                    NurseInShiftEnabled = source.LayoutContext.ReportSettings.NurseInShiftEnabled,
                    FixedLines = source.LayoutContext.ReportSettings.FixedLines,
                },
            },
        };

    private static HctEpoMonthRow ToMonthRow(HctEpoMonthWire month)
    {
        var entries = month.Entries?
            .Select(e => new HctEpoMonthEntry
            {
                DayLabel = e.DayLabel,
                Hb = e.Hb,
                Hct = e.Hct,
                LabIsHistorical = e.LabIsHistorical,
                EpoName = e.EpoName,
                FrequencyText = e.FrequencyText,
                InjectionDate = e.InjectionDate,
                Remarks = e.Remarks,
            })
            .Where(HasAnyField)
            .ToList() ?? [];

        return new HctEpoMonthRow
        {
            MonthIndex = month.MonthIndex,
            MonthLabel = string.IsNullOrWhiteSpace(month.MonthLabel)
                ? (month.MonthIndex is >= 1 and <= 12
                    ? HctEpoMonthLabels.ThaiShort[month.MonthIndex - 1]
                    : string.Empty)
                : month.MonthLabel!,
            Entries = entries,
        };
    }

    private static bool HasAnyField(HctEpoMonthEntry e) =>
        !string.IsNullOrWhiteSpace(e.DayLabel)
        || !string.IsNullOrWhiteSpace(e.Hb)
        || !string.IsNullOrWhiteSpace(e.Hct)
        || !string.IsNullOrWhiteSpace(e.EpoName)
        || !string.IsNullOrWhiteSpace(e.FrequencyText)
        || !string.IsNullOrWhiteSpace(e.InjectionDate)
        || !string.IsNullOrWhiteSpace(e.Remarks);

    private sealed class HctEpoWireModel
    {
        public string? Title { get; set; }
        public int Year { get; set; }
        public HemosheetReportViewModel? Header { get; set; }
        public List<HctEpoMonthWire>? Months { get; set; }
        public HctEpoCoPayCriteria? CoPayCriteria { get; set; }
    }

    private sealed class HctEpoMonthWire
    {
        public int MonthIndex { get; set; }
        public string? MonthLabel { get; set; }
        public List<HctEpoEntryWire>? Entries { get; set; }
    }

    private sealed class HctEpoEntryWire
    {
        public string? DayLabel { get; set; }
        public string? Hb { get; set; }
        public string? Hct { get; set; }
        public bool LabIsHistorical { get; set; }
        public string? EpoName { get; set; }
        public string? FrequencyText { get; set; }
        public string? InjectionDate { get; set; }
        public string? Remarks { get; set; }
    }
}

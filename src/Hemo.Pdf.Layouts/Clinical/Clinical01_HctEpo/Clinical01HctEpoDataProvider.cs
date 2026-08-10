using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Deserializes trusted clinical-01 report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical01HctEpoDataProvider : IReportDataProvider
{
    public const string ReportTitle = "Hemodialysis Review Hct & EPO";

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
                "clinical-01-hct-epo requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<HctEpoWireModel>(json.GetRawText(), JsonOptions)
            ?? new HctEpoWireModel();

        var headerSource = wire.Header ?? new HemosheetReportViewModel();
        var header = new HemosheetReportViewModel
        {
            LogoBase64 = headerSource.LogoBase64,
            Patient = headerSource.Patient,
            Unit = headerSource.Unit,
            LayoutContext = new HemosheetLayoutContextViewModel
            {
                LayoutProfile = headerSource.LayoutContext.LayoutProfile,
                DialysisMode = headerSource.LayoutContext.DialysisMode,
                VascularAccess = headerSource.LayoutContext.VascularAccess,
                Features = headerSource.LayoutContext.Features,
                ReportSettings = new HemosheetReportSettingsViewModel
                {
                    ShowDateAndHdNo = false,
                    ShowHdPerWeek = true,
                    HemosheetTemplate = headerSource.LayoutContext.ReportSettings.HemosheetTemplate,
                    NurseInShiftEnabled = headerSource.LayoutContext.ReportSettings.NurseInShiftEnabled,
                    FixedLines = headerSource.LayoutContext.ReportSettings.FixedLines,
                },
            },
        };

        var months = EnsureTwelve(wire.Months)
            .Select(ToMonthRow)
            .ToList();

        var result = new HctEpoReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            Year = wire.Year,
            Header = header,
            Months = months,
            CoPayCriteria = wire.CoPayCriteria ?? HctEpoCoPayCriteria.CreateDefault(),
        };

        return Task.FromResult<object>(result);
    }

    private static HctEpoMonthRow ToMonthRow(HctEpoMonthWire month)
    {
        var entries = month.Entries?.Select(e => new HctEpoMonthEntry
        {
            DayLabel = e.DayLabel,
            Hb = e.Hb,
            Hct = e.Hct,
            LabIsHistorical = e.LabIsHistorical,
            EpoName = e.EpoName,
            FrequencyText = e.FrequencyText,
            InjectionDate = e.InjectionDate,
            Remarks = e.Remarks,
        }).Where(HasAnyField).ToList() ?? [];

        if (entries.Count == 0)
        {
            entries = BuildEntriesFromLegacyFlat(month);
        }

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

    private static List<HctEpoMonthEntry> BuildEntriesFromLegacyFlat(HctEpoMonthWire month)
    {
        var names = SplitLines(month.EpoName);
        var freqs = SplitLines(month.FrequencyText);
        var dates = SplitLines(month.InjectionDate);
        var remarks = SplitLines(month.Remarks);
        var count = new[] { names.Count, freqs.Count, dates.Count, remarks.Count }.Max();

        // Legacy single Hb/Hct on month → first slot only (not historical).
        if (count == 0
            && (string.IsNullOrWhiteSpace(month.Hb) && string.IsNullOrWhiteSpace(month.Hct)))
        {
            return [];
        }

        count = Math.Max(count, 1);
        var list = new List<HctEpoMonthEntry>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(new HctEpoMonthEntry
            {
                DayLabel = null,
                Hb = i == 0 ? month.Hb : null,
                Hct = i == 0 ? month.Hct : null,
                LabIsHistorical = false,
                EpoName = At(names, i),
                FrequencyText = At(freqs, i),
                InjectionDate = At(dates, i),
                Remarks = At(remarks, i),
            });
        }

        return list;
    }

    private static bool HasAnyField(HctEpoMonthEntry e) =>
        !string.IsNullOrWhiteSpace(e.DayLabel)
        || !string.IsNullOrWhiteSpace(e.Hb)
        || !string.IsNullOrWhiteSpace(e.Hct)
        || !string.IsNullOrWhiteSpace(e.EpoName)
        || !string.IsNullOrWhiteSpace(e.FrequencyText)
        || !string.IsNullOrWhiteSpace(e.InjectionDate)
        || !string.IsNullOrWhiteSpace(e.Remarks);

    private static List<string> SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

    private static string? At(IReadOnlyList<string> list, int index) =>
        index < list.Count ? list[index] : null;

    private static IReadOnlyList<HctEpoMonthWire> EnsureTwelve(IReadOnlyList<HctEpoMonthWire>? months)
    {
        var byIndex = (months ?? Array.Empty<HctEpoMonthWire>())
            .Where(m => m.MonthIndex is >= 1 and <= 12)
            .GroupBy(m => m.MonthIndex)
            .ToDictionary(g => g.Key, g => g.First());

        return Enumerable.Range(1, 12)
            .Select(i => byIndex.TryGetValue(i, out var row)
                ? row
                : new HctEpoMonthWire
                {
                    MonthIndex = i,
                    MonthLabel = HctEpoMonthLabels.ThaiShort[i - 1],
                })
            .ToList();
    }

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
        public string? DateLabel { get; set; }
        public string? Hb { get; set; }
        public string? Hct { get; set; }
        public List<HctEpoEntryWire>? Entries { get; set; }

        public string? EpoName { get; set; }
        public string? FrequencyText { get; set; }
        public string? InjectionDate { get; set; }
        public string? Remarks { get; set; }
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

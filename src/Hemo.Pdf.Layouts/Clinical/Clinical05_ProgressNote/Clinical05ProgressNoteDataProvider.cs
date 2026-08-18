using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;

/// <summary>
/// Deserializes trusted clinical-05 report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical05ProgressNoteDataProvider : IReportDataProvider
{
    public static string ReportTitle => ClinicalReportCatalog.TryGetDefinition(ClinicalReportCatalog.ProgressNote, out var def)
        ? def!.DisplayName
        : "Hemodialysis Progress note";

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
                $"{ClinicalReportCatalog.ProgressNote} requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<Clinical05WireModel>(json.GetRawText(), JsonOptions)
            ?? new Clinical05WireModel();

        var result = new Clinical05ProgressNoteReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            MonthKey = wire.MonthKey ?? string.Empty,
            Header = ApplyHeaderSettings(wire.Header ?? new HemosheetReportViewModel()),
            Sessions = (wire.Sessions ?? [])
                .Select(s => new Clinical05SoapSession
                {
                    HemodialysisId = s.HemodialysisId ?? string.Empty,
                    DateLabel = s.DateLabel ?? string.Empty,
                    Subjective = s.Subjective,
                    GeneralAppearance = s.GeneralAppearance,
                    GeneralAppearanceOther = s.GeneralAppearanceOther,
                    Heent = s.Heent,
                    HeentNote = s.HeentNote,
                    Lung = s.Lung,
                    LungNote = s.LungNote,
                    Extremities = s.Extremities,
                    ExtremitiesNote = s.ExtremitiesNote,
                    ObjectiveOther = s.ObjectiveOther,
                    Assessment = s.Assessment,
                    Plan = s.Plan,
                    OrderForOneDay = s.OrderForOneDay,
                    OrderForContinuation = s.OrderForContinuation,
                })
                .ToList(),
        };

        return Task.FromResult<object>(result);
    }

    private static HemosheetReportViewModel ApplyHeaderSettings(HemosheetReportViewModel source) =>
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

    private sealed class Clinical05WireModel
    {
        public string? Title { get; set; }
        public string? MonthKey { get; set; }
        public HemosheetReportViewModel? Header { get; set; }
        public List<Clinical05SessionWire>? Sessions { get; set; }
    }

    private sealed class Clinical05SessionWire
    {
        public string? HemodialysisId { get; set; }
        public string? DateLabel { get; set; }
        public string? Subjective { get; set; }
        public string? GeneralAppearance { get; set; }
        public string? GeneralAppearanceOther { get; set; }
        public string? Heent { get; set; }
        public string? HeentNote { get; set; }
        public string? Lung { get; set; }
        public string? LungNote { get; set; }
        public string? Extremities { get; set; }
        public string? ExtremitiesNote { get; set; }
        public string? ObjectiveOther { get; set; }
        public string? Assessment { get; set; }
        public string? Plan { get; set; }
        public string? OrderForOneDay { get; set; }
        public string? OrderForContinuation { get; set; }
    }
}

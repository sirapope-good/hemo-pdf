using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Layouts.Clinical.Clinical04_Prescription;

/// <summary>
/// Deserializes trusted clinical-04 report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical04PrescriptionDataProvider : IReportDataProvider
{
    public static string ReportTitle => ClinicalReportCatalog.TryGetDefinition(ClinicalReportCatalog.Prescription, out var def)
        ? def!.DisplayName
        : "Hemodialysis Prescription";

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
                $"{ClinicalReportCatalog.Prescription} requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<Clinical04WireModel>(json.GetRawText(), JsonOptions)
            ?? new Clinical04WireModel();

        var result = new Clinical04PrescriptionReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            ReportDate = wire.ReportDate ?? string.Empty,
            OrderDate = wire.OrderDate ?? string.Empty,
            OrderSubtitle = wire.OrderSubtitle ?? string.Empty,
            Header = ApplyHeaderSettings(wire.Header ?? new HemosheetReportViewModel()),
            DialysisFields = (wire.DialysisFields ?? [])
                .Select(f => new LabelValue
                {
                    Label = f.Label ?? string.Empty,
                    Value = f.Value ?? string.Empty,
                    Indent = f.Indent,
                })
                .ToList(),
            MedicinePrescriptionLines = (wire.MedicinePrescriptionLines ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList(),
            MedHistoryLines = (wire.MedHistoryLines ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList(),
            IsSigned = wire.IsSigned,
            DoctorName = wire.DoctorName ?? string.Empty,
            DoctorUpdated = wire.DoctorUpdated ?? string.Empty,
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
                    ShowHdPerWeek = false,
                    HemosheetTemplate = source.LayoutContext.ReportSettings.HemosheetTemplate,
                    NurseInShiftEnabled = source.LayoutContext.ReportSettings.NurseInShiftEnabled,
                    FixedLines = source.LayoutContext.ReportSettings.FixedLines,
                },
            },
        };

    private sealed class Clinical04WireModel
    {
        public string? Title { get; set; }
        public string? ReportDate { get; set; }
        public string? OrderDate { get; set; }
        public string? OrderSubtitle { get; set; }
        public HemosheetReportViewModel? Header { get; set; }
        public List<Clinical04FieldWire>? DialysisFields { get; set; }
        public List<string>? MedicinePrescriptionLines { get; set; }
        public List<string>? MedHistoryLines { get; set; }
        public bool IsSigned { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorUpdated { get; set; }
    }

    private sealed class Clinical04FieldWire
    {
        public string? Label { get; set; }
        public string? Value { get; set; }
        public int Indent { get; set; }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;

/// <summary>
/// Deserializes trusted clinical-02 report-data from Web.Api (via ReportDataResolver).
/// </summary>
public sealed class Clinical02EpoDrugDataProvider : IReportDataProvider
{
    public static string ReportTitle => ClinicalReportCatalog.TryGetDefinition(ClinicalReportCatalog.EpoDrug, out var def)
        ? def!.DisplayName
        : "Erythropoietin Drug Record";

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
                $"{ClinicalReportCatalog.EpoDrug} requires trusted report-data from Web.Api.");
        }

        var wire = JsonSerializer.Deserialize<EpoDrugWireModel>(json.GetRawText(), JsonOptions)
            ?? new EpoDrugWireModel();

        var header = ApplyHeaderSettings(wire.Header ?? new HemosheetReportViewModel());
        var meta = wire.Meta ?? new EpoDrugMetaWire();

        var result = new EpoDrugReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(wire.Title) ? ReportTitle : wire.Title!,
            Header = header,
            Meta = new EpoDrugMeta
            {
                MonthKey = meta.MonthKey ?? string.Empty,
                MonthLabel = meta.MonthLabel ?? string.Empty,
                YearBe = meta.YearBe,
                MedicineId = meta.MedicineId,
                EpoName = meta.EpoName ?? string.Empty,
                NeedlesPerWeek = meta.NeedlesPerWeek ?? string.Empty,
            },
            Rows = (wire.Rows ?? [])
                .Select(r => new EpoDrugInjectionRow
                {
                    DateLabel = r.DateLabel ?? string.Empty,
                    DoseIndex = r.DoseIndex,
                    StickerText = null, // physical sticker area always blank
                    NurseName = r.NurseName ?? string.Empty,
                    Remarks = r.Remarks,
                })
                .ToList(),
            CoPayCriteria = wire.CoPayCriteria ?? HctEpoCoPayCriteria.CreateDefault(),
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

    private sealed class EpoDrugWireModel
    {
        public string? Title { get; set; }
        public HemosheetReportViewModel? Header { get; set; }
        public EpoDrugMetaWire? Meta { get; set; }
        public List<EpoDrugRowWire>? Rows { get; set; }
        public HctEpoCoPayCriteria? CoPayCriteria { get; set; }
    }

    private sealed class EpoDrugMetaWire
    {
        public string? MonthKey { get; set; }
        public string? MonthLabel { get; set; }
        public int YearBe { get; set; }
        public int MedicineId { get; set; }
        public string? EpoName { get; set; }
        public string? NeedlesPerWeek { get; set; }
    }

    private sealed class EpoDrugRowWire
    {
        public string? DateLabel { get; set; }
        public int DoseIndex { get; set; }
        public string? StickerText { get; set; }
        public string? NurseName { get; set; }
        public string? Remarks { get; set; }
    }
}

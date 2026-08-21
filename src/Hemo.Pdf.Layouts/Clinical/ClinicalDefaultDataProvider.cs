using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Clinical;

public sealed class ClinicalDefaultDataProvider : IReportDataProvider
{
    private static readonly JsonSerializerOptions HeaderJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };

    private readonly IHprpTemplateStore? _templates;

    public ClinicalDefaultDataProvider()
        : this(null)
    {
    }

    public ClinicalDefaultDataProvider(IHprpTemplateStore? templates)
    {
        _templates = templates;
    }

    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var package = _templates?.TryGetCached(
            context.TenantCode,
            ClinicalReportCatalog.ResolveEngineTemplateId(context.ReportTemplateId));

        var title = ResolveTitle(context, package);
        if (package is not null && package.Layout.Body.Count > 0)
        {
            var useThaiUrHeader = string.Equals(
                package.Layout.Header?.Widget,
                HprpWidgetIds.ThaiUrHeader,
                StringComparison.OrdinalIgnoreCase);

            return Task.FromResult<object>(new HprpBoundViewModel
            {
                Title = title,
                Subtitle = context.Metadata.Subtitle
                    ?? HprpLabels.Get(package.GetLabels(package.Manifest.Language), "subtitle", ""),
                Blocks = HprpBinder.Bind(package, context.Data, context, package.Manifest.Language),
                SectionHeaderFill = HprpChrome.FirstFileHeaderFillFromLayout(package.Layout),
                UseThaiUrHeader = useThaiUrHeader,
                Header = useThaiUrHeader ? ReadThaiUrHeader(context.Data) : null,
            });
        }

        return Task.FromResult<object>(BuildFallback(context, title));
    }

    private static HemosheetReportViewModel ReadThaiUrHeader(JsonElement? data)
    {
        if (data is not JsonElement json
            || json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty("header", out var headerEl)
            || headerEl.ValueKind != JsonValueKind.Object)
        {
            return ApplyThaiUrHeaderSettings(new HemosheetReportViewModel());
        }

        var parsed = JsonSerializer.Deserialize<HemosheetReportViewModel>(headerEl.GetRawText(), HeaderJson)
            ?? new HemosheetReportViewModel();
        return ApplyThaiUrHeaderSettings(parsed);
    }

    private static HemosheetReportViewModel ApplyThaiUrHeaderSettings(HemosheetReportViewModel source) =>
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

    private static string ResolveTitle(PdfReportContext context, HprpPackage? package)
    {
        var title = context.Metadata.Title;
        if (string.IsNullOrWhiteSpace(title)
            && ClinicalReportCatalog.TryGetDefinition(context.ReportTemplateId, out var definition))
        {
            title = definition!.DisplayName;
        }

        if (string.IsNullOrWhiteSpace(title) && package is not null)
            title = package.Manifest.DisplayName;

        return string.IsNullOrWhiteSpace(title) ? context.ReportTemplateId : title;
    }

    private static HprpBoundViewModel BuildFallback(PdfReportContext context, string title)
    {
        var rows = new List<KeyValuePair<string, string?>>
        {
            new("Report", title),
            new("Layout", "Default (scaffold)"),
            new("Note", "Foundation placeholder — body pixel-parity not implemented yet."),
        };

        if (context.Data is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in json.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    continue;

                rows.Add(new KeyValuePair<string, string?>(property.Name, FormatJsonValue(property.Value)));
            }
        }

        var simple = new SimpleReportViewModel
        {
            Title = title,
            Subtitle = context.Metadata.Subtitle ?? "Clinical report pack — Default structure",
            Rows = rows,
        };

        var blocks = new List<ReportBlock>();
        var keyValue = KeyValueTablePreviewMapper.Map(simple);
        if (keyValue is not null)
            blocks.Add(keyValue);

        return new HprpBoundViewModel
        {
            Title = title,
            Subtitle = simple.Subtitle,
            Blocks = blocks,
        };
    }

    private static string? FormatJsonValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => null,
            _ => value.GetRawText(),
        };
}

using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Preview;

namespace Hemo.Pdf.Layouts.Clinical;

public sealed class ClinicalDefaultDataProvider : IReportDataProvider
{
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
            return Task.FromResult<object>(new HprpBoundViewModel
            {
                Title = title,
                Subtitle = context.Metadata.Subtitle
                    ?? HprpLabels.Get(package.GetLabels(package.Manifest.Language), "subtitle", ""),
                Blocks = HprpBinder.Bind(package, context.Data, context, package.Manifest.Language),
            });
        }

        return Task.FromResult<object>(BuildFallback(context, title));
    }

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

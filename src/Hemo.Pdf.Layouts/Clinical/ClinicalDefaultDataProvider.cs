using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Layouts.Clinical;

/// <summary>
/// Foundation scaffold data for clinical reports other than Hemodialysis Record (#03).
/// </summary>
public sealed class ClinicalDefaultDataProvider : IReportDataProvider
{
    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var title = context.Metadata.Title;
        if (string.IsNullOrWhiteSpace(title)
            && ClinicalReportCatalog.TryGetDefinition(context.ReportTemplateId, out var definition))
        {
            title = definition!.DisplayName;
        }

        if (string.IsNullOrWhiteSpace(title))
            title = context.ReportTemplateId;

        var rows = new List<KeyValuePair<string, string?>>
        {
            new("Report", title),
            new("Layout", "Default (scaffold)"),
            new(
                "Note",
                "Foundation placeholder — body pixel-parity not implemented yet."),
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

        var viewModel = new SimpleReportViewModel
        {
            Title = title,
            Subtitle = context.Metadata.Subtitle ?? "Clinical report pack — Default structure",
            Rows = rows,
        };

        return Task.FromResult<object>(viewModel);
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

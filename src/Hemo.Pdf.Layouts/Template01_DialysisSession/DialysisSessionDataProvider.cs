using System.Text.Json;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Content;

namespace Hemo.Pdf.Layouts.Template01_DialysisSession;

public sealed class DialysisSessionDataProvider : IReportDataProvider
{
    public Task<object> GetDataAsync(PdfReportContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Data is not JsonElement json || json.ValueKind != JsonValueKind.Object)
        {
            return Task.FromResult<object>(new DialysisSessionViewModel());
        }

        var viewModel = new DialysisSessionViewModel
        {
            PatientInfo = new PatientInfoModel
            {
                Name = GetString(json, "patientName", "name"),
                HospitalNumber = GetString(json, "hospitalNumber", "hn"),
                IdentityNumber = GetString(json, "identityNumber", "nid"),
                DateOfBirth = GetString(json, "dateOfBirth", "dob"),
                Gender = GetString(json, "gender"),
                Unit = GetString(json, "unit"),
            },
            SessionRows = ParseKeyValueRows(json, "session"),
            Grid = ParseGrid(json, "vitals"),
        };

        return Task.FromResult<object>(viewModel);
    }

    private static string? GetString(JsonElement json, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (json.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }

        return null;
    }

    private static IReadOnlyList<KeyValuePair<string, string?>> ParseKeyValueRows(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return section.EnumerateObject()
            .Select(p => new KeyValuePair<string, string?>(p.Name, FormatJsonValue(p.Value)))
            .ToList();
    }

    private static DataGridModel? ParseGrid(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var grid) || grid.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var headers = grid.TryGetProperty("columns", out var columns) && columns.ValueKind == JsonValueKind.Array
            ? columns.EnumerateArray().Select(c => c.GetString() ?? "").Where(s => s.Length > 0).ToList()
            : [];

        var rows = grid.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array
            ? rowsEl.EnumerateArray()
                .Select(row => row.ValueKind == JsonValueKind.Array
                    ? row.EnumerateArray().Select(FormatJsonValue).ToList()
                    : (IReadOnlyList<string?>)[])
                .ToList()
            : [];

        if (headers.Count == 0 && rows.Count == 0)
        {
            return null;
        }

        return new DataGridModel
        {
            Title = GetString(grid, "title") ?? "Vitals",
            ColumnHeaders = headers,
            Rows = rows,
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

using System.Text.Json;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Binds <see cref="HprpLayout.Body"/> + DTO JSON into <see cref="ReportBlock"/>s
/// for the existing PDF/preview pipeline.
/// </summary>
public static class HprpBinder
{
    public static IReadOnlyList<ReportBlock> Bind(
        HprpPackage package,
        JsonElement? data,
        PdfReportContext? context = null,
        string? language = null)
    {
        var labels = package.GetLabels(language);
        var blocks = new List<ReportBlock>();
        foreach (var node in package.Layout.Body)
        {
            if (!HprpWhen.MatchesDto(node.When, data))
                continue;

            var block = BindNode(node, data, labels, context);
            if (block is not null)
                blocks.Add(block);
        }

        return blocks;
    }

    private static ReportBlock? BindNode(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        if (!string.IsNullOrWhiteSpace(node.Widget))
        {
            var fromWidget = BindWidget(node, data, labels, context);
            if (fromWidget is not null)
                return fromWidget;
        }

        return BindGeneric(node, data, labels, context);
    }

    /// <summary>
    /// Binds a form <c>type</c> node (<c>text</c>, <c>key-value-table</c>, …).
    /// Dedicated composers use this for extra blocks declared next to dense widgets.
    /// Widget-only nodes return null (pixels stay in C# section drawers).
    /// </summary>
    public static ReportBlock? BindGeneric(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        if (!HprpWhen.MatchesDto(node.When, data))
            return null;

        var type = node.Type?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type) || !HprpWidgetIds.BlockTypes.Contains(type))
            return null;

        return type switch
        {
            "text" => BindText(node, data, labels, context),
            "key-value-table" => BindKeyValue(node, data, labels, context),
            "field-grid" => BindFieldGrid(node, data, labels, context),
            "data-grid" => BindDataGrid(node, data, labels),
            "patient-info" => BindPatientInfo(node, data, labels, context),
            "signature" => BindSignature(context),
            _ => null,
        };
    }

    private static ReportBlock? BindWidget(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        var widget = node.Widget!.Trim().ToLowerInvariant();
        if (string.Equals(widget, HprpWidgetIds.ThaiUrHeader, StringComparison.OrdinalIgnoreCase))
        {
            var title = ResolveText(node.Title, data, labels, context)
                ?? context?.Metadata.Title
                ?? "";
            if (string.IsNullOrWhiteSpace(title))
                return null;

            return new TextReportBlock
            {
                Content = title,
                Style = "title",
            };
        }

        // Dense clinical widgets: recognized so .hprp body is not silently dropped.
        // Pixel layout is composed by dedicated QuestPDF section renderers.
        if (IsDenseClinicalStubWidget(widget))
        {
            return new TextReportBlock
            {
                Title = ResolveText(node.Title, data, labels, context),
                Content = widget,
                Style = "widget",
            };
        }

        return null;
    }

    private static bool IsDenseClinicalStubWidget(string widget) =>
        string.Equals(widget, HprpWidgetIds.ClinicalHctEpoAnnualTable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(widget, HprpWidgetIds.ClinicalHctEpoCopay, StringComparison.OrdinalIgnoreCase)
        || string.Equals(widget, HprpWidgetIds.ClinicalEpoDrugTable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(widget, HprpWidgetIds.ClinicalSoapTable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(widget, HprpWidgetIds.ClinicalConsentNarrative, StringComparison.OrdinalIgnoreCase);

    private static TextReportBlock BindText(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        return new TextReportBlock
        {
            Title = ResolveText(node.Title, data, labels, context),
            Content = ResolveText(node.Content, data, labels, context)
                ?? ResolveBind(node.Bind, data, context)
                ?? "",
            Style = string.IsNullOrWhiteSpace(node.Style) ? "body" : node.Style!,
        };
    }

    private static KeyValueTableReportBlock BindKeyValue(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        var rows = new List<LabelValue>();
        if (node.Rows is not null)
        {
            foreach (var row in node.Rows)
            {
                rows.Add(new LabelValue
                {
                    Label = ResolveText(row.Label, data, labels, context) ?? "",
                    Value = ResolveText(row.Content, data, labels, context)
                        ?? ResolveBind(row.Bind, data, context)
                        ?? "",
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(node.BindRows)
            && !string.Equals(node.BindRows, "$flatten", StringComparison.OrdinalIgnoreCase))
        {
            rows.AddRange(BindLabelValueRows(node.BindRows, data));
        }

        if (node.AppendFlatten || string.Equals(node.BindRows, "$flatten", StringComparison.OrdinalIgnoreCase))
            rows.AddRange(FlattenScalars(data));

        return new KeyValueTableReportBlock
        {
            Title = ResolveText(node.Title, data, labels, context),
            Rows = rows,
            Chrome = node.Chrome,
        };
    }

    private static IReadOnlyList<LabelValue> BindLabelValueRows(string bindRows, JsonElement? data)
    {
        var table = HprpJsonPath.Select(data, bindRows);
        if (table is null || table.Value.ValueKind != JsonValueKind.Array)
            return [];

        var rows = new List<LabelValue>();
        foreach (var item in table.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var label = ReadObjectString(item, "label") ?? ReadObjectString(item, "Label") ?? "";
            var value = ReadObjectString(item, "value") ?? ReadObjectString(item, "Value") ?? "";
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value))
                continue;

            rows.Add(new LabelValue { Label = label, Value = value });
        }

        return rows;
    }

    private static string? ReadObjectString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var prop))
            return null;
        return HprpJsonPath.AsString(prop);
    }

    private static FieldGridReportBlock? BindFieldGrid(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        var fields = new List<FieldGridField>();
        foreach (var field in node.Fields ?? [])
        {
            var label = ResolveText(field.Label, data, labels, context) ?? "";
            var value = ResolveText(field.Content, data, labels, context)
                ?? ResolveBind(field.Bind, data, context);
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value))
                continue;

            fields.Add(new FieldGridField
            {
                Label = label,
                Value = value,
                ColumnSpan = field.ColumnSpan <= 0 ? 1 : field.ColumnSpan,
            });
        }

        if (fields.Count == 0)
            return null;

        return new FieldGridReportBlock
        {
            Title = ResolveText(node.Title, data, labels, context),
            Columns = node.Columns <= 0 ? 2 : node.Columns,
            Fields = fields,
            Chrome = node.Chrome,
        };
    }

    private static DataGridReportBlock? BindDataGrid(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels)
    {
        var table = HprpJsonPath.Select(data, node.BindRows);
        if (table is null || table.Value.ValueKind != JsonValueKind.Array)
            return null;

        var headers = node.ColumnHeaders?.ToList() ?? [];
        if (headers.Count == 0 && !string.IsNullOrWhiteSpace(node.ColumnHeadersBind))
        {
            var boundHeaders = HprpJsonPath.Select(data, node.ColumnHeadersBind);
            if (boundHeaders is { ValueKind: JsonValueKind.Array })
            {
                headers = boundHeaders.Value.EnumerateArray()
                    .Select(h => HprpJsonPath.AsString(h) ?? "")
                    .ToList();
            }
        }

        var rows = new List<IReadOnlyList<string>>();
        foreach (var item in table.Value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Array)
            {
                rows.Add(item.EnumerateArray().Select(cell => HprpJsonPath.AsString(cell) ?? "").ToList());
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            if (headers.Count == 0)
                headers = item.EnumerateObject().Select(p => p.Name).ToList();

            rows.Add(headers.Select(h =>
                item.TryGetProperty(h, out var cell) ? HprpJsonPath.AsString(cell) ?? "" : "").ToList());
        }

        var weights = HprpChrome.ParseColumnWeights(node.Chrome?.ColumnWidths, headers.Count);
        return new DataGridReportBlock
        {
            Title = ResolveText(node.Title, data, labels, null),
            Columns = headers,
            ColumnWeights = weights,
            Rows = rows,
            Chrome = node.Chrome,
        };
    }

    private static PatientInfoReportBlock BindPatientInfo(
        HprpLayoutNode node,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        var column = new List<LabelValue>();
        foreach (var field in node.Fields ?? [])
        {
            column.Add(new LabelValue
            {
                Label = ResolveText(field.Label, data, labels, context) ?? "",
                Value = ResolveText(field.Content, data, labels, context)
                    ?? ResolveBind(field.Bind, data, context)
                    ?? "",
            });
        }

        return new PatientInfoReportBlock
        {
            Title = ResolveText(node.Title, data, labels, context),
            Columns = [column],
        };
    }

    private static SignatureReportBlock? BindSignature(PdfReportContext? context)
    {
        var signatures = context?.Signatures?.Signatures ?? [];
        if (signatures.Count == 0)
            return null;

        return new SignatureReportBlock
        {
            Slots = signatures.Select(s => new SignatureSlot
            {
                Role = s.SignerRole ?? "",
                Name = s.SignerName,
            }).ToList(),
        };
    }

    private static string? ResolveText(
        JsonElement element,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        PdfReportContext? context)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;

        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("$label", out var labelKey)
            && labelKey.ValueKind == JsonValueKind.String)
        {
            var key = labelKey.GetString() ?? "";
            return HprpLabels.Get(labels, key, key);
        }

        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("$bind", out var bind)
            && bind.ValueKind == JsonValueKind.String)
        {
            return ResolveBind(bind.GetString(), data, context);
        }

        return HprpJsonPath.AsString(element);
    }

    private static string? ResolveBind(string? bind, JsonElement? data, PdfReportContext? context)
    {
        if (string.IsNullOrWhiteSpace(bind))
            return null;

        if (string.Equals(bind, "$title", StringComparison.OrdinalIgnoreCase))
            return context?.Metadata.Title;

        if (string.Equals(bind, "$subtitle", StringComparison.OrdinalIgnoreCase))
            return context?.Metadata.Subtitle;

        return HprpJsonPath.AsString(HprpJsonPath.Select(data, bind));
    }

    private static IReadOnlyList<LabelValue> FlattenScalars(JsonElement? data)
    {
        if (data is null || data.Value.ValueKind != JsonValueKind.Object)
            return [];

        var rows = new List<LabelValue>();
        foreach (var property in data.Value.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                continue;

            rows.Add(new LabelValue
            {
                Label = property.Name,
                Value = HprpJsonPath.AsString(property.Value) ?? "",
            });
        }

        return rows;
    }
}

using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp.Header;

public sealed class HprpHeaderResolvedLine
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public string? Label2 { get; init; }
    public string? Value2 { get; init; }
    public float Weight { get; init; } = 1f;
    public string Id { get; init; } = "";
}

public sealed class HprpHeaderLayoutModel
{
    public required HprpHeaderPreset Preset { get; init; }
    public float TitleRowHeightMm { get; init; }
    public float BottomRowHeightMm { get; init; }
    public string TitleText { get; init; } = "";
    public string? LogoBase64 { get; init; }
    public string LogoFallbackText { get; init; } = "";
    public IReadOnlyList<HprpHeaderResolvedLine> MetaLines { get; init; } = [];
    public IReadOnlyList<HprpHeaderResolvedLine> BottomFields { get; init; } = [];
    public bool ShowDateAndHdNo { get; init; }
    public string DateText { get; init; } = "";
    public string HdNoText { get; init; } = "";
}

/// <summary>
/// Builds header display model from preset + JSON data.
/// Shared by QuestPDF composer and Studio HTML renderer.
/// </summary>
public static class HprpHeaderLayoutEngine
{
    public static HprpHeaderLayoutModel Build(
        HprpHeaderPreset preset,
        JsonElement? data,
        string? fallbackTitle = null)
    {
        var root = data ?? default;
        var hasData = root.ValueKind == JsonValueKind.Object;

        var titleBind = preset.Columns
            .FirstOrDefault(c => string.Equals(c.Kind, HprpHeaderColumnKinds.Title, StringComparison.OrdinalIgnoreCase))
            ?.Bind ?? "$.title";
        var logoBind = preset.Columns
            .FirstOrDefault(c => string.Equals(c.Kind, HprpHeaderColumnKinds.Logo, StringComparison.OrdinalIgnoreCase))
            ?.Bind ?? "$.header.logoBase64";

        var title = ReadString(root, hasData, titleBind)
            ?? fallbackTitle
            ?? "";
        var logo = ReadString(root, hasData, logoBind);
        var unitName = ReadString(root, hasData, "$.header.unit.fullName") ?? "";

        var meta = preset.MetaLines
            .Select(line => new HprpHeaderResolvedLine
            {
                Id = line.Id,
                Label = line.Label,
                Value = FormatValue(ReadValue(root, hasData, line.Bind)),
                Label2 = line.Label2,
                Value2 = line.Bind2 is null ? null : FormatValue(ReadValue(root, hasData, line.Bind2)),
                Weight = line.Weight,
            })
            .ToList();

        var bottom = preset.BottomFields
            .Where(f => !f.WhenHdPerWeek || preset.ShowHdPerWeek)
            .Select(line => new HprpHeaderResolvedLine
            {
                Id = line.Id,
                Label = line.Label,
                Value = FormatValue(ReadValue(root, hasData, line.Bind)),
                Weight = Math.Max(0.1f, line.Weight),
            })
            .ToList();

        return new HprpHeaderLayoutModel
        {
            Preset = preset,
            TitleRowHeightMm = preset.TitleRowHeightMm > 0 ? preset.TitleRowHeightMm : 21.6f,
            BottomRowHeightMm = preset.BottomRowHeightMm > 0 ? preset.BottomRowHeightMm : 5.4f,
            TitleText = title,
            LogoBase64 = logo,
            LogoFallbackText = unitName,
            MetaLines = meta,
            BottomFields = bottom,
            ShowDateAndHdNo = preset.ShowDateAndHdNo,
            DateText = FormatValue(ReadValue(root, hasData, "$.header.cycleStartTime")),
            HdNoText = FormatValue(ReadValue(root, hasData, "$.header.treatmentNo")),
        };
    }

    private static string FormatValue(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static string? ReadString(JsonElement root, bool hasData, string? path)
    {
        var v = ReadValue(root, hasData, path);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static string? ReadValue(JsonElement root, bool hasData, string? path)
    {
        if (!hasData || string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = path.Trim();
        if (!normalized.StartsWith("$", StringComparison.Ordinal))
            normalized = "$." + normalized.TrimStart('.');

        var selected = HprpJsonPath.Select(root, normalized);
        if (selected is null)
            return null;

        var el = selected.Value;
        if (el.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                var s = HprpJsonPath.AsString(item);
                if (!string.IsNullOrWhiteSpace(s))
                    parts.Add(s!);
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        return HprpJsonPath.AsString(el);
    }
}

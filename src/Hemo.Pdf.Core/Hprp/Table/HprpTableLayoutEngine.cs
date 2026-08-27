using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp.Table;

public sealed class HprpTableCellModel
{
    public required string Text { get; init; }
    public bool Historical { get; init; }
    public bool Center { get; init; }
}

public sealed class HprpTableRowModel
{
    public required string Kind { get; init; }
    public int GroupIndex { get; init; }
    public int SlotIndex { get; init; }
    public string? GroupLabel { get; init; }
    public IReadOnlyList<HprpTableCellModel> Cells { get; init; } = [];
}

public sealed class HprpTableLayoutModel
{
    public required ResolvedTablePreset Preset { get; init; }
    public float HeaderHeightMm { get; init; }
    public float SlotHeightMm { get; init; }
    public float BlockHeightMm { get; init; }
    public IReadOnlyList<string> HeaderLabels { get; init; } = [];
    public IReadOnlyList<HprpTableRowModel> Rows { get; init; } = [];
}

/// <summary>
/// Builds row/column models from preset + bindings + JSON data.
/// Shared rules for QuestPDF composer and Studio HTML renderer.
/// </summary>
public static class HprpTableLayoutEngine
{
    private const float HeaderBarHeightMm = 5f;
    private const float LayoutSafetyMm = 1.5f;
    private const float MinSlotHeightMm = 4f;

    private static readonly string[] ThaiMonthLabels =
    [
        "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
        "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค.",
    ];

    public static HprpTableLayoutModel Build(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        IReadOnlyDictionary<string, string> labels,
        JsonElement? data,
        float boxHeightMm)
    {
        var rowMode = preset.RowMode.Trim().ToLowerInvariant();
        var slots = Math.Max(1, preset.SlotsPerGroup);
        var slotHeight = BudgetSlotHeight(boxHeightMm, rowMode, preset.GroupCount, slots);
        var blockHeight = slotHeight * slots;

        var headerLabels = BuildHeaderLabels(preset, labels);
        var rows = rowMode switch
        {
            HprpTableRowModes.Freedom => BuildFreedomRows(preset, bindings, data, labels),
            HprpTableRowModes.Monthly => BuildGroupedRows(preset, bindings, data, labels, preset.GroupCount, slots),
            _ => BuildGroupedRows(preset, bindings, data, labels, Math.Max(1, preset.GroupCount), slots),
        };

        return new HprpTableLayoutModel
        {
            Preset = preset,
            HeaderHeightMm = HeaderBarHeightMm,
            SlotHeightMm = slotHeight,
            BlockHeightMm = blockHeight,
            HeaderLabels = headerLabels,
            Rows = rows,
        };
    }

    public static float BudgetSlotHeight(float boxHeightMm, string rowMode, int groupCount, int slotsPerGroup)
    {
        var groups = rowMode == HprpTableRowModes.Freedom
            ? 1
            : Math.Max(1, groupCount);
        var available = Math.Max(0f, boxHeightMm - HeaderBarHeightMm - LayoutSafetyMm);
        var perBlock = available / groups;
        return Math.Max(perBlock / Math.Max(1, slotsPerGroup), MinSlotHeightMm);
    }

    private static IReadOnlyList<string> BuildHeaderLabels(
        ResolvedTablePreset preset,
        IReadOnlyDictionary<string, string> labels)
    {
        var list = new List<string>
        {
            HprpLabels.Get(labels, preset.DateColumns.DateHeaderLabelKey ?? "colDate", "วัน/เดือน/ปี"),
        };
        foreach (var col in preset.Columns)
        {
            list.Add(HprpLabels.Get(labels, col.LabelKey ?? col.Id, col.Title ?? col.Id));
        }

        return list;
    }

    private static IReadOnlyList<HprpTableRowModel> BuildFreedomRows(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels)
    {
        _ = labels;
        var rows = new List<HprpTableRowModel>();
        var count = Math.Max(1, preset.FreedomRowCount);
        for (var r = 0; r < count; r++)
        {
            rows.Add(new HprpTableRowModel
            {
                Kind = "freedom",
                GroupIndex = 0,
                SlotIndex = r,
                Cells = BuildFreedomCells(preset, bindings, data, r),
            });
        }

        return rows;
    }

    private static IReadOnlyList<HprpTableRowModel> BuildGroupedRows(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels,
        int groupCount,
        int slotsPerGroup)
    {
        _ = labels;
        var rows = new List<HprpTableRowModel>();
        for (var g = 0; g < groupCount; g++)
        {
            var groupLabel = ResolveGroupLabel(bindings, data, g)
                ?? DefaultMonthLabel(g);

            for (var s = 0; s < slotsPerGroup; s++)
            {
                rows.Add(new HprpTableRowModel
                {
                    Kind = "entry",
                    GroupIndex = g,
                    SlotIndex = s,
                    GroupLabel = s == 0 ? groupLabel : null,
                    Cells = BuildEntryCells(preset, bindings, data, g, s),
                });
            }
        }

        return rows;
    }

    private static string DefaultMonthLabel(int groupIndex) =>
        groupIndex >= 0 && groupIndex < ThaiMonthLabels.Length
            ? ThaiMonthLabels[groupIndex]
            : (groupIndex + 1).ToString();

    private static string? ResolveGroupLabel(
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        int groupIndex)
    {
        if (data is not JsonElement root || root.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var b in bindings)
        {
            if (!string.Equals(b.Context, HprpTableBindingContexts.GroupLabel, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(b.Column, "month", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(b.Column, "group", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return HprpJsonPath.ReadAt(root, b.Path, groupIndex, 0);
        }

        return null;
    }

    private static IReadOnlyList<HprpTableCellModel> BuildEntryCells(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        int groupIndex,
        int slotIndex)
    {
        var cells = new List<HprpTableCellModel>();
        var root = data ?? default;
        var hasData = root.ValueKind == JsonValueKind.Object;

        cells.Add(new HprpTableCellModel
        {
            Text = ResolveBinding(bindings, preset, root, hasData, groupIndex, slotIndex, "day", HprpTableBindingContexts.Entry) ?? " ",
            Historical = ResolveHistorical(bindings, root, hasData, groupIndex, slotIndex),
            Center = true,
        });

        foreach (var col in preset.Columns)
        {
            var text = ResolveBinding(bindings, preset, root, hasData, groupIndex, slotIndex, col.Id, HprpTableBindingContexts.Entry);
            cells.Add(new HprpTableCellModel
            {
                Text = string.IsNullOrWhiteSpace(text) ? " " : text!,
                Historical = col.IsLab && ResolveHistorical(bindings, root, hasData, groupIndex, slotIndex),
                Center = col.Center,
            });
        }

        return cells;
    }

    private static IReadOnlyList<HprpTableCellModel> BuildFreedomCells(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        int rowIndex)
    {
        var cells = new List<HprpTableCellModel>();
        var root = data ?? default;
        var hasData = root.ValueKind == JsonValueKind.Object;

        foreach (var col in preset.Columns)
        {
            var text = ResolveBinding(bindings, preset, root, hasData, rowIndex, 0, col.Id, HprpTableBindingContexts.FreedomRow);
            cells.Add(new HprpTableCellModel
            {
                Text = string.IsNullOrWhiteSpace(text) ? " " : text!,
                Center = col.Center,
            });
        }

        return cells;
    }

    private static bool ResolveHistorical(
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement root,
        bool hasData,
        int groupIndex,
        int slotIndex)
    {
        if (!hasData)
            return false;

        foreach (var b in bindings)
        {
            if (!string.Equals(b.Context, HprpTableBindingContexts.LabHistorical, StringComparison.OrdinalIgnoreCase))
                continue;
            return HprpJsonPath.ReadBoolAt(root, b.Path, groupIndex, slotIndex);
        }

        foreach (var b in bindings)
        {
            if (!b.Path.Contains("labIsHistorical", StringComparison.OrdinalIgnoreCase))
                continue;
            return HprpJsonPath.ReadBoolAt(root, b.Path, groupIndex, slotIndex);
        }

        return false;
    }

    private static string? ResolveBinding(
        IReadOnlyList<HprpTableBinding> bindings,
        ResolvedTablePreset preset,
        JsonElement root,
        bool hasData,
        int groupIndex,
        int slotIndex,
        string columnId,
        string context)
    {
        if (!hasData)
            return null;

        foreach (var b in bindings)
        {
            if (!string.Equals(b.Column, columnId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(b.Context, context, StringComparison.OrdinalIgnoreCase))
                continue;

            return HprpJsonPath.ReadAt(root, b.Path, groupIndex, slotIndex, groupIndex);
        }

        return null;
    }
}

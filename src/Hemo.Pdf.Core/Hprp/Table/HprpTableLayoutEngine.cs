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
        var freedomRows = ResolveFreedomRowCount(preset);
        var matrixRows = rowMode == HprpTableRowModes.Matrix
            ? CountMatrixVisualRows(data)
            : 0;
        var slots = rowMode switch
        {
            HprpTableRowModes.Freedom => freedomRows,
            HprpTableRowModes.Matrix => Math.Max(1, matrixRows),
            _ => Math.Max(1, preset.SlotsPerGroup),
        };
        var slotHeight = BudgetSlotHeight(boxHeightMm, rowMode, preset.GroupCount, slots, freedomRows, matrixRows);
        var blockHeight = slotHeight * (rowMode switch
        {
            HprpTableRowModes.Freedom => freedomRows,
            HprpTableRowModes.Matrix => Math.Max(1, matrixRows),
            _ => slots,
        });

        var headerLabels = BuildHeaderLabels(preset, labels, rowMode);
        var rows = rowMode switch
        {
            HprpTableRowModes.Freedom => BuildFreedomRows(preset, bindings, data, labels),
            HprpTableRowModes.Matrix => BuildMatrixRows(preset, data),
            HprpTableRowModes.Monthly => BuildGroupedRows(preset, bindings, data, labels, preset.GroupCount, Math.Max(1, preset.SlotsPerGroup)),
            _ => BuildGroupedRows(preset, bindings, data, labels, Math.Max(1, preset.GroupCount), Math.Max(1, preset.SlotsPerGroup)),
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

    public static float BudgetSlotHeight(
        float boxHeightMm,
        string rowMode,
        int groupCount,
        int slotsPerGroup,
        int freedomRowCount = 0,
        int matrixRowCount = 0)
    {
        // Matrix draws a 2-row header (year + month) inside the body; reserve ~10mm.
        var headerReserve = rowMode == HprpTableRowModes.Matrix
            ? HeaderBarHeightMm * 2f + 4f
            : HeaderBarHeightMm;
        var available = Math.Max(0f, boxHeightMm - headerReserve);
        if (rowMode == HprpTableRowModes.Freedom)
        {
            var rows = Math.Max(1, freedomRowCount > 0 ? freedomRowCount : slotsPerGroup);
            return Math.Max(available / rows, MinSlotHeightMm);
        }

        if (rowMode == HprpTableRowModes.Matrix)
        {
            var rows = Math.Max(1, matrixRowCount > 0 ? matrixRowCount : slotsPerGroup);
            return Math.Max(available / rows, MinSlotHeightMm);
        }

        var groups = Math.Max(1, groupCount);
        var perBlock = available / groups;
        return Math.Max(perBlock / Math.Max(1, slotsPerGroup), MinSlotHeightMm);
    }

    private static int ResolveFreedomRowCount(ResolvedTablePreset preset)
    {
        if (preset.StaticRows is { Count: > 0 })
            return preset.StaticRows.Count;
        return Math.Max(1, preset.FreedomRowCount);
    }

    private static IReadOnlyList<string> BuildHeaderLabels(
        ResolvedTablePreset preset,
        IReadOnlyDictionary<string, string> labels,
        string rowMode)
    {
        var list = new List<string>();
        if (rowMode != HprpTableRowModes.Freedom && rowMode != HprpTableRowModes.Matrix)
        {
            list.Add(HprpLabels.Get(labels, preset.DateColumns.DateHeaderLabelKey ?? "colDate", "วัน/เดือน/ปี"));
        }

        foreach (var col in preset.Columns)
        {
            list.Add(HprpLabels.Get(labels, col.LabelKey ?? col.Id, col.Title ?? col.Id));
        }

        return list;
    }

    /// <summary>
    /// Studio/PDF shared row model for matrix mode (item label + month marks).
    /// Group header rows use <c>Kind = "group"</c> with a single spanning label cell.
    /// </summary>
    private static IReadOnlyList<HprpTableRowModel> BuildMatrixRows(
        ResolvedTablePreset preset,
        JsonElement? data)
    {
        _ = preset;
        var rows = new List<HprpTableRowModel>();
        if (data is not JsonElement root || root.ValueKind != JsonValueKind.Object)
            return rows;

        var monthCount = 0;
        if (root.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
            monthCount = cols.GetArrayLength();

        if (!root.TryGetProperty("checklistItems", out var items) || items.ValueKind != JsonValueKind.Array)
            return rows;

        string? lastGroup = null;
        var index = 0;
        foreach (var item in items.EnumerateArray())
        {
            var group = item.TryGetProperty("group", out var g) && g.ValueKind == JsonValueKind.String
                ? g.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(group) && group != lastGroup)
            {
                lastGroup = group;
                rows.Add(new HprpTableRowModel
                {
                    Kind = "group",
                    GroupIndex = index,
                    SlotIndex = 0,
                    GroupLabel = group,
                    Cells =
                    [
                        new HprpTableCellModel { Text = group!, Center = false },
                    ],
                });
            }

            var label = item.TryGetProperty("label", out var lab) && lab.ValueKind == JsonValueKind.String
                ? lab.GetString() ?? " "
                : " ";
            var cells = new List<HprpTableCellModel>
            {
                new() { Text = string.IsNullOrWhiteSpace(label) ? " " : label, Center = false },
            };

            if (item.TryGetProperty("marks", out var marks) && marks.ValueKind == JsonValueKind.Array)
            {
                foreach (var mark in marks.EnumerateArray())
                {
                    var text = mark.ValueKind == JsonValueKind.String ? mark.GetString() : null;
                    cells.Add(new HprpTableCellModel
                    {
                        Text = string.IsNullOrWhiteSpace(text) ? " " : text!,
                        Center = true,
                    });
                }
            }

            while (cells.Count < monthCount + 1)
                cells.Add(new HprpTableCellModel { Text = " ", Center = true });

            rows.Add(new HprpTableRowModel
            {
                Kind = "matrix",
                GroupIndex = index,
                SlotIndex = 0,
                Cells = cells,
            });
            index++;
        }

        return rows;
    }

    private static int CountMatrixVisualRows(JsonElement? data)
    {
        if (data is not JsonElement root || root.ValueKind != JsonValueKind.Object)
            return 8;

        if (!root.TryGetProperty("checklistItems", out var items) || items.ValueKind != JsonValueKind.Array)
            return 8;

        var count = 0;
        string? lastGroup = null;
        foreach (var item in items.EnumerateArray())
        {
            var group = item.TryGetProperty("group", out var g) && g.ValueKind == JsonValueKind.String
                ? g.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(group) && group != lastGroup)
            {
                lastGroup = group;
                count++;
            }

            count++;
        }

        return Math.Max(1, count);
    }

    private static IReadOnlyList<HprpTableRowModel> BuildFreedomRows(
        ResolvedTablePreset preset,
        IReadOnlyList<HprpTableBinding> bindings,
        JsonElement? data,
        IReadOnlyDictionary<string, string> labels)
    {
        _ = labels;
        var rows = new List<HprpTableRowModel>();
        if (preset.StaticRows is { Count: > 0 } staticRows)
        {
            for (var r = 0; r < staticRows.Count; r++)
            {
                var cells = new List<HprpTableCellModel>();
                var src = staticRows[r];
                for (var c = 0; c < preset.Columns.Count; c++)
                {
                    var text = c < src.Count ? src[c] : " ";
                    cells.Add(new HprpTableCellModel
                    {
                        Text = string.IsNullOrWhiteSpace(text) ? " " : text,
                        Center = preset.Columns[c].Center,
                    });
                }

                rows.Add(new HprpTableRowModel
                {
                    Kind = "freedom",
                    GroupIndex = 0,
                    SlotIndex = r,
                    Cells = cells,
                });
            }

            return rows;
        }

        var count = ResolveFreedomRowCount(preset);
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

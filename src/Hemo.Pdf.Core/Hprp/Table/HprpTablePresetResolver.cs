using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp.Table;

public static class HprpTablePresetResolver
{
    public static ResolvedTablePreset Resolve(
        HprpTablePreset basePreset,
        HprpDesignerElement? element = null)
    {
        var columns = MergeColumns(basePreset.Columns, element?.ColumnOverrides);
        var chrome = element?.Chrome ?? basePreset.Chrome;

        return new ResolvedTablePreset
        {
            Id = basePreset.Id,
            RowMode = basePreset.RowMode,
            GroupCount = basePreset.GroupCount,
            SlotsPerGroup = basePreset.SlotsPerGroup,
            FreedomRowCount = basePreset.FreedomRowCount,
            DateColumns = basePreset.DateColumns ?? new HprpTableDateColumns(),
            Columns = columns,
            Chrome = chrome,
        };
    }

    public static IReadOnlyList<HprpTableColumnDef> MergeColumns(
        IReadOnlyList<HprpTableColumnDef> baseColumns,
        IReadOnlyList<HprpTableColumnDef>? overrides)
    {
        if (overrides is not { Count: > 0 })
            return baseColumns;

        var map = baseColumns.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var o in overrides)
        {
            if (string.IsNullOrWhiteSpace(o.Id))
                continue;
            map[o.Id] = o;
        }

        // Preserve base order, append new override ids at end.
        var ordered = baseColumns.Select(c => map[c.Id]).ToList();
        foreach (var o in overrides)
        {
            if (string.IsNullOrWhiteSpace(o.Id))
                continue;
            if (baseColumns.All(b => !string.Equals(b.Id, o.Id, StringComparison.OrdinalIgnoreCase)))
                ordered.Add(o);
        }

        return ordered;
    }
}

public sealed class ResolvedTablePreset
{
    public required string Id { get; init; }
    public required string RowMode { get; init; }
    public int GroupCount { get; init; }
    public int SlotsPerGroup { get; init; }
    public int FreedomRowCount { get; init; }
    public required HprpTableDateColumns DateColumns { get; init; }
    public required IReadOnlyList<HprpTableColumnDef> Columns { get; init; }
    public HprpChrome? Chrome { get; init; }
}

using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Maps <see cref="HprpLayoutNode.ColumnPlan"/> onto known <see cref="HctEpoMonthEntry"/> fields.
/// Empty plan = recipe default (same columns as the original hardcoded table).
/// </summary>
public static class HctEpoAnnualColumnPlan
{
    public readonly record struct ColumnSpec(
        float Weight,
        string Bind,
        string LabelKey,
        string Title,
        bool Center,
        bool IsLab);

    public static IReadOnlyList<ColumnSpec> Resolve(HprpLayoutNode? node)
    {
        var recipe = HprpWidgetRecipes.ClinicalHctEpoAnnualTable;
        if (node?.ColumnPlan is not { Count: > 0 })
            return FromRecipe(recipe.DefaultColumnPlan, recipe);

        return node.ColumnPlan.Select(item => ResolveItem(item, recipe)).ToList();
    }

    public static string? ReadCell(HctEpoMonthEntry entry, string? bind) =>
        bind?.Trim() switch
        {
            "hb" => entry.Hb,
            "hct" => entry.Hct,
            "epoName" => entry.EpoName,
            "frequencyText" => entry.FrequencyText,
            "injectionDate" => entry.InjectionDate,
            "remarks" => entry.Remarks,
            _ => null,
        };

    private static IReadOnlyList<ColumnSpec> FromRecipe(
        IReadOnlyList<HprpColumnPlanItem> plan,
        HprpWidgetRecipe recipe) =>
        plan.Select(item => ResolveItem(item, recipe)).ToList();

    private static ColumnSpec ResolveItem(HprpColumnPlanItem item, HprpWidgetRecipe recipe)
    {
        var bind = item.Bind?.Trim() ?? "";
        var known = recipe.BindFields.FirstOrDefault(f =>
            string.Equals(f.Bind, bind, StringComparison.OrdinalIgnoreCase));
        var defaultCol = recipe.DefaultColumnPlan.FirstOrDefault(c =>
            string.Equals(c.Bind, bind, StringComparison.OrdinalIgnoreCase));

        return new ColumnSpec(
            Weight: item.Weight is > 0 ? item.Weight.Value : defaultCol?.Weight ?? 1f,
            Bind: known?.Bind ?? bind,
            LabelKey: FirstNonEmpty(item.LabelKey, known?.LabelKey, defaultCol?.LabelKey) ?? "",
            Title: FirstNonEmpty(known?.DefaultLabel, item.LabelKey, bind) ?? "",
            Center: item.Center ?? defaultCol?.Center ?? false,
            IsLab: item.IsLab ?? defaultCol?.IsLab ?? false);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}

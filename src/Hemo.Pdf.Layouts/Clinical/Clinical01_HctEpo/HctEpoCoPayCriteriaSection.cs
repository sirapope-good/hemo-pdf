using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Co-pay eligibility reference tables (NHSO + Social Security).
/// Values come from <see cref="HctEpoCoPayCriteria"/> so tenants can override later.
/// </summary>
public sealed class HctEpoCoPayCriteriaSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;
    private const float RowH = 5.5f;
    private const float GapMm = 3f;

    /// <summary>Fixed height used by the page budget so month rows fill down to this block.</summary>
    public static float EstimateHeightMm(HctEpoCoPayCriteria criteria)
    {
        var nhsoRows = Math.Max(criteria.NhsoRules?.Count ?? 0, 1);
        var ssoRows = Math.Max((criteria.SsoRules?.Count ?? 0) + 1, 2); // +1 empty trailing row
        var titleMm = HemosheetThaiUrStyle.HeaderBarHeightMm;
        var tablesMm = Math.Max(
            HemosheetThaiUrStyle.HeaderBarHeightMm + nhsoRows * RowH,
            HemosheetThaiUrStyle.HeaderBarHeightMm + ssoRows * RowH);
        return titleMm + tablesMm;
    }

    public void Compose(IContainer container, HctEpoCoPayCriteria criteria)
    {
        // TODO(tenant): allow tenant override / replace of co-pay criteria block.
        container.Column(col =>
        {
            col.Item().Element(c => c.HeaderBar(criteria.Title));

            col.Item().Row(row =>
            {
                row.RelativeItem(1.15f).Element(c => ComposeNhso(c, criteria.NhsoRules));
                row.ConstantItem(GapMm, Mm);
                row.RelativeItem(1.85f).Element(c => ComposeSso(c, criteria.SsoRules));
            });
        });
    }

    private static void ComposeNhso(IContainer container, IReadOnlyList<HctEpoNhsoRuleRow> rules)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(1.4f);
            });

            HeaderCell(t, "สปสช");
            HeaderCell(t, "จำนวนเข็ม/สัปดาห์");

            foreach (var rule in rules)
            {
                DataCell(t, rule.Condition);
                DataCell(t, rule.InjectionsPerWeek);
            }
        });
    }

    private static void ComposeSso(IContainer container, IReadOnlyList<HctEpoSsoRuleRow> rules)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.8f);
                cols.RelativeColumn(1.0f);
                cols.RelativeColumn(1.0f);
                cols.RelativeColumn(1.0f);
            });

            HeaderCell(t, "ประกันสังคม");
            HeaderCell(t, "Hct ≤ 36");
            HeaderCell(t, "Hct > 36");
            HeaderCell(t, "Hct ≥ 39");

            foreach (var rule in rules)
            {
                DataCell(t, rule.Medicine, center: false);
                DataCell(t, rule.HctLe36);
                DataCell(t, rule.HctGt36);
                DataCell(t, rule.HctGe39);
            }

            // Match PDF empty trailing row for visual balance.
            DataCell(t, "");
            DataCell(t, "");
            DataCell(t, "");
            DataCell(t, "");
        });
    }

    private static void HeaderCell(TableDescriptor t, string text)
    {
        t.Cell()
            .Border(Bw)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .PaddingHorizontal(1f)
            .Text(text)
            .Style(ThaiUrText.Bold);
    }

    private static void DataCell(TableDescriptor t, string? text, bool center = true)
    {
        var cell = t.Cell()
            .Border(Bw)
            .MinHeight(RowH, Mm)
            .PaddingHorizontal(1.2f)
            .AlignMiddle();

        if (center)
            cell.AlignCenter();

        cell.Text(string.IsNullOrWhiteSpace(text) ? "" : text!).Style(ThaiUrText.Base);
    }
}

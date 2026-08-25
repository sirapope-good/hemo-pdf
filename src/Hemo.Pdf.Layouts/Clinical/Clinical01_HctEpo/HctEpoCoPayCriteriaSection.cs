using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Co-pay eligibility reference tables (NHSO + Social Security).
/// Values come from <see cref="HctEpoCoPayCriteria"/> so tenants can override later.
/// Header chrome / labels come from the layout node when present.
/// </summary>
public sealed class HctEpoCoPayCriteriaSection
{
    private const Unit Mm = Unit.Millimetre;
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

    public void Compose(
        IContainer container,
        HctEpoCoPayCriteria criteria,
        IReadOnlyDictionary<string, string>? labels = null,
        HprpLayoutNode? node = null)
    {
        var chrome = node?.Chrome;
        container.Column(col =>
        {
            col.Item().Element(c => ComposeTitleBar(c, criteria.Title, chrome));

            col.Item().Row(row =>
            {
                row.RelativeItem(1.15f).Element(c => ComposeNhso(c, criteria.NhsoRules, labels, chrome));
                row.ConstantItem(GapMm, Mm);
                row.RelativeItem(1.85f).Element(c => ComposeSso(c, criteria.SsoRules, labels, chrome));
            });
        });
    }

    private static void ComposeTitleBar(IContainer container, string title, HprpChrome? chrome)
    {
        var bw = BorderWidth(chrome);
        container
            .Border(bw)
            .Background(HeaderFill(chrome))
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .Text(title)
            .Style(HeaderTextStyle(chrome));
    }

    private static void ComposeNhso(
        IContainer container,
        IReadOnlyList<HctEpoNhsoRuleRow> rules,
        IReadOnlyDictionary<string, string>? labels,
        HprpChrome? chrome)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(1.4f);
            });

            HeaderCell(t, HprpLabels.Get(labels, "nhso", "สปสช"), chrome);
            HeaderCell(t, HprpLabels.Get(labels, "nhsoInjections", "จำนวนเข็ม/สัปดาห์"), chrome);

            foreach (var rule in rules)
            {
                DataCell(t, rule.Condition, chrome);
                DataCell(t, rule.InjectionsPerWeek, chrome);
            }
        });
    }

    private static void ComposeSso(
        IContainer container,
        IReadOnlyList<HctEpoSsoRuleRow> rules,
        IReadOnlyDictionary<string, string>? labels,
        HprpChrome? chrome)
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

            HeaderCell(t, HprpLabels.Get(labels, "sso", "ประกันสังคม"), chrome);
            HeaderCell(t, HprpLabels.Get(labels, "ssoHctLe36", "Hct ≤ 36"), chrome);
            HeaderCell(t, HprpLabels.Get(labels, "ssoHctGt36", "Hct > 36"), chrome);
            HeaderCell(t, HprpLabels.Get(labels, "ssoHctGe39", "Hct ≥ 39"), chrome);

            foreach (var rule in rules)
            {
                DataCell(t, rule.Medicine, chrome, center: false);
                DataCell(t, rule.HctLe36, chrome);
                DataCell(t, rule.HctGt36, chrome);
                DataCell(t, rule.HctGe39, chrome);
            }

            DataCell(t, "", chrome);
            DataCell(t, "", chrome);
            DataCell(t, "", chrome);
            DataCell(t, "", chrome);
        });
    }

    private static void HeaderCell(TableDescriptor t, string text, HprpChrome? chrome)
    {
        var bw = BorderWidth(chrome);
        t.Cell()
            .Border(bw)
            .Background(HeaderFill(chrome))
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .PaddingHorizontal(1f)
            .Text(text)
            .Style(HeaderTextStyle(chrome));
    }

    private static void DataCell(TableDescriptor t, string? text, HprpChrome? chrome, bool center = true)
    {
        var bw = BorderWidth(chrome);
        var cell = t.Cell()
            .Border(bw)
            .MinHeight(RowH, Mm)
            .PaddingHorizontal(1.2f)
            .AlignMiddle();

        if (center)
            cell.AlignCenter();

        var style = BodyTextStyle(chrome);
        cell.Text(string.IsNullOrWhiteSpace(text) ? "" : text!).Style(style);
    }

    private static float BorderWidth(HprpChrome? chrome) =>
        string.IsNullOrWhiteSpace(chrome?.Border)
            ? HemosheetThaiUrStyle.BorderWidth
            : HprpChrome.ResolveBorderWidth(chrome);

    private static string HeaderFill(HprpChrome? chrome) =>
        HprpChrome.FileHeaderFillOrNull(chrome)
        ?? ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground);

    private static TextStyle HeaderTextStyle(HprpChrome? chrome)
    {
        var style = ThaiUrText.Bold;
        return chrome?.FontSize is > 0 and < 48
            ? style.FontSize(chrome.FontSize.Value)
            : style;
    }

    private static TextStyle BodyTextStyle(HprpChrome? chrome)
    {
        var style = ThaiUrText.Base;
        return chrome?.FontSize is > 0 and < 48
            ? style.FontSize(chrome.FontSize.Value)
            : style;
    }
}

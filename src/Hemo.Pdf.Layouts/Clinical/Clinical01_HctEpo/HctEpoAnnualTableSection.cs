using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Annual Hct / EPO table: 12 month blocks.
/// Month name is a narrow merged cell; day + labs + ESA share ruled sub-rows
/// (same thin border style as the co-pay criteria tables).
/// Historical labs/dates render gray.
/// Column order/visibility comes from <see cref="HprpLayoutNode.ColumnPlan"/> when present.
/// </summary>
public sealed class HctEpoAnnualTableSection
{
    private const Unit Mm = Unit.Millimetre;

    private const float MonthColWeight = 0.45f;
    private const float DayColWeight = 1.35f;
    private const float DateGroupWeight = MonthColWeight + DayColWeight;
    private const float EntryColsWeight = 8.2f;
    private const float RightBlockWeight = DayColWeight + EntryColsWeight;

    public void Compose(
        IContainer container,
        HctEpoReportViewModel vm,
        float monthRowHeightMm,
        IReadOnlyDictionary<string, string>? labels = null,
        HprpLayoutNode? node = null)
    {
        var columns = HctEpoAnnualColumnPlan.Resolve(node);
        var chrome = node?.Chrome;
        var slotHeightMm = monthRowHeightMm / HctEpoMonthLabels.SlotsPerMonth;

        container.Column(col =>
        {
            col.Item().Element(c => ComposeHeaderRow(c, labels, columns, chrome));

            foreach (var row in HctEpoMonthLabels.EnsureTwelve(vm.Months))
            {
                col.Item().Element(c => ComposeMonthBlock(c, row, slotHeightMm, columns, chrome));
            }
        });
    }

    private static void ComposeHeaderRow(
        IContainer container,
        IReadOnlyDictionary<string, string>? labels,
        IReadOnlyList<HctEpoAnnualColumnPlan.ColumnSpec> columns,
        HprpChrome? chrome)
    {
        var bw = BorderWidth(chrome);
        var fill = HeaderFill(chrome);
        var headerStyle = HeaderTextStyle(chrome);

        container.Row(row =>
        {
            row.RelativeItem(DateGroupWeight)
                .Border(bw)
                .Background(fill)
                .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
                .AlignMiddle()
                .AlignCenter()
                .Text(HprpLabels.Get(labels, "colDate", "วัน/เดือน/ปี"))
                .Style(headerStyle);

            foreach (var col in columns)
            {
                row.RelativeItem(col.Weight)
                    .Border(bw)
                    .Background(fill)
                    .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
                    .AlignMiddle()
                    .AlignCenter()
                    .Text(HprpLabels.Get(labels, col.LabelKey, col.Title))
                    .Style(headerStyle);
            }
        });
    }

    private static void ComposeMonthBlock(
        IContainer container,
        HctEpoMonthRow month,
        float slotHeightMm,
        IReadOnlyList<HctEpoAnnualColumnPlan.ColumnSpec> columns,
        HprpChrome? chrome)
    {
        var slots = PadEntries(month.Entries, HctEpoMonthLabels.SlotsPerMonth);
        var blockHeightMm = slotHeightMm * HctEpoMonthLabels.SlotsPerMonth;
        var bw = BorderWidth(chrome);

        container.Height(blockHeightMm, Mm).Row(row =>
        {
            row.RelativeItem(MonthColWeight)
                .ExtendVertical()
                .Border(bw)
                .AlignMiddle()
                .AlignCenter()
                .PaddingHorizontal(0.5f)
                .Text(month.MonthLabel)
                .Style(BodyTextStyle(chrome, historical: false));

            row.RelativeItem(RightBlockWeight)
                .ExtendVertical()
                .Column(entryCol =>
                {
                    foreach (var entry in slots)
                    {
                        entryCol.Item()
                            .Height(slotHeightMm, Mm)
                            .Element(c => ComposeEntrySubRow(c, entry, columns, chrome));
                    }
                });
        });
    }

    private static void ComposeEntrySubRow(
        IContainer container,
        HctEpoMonthEntry entry,
        IReadOnlyList<HctEpoAnnualColumnPlan.ColumnSpec> columns,
        HprpChrome? chrome)
    {
        var bw = BorderWidth(chrome);
        var labStyle = BodyTextStyle(chrome, entry.LabIsHistorical);
        var bodyStyle = BodyTextStyle(chrome, historical: false);

        container.Row(row =>
        {
            row.RelativeItem(DayColWeight)
                .Border(bw)
                .ExtendVertical()
                .PaddingHorizontal(1.2f)
                .AlignMiddle()
                .AlignCenter()
                .Text(string.IsNullOrWhiteSpace(entry.DayLabel) ? " " : entry.DayLabel!)
                .Style(entry.LabIsHistorical ? labStyle : bodyStyle);

            foreach (var col in columns)
            {
                var value = HctEpoAnnualColumnPlan.ReadCell(entry, col.Bind);
                var cell = row.RelativeItem(col.Weight)
                    .Border(bw)
                    .ExtendVertical()
                    .PaddingHorizontal(1.2f)
                    .AlignMiddle();

                if (col.Center)
                    cell = cell.AlignCenter();

                cell.Text(string.IsNullOrWhiteSpace(value) ? " " : value!)
                    .Style(col.IsLab ? labStyle : bodyStyle);
            }
        });
    }

    private static IReadOnlyList<HctEpoMonthEntry> PadEntries(
        IReadOnlyList<HctEpoMonthEntry>? entries,
        int slotCount)
    {
        var list = (entries ?? Array.Empty<HctEpoMonthEntry>()).ToList();
        while (list.Count < slotCount)
            list.Add(new HctEpoMonthEntry());
        return list.Count > slotCount ? list.Take(slotCount).ToList() : list;
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

    private static TextStyle BodyTextStyle(HprpChrome? chrome, bool historical)
    {
        var style = historical ? ThaiUrText.Historical : ThaiUrText.Base;
        return chrome?.FontSize is > 0 and < 48
            ? style.FontSize(chrome.FontSize.Value)
            : style;
    }
}

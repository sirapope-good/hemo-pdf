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
/// </summary>
public sealed class HctEpoAnnualTableSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    private const float MonthColWeight = 0.45f;
    private const float DayColWeight = 1.35f;
    private const float DateGroupWeight = MonthColWeight + DayColWeight;
    private const float EntryColsWeight = 8.2f;
    private const float RightBlockWeight = DayColWeight + EntryColsWeight;

    private static readonly ColumnSpec[] Columns =
    [
        new(1.0f, "colHb", "Hb(g/dL)", Center: true, IsLab: true),
        new(1.0f, "colHct", "Hct(%)", Center: true, IsLab: true),
        new(1.8f, "colEpo", "EPO", Center: false, IsLab: false),
        new(1.8f, "colFrequency", "ความถี่", Center: false, IsLab: false),
        new(1.2f, "colInjectDay", "วันฉีด", Center: true, IsLab: false),
        new(1.4f, "colRemarks", "หมายเหตุ", Center: false, IsLab: false),
    ];

    private readonly record struct ColumnSpec(float Weight, string LabelKey, string Title, bool Center, bool IsLab);

    public void Compose(
        IContainer container,
        HctEpoReportViewModel vm,
        float monthRowHeightMm,
        IReadOnlyDictionary<string, string>? labels = null)
    {
        var slotHeightMm = monthRowHeightMm / HctEpoMonthLabels.SlotsPerMonth;

        container.Column(col =>
        {
            col.Item().Element(c => ComposeHeaderRow(c, labels));

            foreach (var row in HctEpoMonthLabels.EnsureTwelve(vm.Months))
            {
                col.Item().Element(c => ComposeMonthBlock(c, row, slotHeightMm));
            }
        });
    }

    private static void ComposeHeaderRow(IContainer container, IReadOnlyDictionary<string, string>? labels)
    {
        container.Row(row =>
        {
            row.RelativeItem(DateGroupWeight)
                .Border(Bw)
                .Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
                .AlignMiddle()
                .AlignCenter()
                .Text(HprpLabels.Get(labels, "colDate", "วัน/เดือน/ปี"))
                .Style(ThaiUrText.Bold);

            foreach (var col in Columns)
            {
                row.RelativeItem(col.Weight)
                    .Border(Bw)
                    .Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
                    .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
                    .AlignMiddle()
                    .AlignCenter()
                    .Text(HprpLabels.Get(labels, col.LabelKey, col.Title))
                    .Style(ThaiUrText.Bold);
            }
        });
    }

    private static void ComposeMonthBlock(
        IContainer container,
        HctEpoMonthRow month,
        float slotHeightMm)
    {
        var slots = PadEntries(month.Entries, HctEpoMonthLabels.SlotsPerMonth);
        var blockHeightMm = slotHeightMm * HctEpoMonthLabels.SlotsPerMonth;

        container.Height(blockHeightMm, Mm).Row(row =>
        {
            row.RelativeItem(MonthColWeight)
                .ExtendVertical()
                .Border(Bw)
                .AlignMiddle()
                .AlignCenter()
                .PaddingHorizontal(0.5f)
                .Text(month.MonthLabel)
                .Style(ThaiUrText.Base);

            row.RelativeItem(RightBlockWeight)
                .ExtendVertical()
                .Column(entryCol =>
                {
                    foreach (var entry in slots)
                    {
                        entryCol.Item()
                            .Height(slotHeightMm, Mm)
                            .Element(c => ComposeEntrySubRow(c, entry));
                    }
                });
        });
    }

    private static void ComposeEntrySubRow(IContainer container, HctEpoMonthEntry entry)
    {
        var labStyle = entry.LabIsHistorical ? ThaiUrText.Historical : ThaiUrText.Base;
        var values = new[]
        {
            entry.Hb,
            entry.Hct,
            entry.EpoName,
            entry.FrequencyText,
            entry.InjectionDate,
            entry.Remarks,
        };

        container.Row(row =>
        {
            row.RelativeItem(DayColWeight)
                .Border(Bw)
                .ExtendVertical()
                .PaddingHorizontal(1.2f)
                .AlignMiddle()
                .AlignCenter()
                .Text(string.IsNullOrWhiteSpace(entry.DayLabel) ? " " : entry.DayLabel!)
                .Style(entry.LabIsHistorical ? ThaiUrText.Historical : ThaiUrText.Base);

            for (var i = 0; i < Columns.Length; i++)
            {
                var col = Columns[i];
                var cell = row.RelativeItem(col.Weight)
                    .Border(Bw)
                    .ExtendVertical()
                    .PaddingHorizontal(1.2f)
                    .AlignMiddle();

                if (col.Center)
                    cell = cell.AlignCenter();

                cell.Text(string.IsNullOrWhiteSpace(values[i]) ? " " : values[i]!)
                    .Style(col.IsLab ? labStyle : ThaiUrText.Base);
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
}

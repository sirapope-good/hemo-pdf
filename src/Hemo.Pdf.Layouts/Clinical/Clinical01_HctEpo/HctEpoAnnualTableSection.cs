using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;

/// <summary>
/// Annual Hct / EPO table: 12 month blocks.
/// Month name is a narrow merged cell; day (lab date) + labs + ESA share 3 ruled sub-rows.
/// Historical labs render gray. Thick vertical rule separates lab block from EPO block.
/// </summary>
public sealed class HctEpoAnnualTableSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;
    private const float LabEpoDividerBw = 1.25f;

    // Abbreviation only (ม.ค.) — keep narrow; day needs room for dd-MM-yyyy.
    private const float MonthColWeight = 0.45f;
    private const float DayColWeight = 1.35f;
    private const float DateGroupWeight = MonthColWeight + DayColWeight; // 1.8
    private const float EntryColsWeight = 8.2f;
    private const float RightBlockWeight = DayColWeight + EntryColsWeight; // 9.55

    private static readonly (float Weight, string Title, bool ThickRight)[] TrailingHeaders =
    [
        (1.0f, "Hb(g/dL)", false),
        (1.0f, "Hct(%)", true),   // thick rule before EPO
        (1.8f, "EPO", false),
        (1.8f, "ความถี่", false),
        (1.2f, "วันฉีด", false),
        (1.4f, "หมายเหตุ", false),
    ];

    private static readonly (float Weight, bool Center, bool IsLab, bool ThickRightAfter)[] EntryValueColumns =
    [
        (1.0f, true, true, false),   // Hb
        (1.0f, true, true, true),    // Hct — thick right edge
        (1.8f, false, false, false), // EPO
        (1.8f, false, false, false), // Frequency
        (1.2f, true, false, false),  // Injection date
        (1.4f, false, false, false), // Remarks
    ];

    public void Compose(IContainer container, HctEpoReportViewModel vm, float monthRowHeightMm)
    {
        var slotHeightMm = monthRowHeightMm / HctEpoMonthLabels.SlotsPerMonth;

        container.Column(col =>
        {
            col.Item().Element(ComposeHeaderRow);

            foreach (var row in EnsureTwelve(vm.Months))
            {
                col.Item().Element(c => ComposeMonthBlock(c, row, slotHeightMm));
            }
        });
    }

    private static void ComposeHeaderRow(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem(DateGroupWeight)
                .Border(Bw)
                .Background(HemosheetThaiUrStyle.HeaderBackground)
                .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
                .AlignMiddle()
                .AlignCenter()
                .Text("วัน/เดือน/ปี")
                .Style(ThaiUrText.Bold);

            foreach (var (weight, title, thickRight) in TrailingHeaders)
            {
                HeaderCell(row.RelativeItem(weight), title, thickRight);
            }
        });
    }

    private static void HeaderCell(IContainer cell, string title, bool thickRight)
    {
        ApplyBoxBorder(cell, thickRight)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .Text(title)
            .Style(ThaiUrText.Bold);
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
            ApplyBoxBorder(row.RelativeItem(DayColWeight), thickRight: false)
                .ExtendVertical()
                .PaddingHorizontal(1.0f)
                .AlignMiddle()
                .AlignCenter()
                .Text(string.IsNullOrWhiteSpace(entry.DayLabel) ? " " : entry.DayLabel!)
                .Style(entry.LabIsHistorical ? ThaiUrText.Historical : ThaiUrText.Base);

            for (var i = 0; i < EntryValueColumns.Length; i++)
            {
                var (weight, center, isLab, thickRight) = EntryValueColumns[i];
                var cell = ApplyBoxBorder(row.RelativeItem(weight), thickRight)
                    .ExtendVertical()
                    .PaddingHorizontal(1.2f)
                    .PaddingVertical(0.6f)
                    .AlignMiddle();

                if (center)
                    cell = cell.AlignCenter();

                cell.Text(string.IsNullOrWhiteSpace(values[i]) ? " " : values[i]!)
                    .Style(isLab ? labStyle : ThaiUrText.Base);
            }
        });
    }

    /// <summary>
    /// Per-edge borders so the Hct→EPO divider can be thicker without doubling other lines.
    /// </summary>
    private static IContainer ApplyBoxBorder(IContainer cell, bool thickRight) =>
        cell
            .BorderLeft(Bw)
            .BorderTop(Bw)
            .BorderBottom(Bw)
            .BorderRight(thickRight ? LabEpoDividerBw : Bw);

    private static IReadOnlyList<HctEpoMonthEntry> PadEntries(
        IReadOnlyList<HctEpoMonthEntry>? entries,
        int slotCount)
    {
        var list = (entries ?? Array.Empty<HctEpoMonthEntry>()).ToList();
        while (list.Count < slotCount)
            list.Add(new HctEpoMonthEntry());
        return list.Count > slotCount ? list.Take(slotCount).ToList() : list;
    }

    private static IReadOnlyList<HctEpoMonthRow> EnsureTwelve(IReadOnlyList<HctEpoMonthRow> months)
    {
        if (months.Count == 12)
            return months;

        var byIndex = months.ToDictionary(m => m.MonthIndex);
        return Enumerable.Range(1, 12)
            .Select(i => byIndex.TryGetValue(i, out var row)
                ? row
                : new HctEpoMonthRow
                {
                    MonthIndex = i,
                    MonthLabel = HctEpoMonthLabels.ThaiShort[i - 1],
                })
            .ToList();
    }
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Layouts.Clinical.Clinical05_ProgressNote;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Table;

/// <summary>QuestPDF renderer for <see cref="HprpTableLayoutModel"/>.</summary>
public static class ConfigurableTableComposer
{
    private const Unit Mm = Unit.Millimetre;

    public static void Compose(
        IContainer container,
        HprpTableLayoutModel model,
        object? boundModel = null)
    {
        var preset = model.Preset;
        var chrome = preset.Chrome;
        var bw = BorderWidth(chrome);
        var rowMode = preset.RowMode.Trim().ToLowerInvariant();

        if (rowMode == HprpTableRowModes.Freedom)
        {
            ComposeFreedom(container, model, bw, chrome, boundModel);
            return;
        }

        if (rowMode == HprpTableRowModes.Matrix)
        {
            ComposeMatrix(container, chrome, boundModel);
            return;
        }

        container.Column(col =>
        {
            col.Item().Element(c => ComposeHeader(c, model, bw, chrome));

            var grouped = model.Rows
                .GroupBy(r => r.GroupIndex)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                col.Item().Element(c => ComposeGroupBlock(c, group.ToList(), model, bw, chrome));
            }
        });
    }

    private static void ComposeMatrix(
        IContainer container,
        HprpChrome? chrome,
        object? boundModel)
    {
        if (boundModel is Clinical05ProgressNoteChecklistReportViewModel checklist)
        {
            Clinical05ChecklistSections.ComposeChecklistGridSection(container, checklist, chrome);
            return;
        }

        container.Border(0.5f).Padding(2)
            .Text("Checklist matrix — bind clinical-05 checklist sample")
            .FontSize(8);
    }

    private static void ComposeHeader(
        IContainer container,
        HprpTableLayoutModel model,
        float bw,
        HprpChrome? chrome)
    {
        var preset = model.Preset;
        var dateWeight = preset.DateColumns.MonthWeight + preset.DateColumns.DayWeight;
        var fill = HeaderFill(chrome);
        var style = HeaderTextStyle(chrome);

        container.Row(row =>
        {
            row.RelativeItem(dateWeight)
                .Border(bw)
                .Background(fill)
                .Height(model.HeaderHeightMm, Mm)
                .AlignMiddle()
                .AlignCenter()
                .Text(model.HeaderLabels.FirstOrDefault() ?? "")
                .Style(style);

            var dataHeaders = model.HeaderLabels.Skip(1).ToList();
            for (var i = 0; i < preset.Columns.Count; i++)
            {
                var label = i < dataHeaders.Count ? dataHeaders[i] : preset.Columns[i].Id;
                row.RelativeItem(Math.Max(0.1f, preset.Columns[i].Weight))
                    .Border(bw)
                    .Background(fill)
                    .Height(model.HeaderHeightMm, Mm)
                    .AlignMiddle()
                    .AlignCenter()
                    .Text(label)
                    .Style(style);
            }
        });
    }

    private static void ComposeGroupBlock(
        IContainer container,
        IReadOnlyList<HprpTableRowModel> slots,
        HprpTableLayoutModel model,
        float bw,
        HprpChrome? chrome)
    {
        var preset = model.Preset;
        var monthWeight = preset.DateColumns.MonthWeight;
        var dayWeight = preset.DateColumns.DayWeight;
        var rightWeight = dayWeight + preset.Columns.Sum(c => c.Weight);
        var groupLabel = slots.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.GroupLabel))?.GroupLabel ?? " ";

        container.Height(model.BlockHeightMm, Mm).Row(row =>
        {
            row.RelativeItem(monthWeight)
                .ExtendVertical()
                .Border(bw)
                .AlignMiddle()
                .AlignCenter()
                .PaddingHorizontal(0.5f)
                .Text(groupLabel)
                .Style(BodyTextStyle(chrome, false));

            row.RelativeItem(rightWeight)
                .ExtendVertical()
                .Column(entryCol =>
                {
                    foreach (var slot in slots.OrderBy(s => s.SlotIndex))
                    {
                        entryCol.Item()
                            .Height(model.SlotHeightMm, Mm)
                            .Element(c => ComposeEntryRow(c, slot, preset, bw, chrome));
                    }
                });
        });
    }

    private static void ComposeEntryRow(
        IContainer container,
        HprpTableRowModel slot,
        ResolvedTablePreset preset,
        float bw,
        HprpChrome? chrome)
    {
        var dayWeight = preset.DateColumns.DayWeight;
        var cells = slot.Cells;
        var dayCell = cells.Count > 0 ? cells[0] : new HprpTableCellModel { Text = " " };

        container.Row(row =>
        {
            row.RelativeItem(dayWeight)
                .Border(bw)
                .ExtendVertical()
                .PaddingHorizontal(1.2f)
                .AlignMiddle()
                .AlignCenter()
                .Text(dayCell.Text)
                .Style(BodyTextStyle(chrome, dayCell.Historical));

            for (var i = 0; i < preset.Columns.Count; i++)
            {
                var cellIndex = i + 1;
                var cell = cellIndex < cells.Count
                    ? cells[cellIndex]
                    : new HprpTableCellModel { Text = " " };
                var col = preset.Columns[i];
                var box = row.RelativeItem(Math.Max(0.1f, col.Weight))
                    .Border(bw)
                    .ExtendVertical()
                    .PaddingHorizontal(1.2f)
                    .AlignMiddle();

                if (col.Center)
                    box = box.AlignCenter();

                box.Text(cell.Text)
                    .Style(BodyTextStyle(chrome, cell.Historical || col.IsLab && cell.Historical));
            }
        });
    }

    private static void ComposeFreedom(
        IContainer container,
        HprpTableLayoutModel model,
        float bw,
        HprpChrome? chrome,
        object? boundModel)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeFreedomHeader(c, model, bw, chrome));
            foreach (var row in model.Rows)
            {
                col.Item().Height(model.SlotHeightMm, Mm)
                    .Element(c => ComposeFreedomRow(c, row, model.Preset, bw, chrome, model.SlotHeightMm, boundModel));
            }
        });
    }

    private static void ComposeFreedomHeader(
        IContainer container,
        HprpTableLayoutModel model,
        float bw,
        HprpChrome? chrome)
    {
        var fill = HeaderFill(chrome);
        var style = HeaderTextStyle(chrome);
        container.Row(row =>
        {
            foreach (var col in model.Preset.Columns)
            {
                var idx = model.Preset.Columns.ToList().IndexOf(col);
                var label = idx >= 0 && idx < model.HeaderLabels.Count
                    ? model.HeaderLabels[idx]
                    : col.Title ?? col.Id;
                row.RelativeItem(Math.Max(0.1f, col.Weight))
                    .Border(bw)
                    .Background(fill)
                    .Height(model.HeaderHeightMm, Mm)
                    .AlignMiddle()
                    .AlignCenter()
                    .Text(label)
                    .Style(style);
            }
        });
    }

    private static void ComposeFreedomRow(
        IContainer container,
        HprpTableRowModel row,
        ResolvedTablePreset preset,
        float bw,
        HprpChrome? chrome,
        float slotHeightMm,
        object? boundModel)
    {
        var bands = HprpChrome.ResolveBandWeights(
            chrome?.BandWeights,
            Clinical05SoapTableSection.DefaultSoapBandWeights);
        var sessions = boundModel is Clinical05ProgressNoteReportViewModel soapVm
            ? soapVm.Sessions
            : null;

        container.Row(r =>
        {
            for (var i = 0; i < preset.Columns.Count; i++)
            {
                var cell = i < row.Cells.Count ? row.Cells[i] : new HprpTableCellModel { Text = " " };
                var col = preset.Columns[i];
                var kind = (col.CellKind ?? HprpTableCellKinds.Text).Trim();

                if (string.Equals(kind, HprpTableCellKinds.SoapProgress, StringComparison.OrdinalIgnoreCase))
                {
                    Clinical05SoapSession? session = null;
                    if (sessions is not null && row.SlotIndex >= 0 && row.SlotIndex < sessions.Count)
                        session = sessions[row.SlotIndex];

                    r.RelativeItem(Math.Max(0.1f, col.Weight))
                        .Element(c => Clinical05SoapTableSection.ComposeProgressCell(
                            c,
                            session,
                            slotHeightMm,
                            bw,
                            bands));
                    continue;
                }

                var box = r.RelativeItem(Math.Max(0.1f, col.Weight))
                    .Border(bw)
                    .ExtendVertical()
                    .PaddingHorizontal(1.2f)
                    .AlignTop();
                if (col.Center)
                    box = box.AlignCenter();
                box.Text(cell.Text).Style(BodyTextStyle(chrome, cell.Historical));
            }
        });
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
        return chrome?.FontSize is > 0 and < 48 ? style.FontSize(chrome.FontSize.Value) : style;
    }

    private static TextStyle BodyTextStyle(HprpChrome? chrome, bool historical)
    {
        var style = historical ? ThaiUrText.Historical : ThaiUrText.Base;
        return chrome?.FontSize is > 0 and < 48 ? style.FontSize(chrome.FontSize.Value) : style;
    }
}

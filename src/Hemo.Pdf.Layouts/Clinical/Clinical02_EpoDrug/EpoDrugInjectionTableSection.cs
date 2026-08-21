using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical02_EpoDrug;

/// <summary>
/// Injection table: ว/ด/ป | เข็มที่ | Sticker (blank) | พยาบาลผู้ฉีด | หมายเหตุ.
/// </summary>
public sealed class EpoDrugInjectionTableSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;
    private const int MinEmptyRows = 8;

    public void Compose(
        IContainer container,
        EpoDrugReportViewModel vm,
        float rowHeightMm,
        IReadOnlyDictionary<string, string>? labels = null)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.1f);
                cols.RelativeColumn(0.7f);
                cols.RelativeColumn(2.2f);
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(1.4f);
            });

            HeaderCell(t, HprpLabels.Get(labels, "colDate", "ว/ด/ป"));
            HeaderCell(t, HprpLabels.Get(labels, "colDose", "เข็มที่"));
            HeaderCell(t, HprpLabels.Get(labels, "colSticker", "Sticker"));
            HeaderCell(t, HprpLabels.Get(labels, "colNurse", "พยาบาลผู้ฉีด"));
            HeaderCell(t, HprpLabels.Get(labels, "colRemarks", "หมายเหตุ"));

            var rows = vm.Rows ?? [];
            var drawCount = Math.Max(rows.Count, MinEmptyRows);
            for (var i = 0; i < drawCount; i++)
            {
                var row = i < rows.Count ? rows[i] : null;
                DataCell(t, row?.DateLabel ?? string.Empty, rowHeightMm);
                DataCell(t, row is null ? string.Empty : row.DoseIndex.ToString(), rowHeightMm, center: true);
                DataCell(t, string.Empty, rowHeightMm);
                DataCell(t, row?.NurseName ?? string.Empty, rowHeightMm);
                DataCell(t, row?.Remarks ?? string.Empty, rowHeightMm);
            }
        });
    }

    private static void HeaderCell(TableDescriptor t, string text)
    {
        t.Cell()
            .Border(Bw)
            .Background(ReportSectionHeaderChrome.Resolve(HemosheetThaiUrStyle.HeaderBackground))
            .Height(HemosheetThaiUrStyle.HeaderBarHeightMm, Mm)
            .AlignMiddle()
            .AlignCenter()
            .PaddingHorizontal(1f)
            .Text(text)
            .Style(ThaiUrText.Bold);
    }

    private static void DataCell(TableDescriptor t, string text, float heightMm, bool center = false)
    {
        var cell = t.Cell()
            .Border(Bw)
            .MinHeight(heightMm, Mm)
            .PaddingHorizontal(1.2f)
            .AlignMiddle();

        if (center)
        {
            cell = cell.AlignCenter();
        }

        cell.Text(text ?? string.Empty).Style(ThaiUrText.Base);
    }
}

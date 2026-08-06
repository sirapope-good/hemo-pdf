using Hemo.Pdf.Core.Models.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.ThaiUr;

/// <summary>
/// Shared ThaiUR clinical report header: logo | title | patient meta, then Diagnosis / Drug Allergy
/// (and optional Date + HD NO. cell). Reusable across the clinical report pack via
/// <see cref="HemosheetReportSettingsViewModel.ShowDateAndHdNo"/>.
/// </summary>
public static class ThaiUrReportHeader
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetThaiUrStyle.BorderWidth;

    public static void Compose(
        IContainer container,
        HemosheetReportViewModel vm,
        string title = "Hemodialysis Record")
    {
        var showDateHdNo = vm.LayoutContext.ReportSettings.ShowDateAndHdNo;

        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(48, Mm);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.ConstantColumn(70, Mm);
            });

            t.Cell().Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Element(logo => Logo(logo, vm));

            t.Cell().ColumnSpan(2).Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Text(title).Style(ThaiUrText.Title);

            // Patient meta only (Name…ID). Date/HD NO. are a separate bordered cell below
            // so other reports can omit that cell without leaving an empty gap.
            t.Cell().Border(Bw).Height(HemosheetThaiUrStyle.TitleHeightMm, Mm)
                .PaddingHorizontal(1.5f).AlignTop().Column(meta =>
                {
                    MetaLine(meta, "Name", vm.Patient.Name, null, null);
                    MetaLine(meta, "CN", vm.Patient.Hn, "Age", ThaiUrData.Num(vm.Patient.Age));
                    MetaLine(meta, "Coverage", vm.Patient.Coverage, null, null);
                    MetaLine(meta, "ID Card NO.", vm.Patient.IdentityNumber, null, null);
                });

            if (showDateHdNo)
            {
                t.Cell().ColumnSpan(3).Border(Bw).Height(HemosheetThaiUrStyle.MetaRowHeightMm, Mm).Row(r =>
                {
                    DiagnosisAllergyRow(r, vm);
                });

                t.Cell().Border(Bw).Height(HemosheetThaiUrStyle.MetaRowHeightMm, Mm)
                    .PaddingHorizontal(1.5f).AlignMiddle().Row(r =>
                    {
                        r.ConstantItem(22, Mm).AlignMiddle().Text("Date").Style(ThaiUrText.Bold);
                        r.RelativeItem().AlignMiddle()
                            .Text(ThaiUrData.Date(vm.CycleStartTime)).Style(ThaiUrText.Base);
                        r.ConstantItem(12, Mm).AlignMiddle().Text("HD NO.").Style(ThaiUrText.Bold);
                        r.ConstantItem(14, Mm).AlignMiddle()
                            .Text(ThaiUrData.Num(vm.TreatmentNo)).Style(ThaiUrText.Base);
                    });
            }
            else
            {
                t.Cell().ColumnSpan(4).Border(Bw).Height(HemosheetThaiUrStyle.MetaRowHeightMm, Mm).Row(r =>
                {
                    DiagnosisAllergyRow(r, vm);
                });
            }
        });
    }

    private static void DiagnosisAllergyRow(RowDescriptor r, HemosheetReportViewModel vm)
    {
        r.ConstantItem(16, Mm).LabelBold("Diagnosis");
        r.RelativeItem(2).Value(vm.Patient.Diagnosis ?? vm.Patient.Underlying);
        r.ConstantItem(20, Mm).LabelBold("Drug Allergy");
        r.RelativeItem(1).Value(ThaiUrData.Allergies(vm));
    }

    private static void Logo(IContainer c, HemosheetReportViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.LogoBase64))
        {
            try
            {
                var raw = vm.LogoBase64;
                var comma = raw.IndexOf(',');
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                    raw = raw[(comma + 1)..];
                c.Image(Convert.FromBase64String(raw)).FitArea();
                return;
            }
            catch
            {
                // fall through
            }
        }

        c.Text(vm.Unit.FullName ?? "").Style(ThaiUrText.Base);
    }

    private static void MetaLine(ColumnDescriptor col, string label, string? value, string? label2, string? value2)
    {
        col.Item().Height(HemosheetThaiUrStyle.MetaRowHeightMm, Mm).AlignMiddle().Row(r =>
        {
            r.ConstantItem(22, Mm).AlignMiddle().Text(label).Style(ThaiUrText.Bold);
            r.RelativeItem().AlignMiddle().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).Style(ThaiUrText.Base);
            if (label2 is not null)
            {
                r.ConstantItem(12, Mm).AlignMiddle().Text(label2).Style(ThaiUrText.Bold);
                r.ConstantItem(14, Mm).AlignMiddle().Text(string.IsNullOrWhiteSpace(value2) ? "-" : value2).Style(ThaiUrText.Base);
            }
        });
    }
}

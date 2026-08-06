using System.Globalization;
using Hemo.Pdf.Core.Models.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Default;

/// <summary>
/// CICM-style clinical-03 Default header: logo | unit name | patient card.
/// Fields that ThaiUR keeps in content / sub-header (Birth, Sex, Physician, Allergies,
/// Treatment No., Date+time) live in this header instead of a Diagnosis/Drug Allergy row.
/// </summary>
public static class DefaultReportHeader
{
    private const Unit Mm = Unit.Millimetre;
    private const float Bw = HemosheetDefaultStyle.BorderWidth;
    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");

    public static void Compose(IContainer container, HemosheetReportViewModel vm)
    {
        // Do NOT put a multi-row Column inside a fixed-Height table cell when Thai text may
        // exceed MetaRowHeightMm — QuestPDF then paginates one meta row per page.
        // Let the table row grow with the patient card; logo/title cells AlignMiddle.
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(42, Mm);
                cols.RelativeColumn(1.2f);
                cols.RelativeColumn(2.2f);
            });

            t.Cell().Border(Bw).MinHeight(HemosheetDefaultStyle.TitleHeightMm, Mm)
                .AlignMiddle().AlignCenter()
                .Element(c => Logo(c, vm));

            t.Cell().Border(Bw).MinHeight(HemosheetDefaultStyle.TitleHeightMm, Mm)
                .Padding(2).AlignMiddle().AlignCenter()
                .Text(UnitTitle(vm))
                .FontFamily(HemosheetDefaultStyle.FontFamily)
                .FontSize(HemosheetDefaultStyle.TitleFontSize)
                .Bold()
                .FontColor(Colors.Black);

            t.Cell().Border(Bw).MinHeight(HemosheetDefaultStyle.TitleHeightMm, Mm)
                .PaddingHorizontal(1.5f).PaddingVertical(1).AlignTop()
                .Column(meta =>
                {
                    var p = vm.Patient;
                    MetaLine(meta, "Name", p.Name, "ID No", p.IdentityNumber);
                    MetaLine(meta, "HN", p.Hn, "Physician", p.DoctorName ?? vm.DoctorName);
                    MetaLine(meta, "Birth Date", FormatBirth(p.BirthDate), "Treatment No.", Num(vm.TreatmentNo));
                    MetaLine(meta, "Allergies", Allergies(vm), "Sex", p.Sex);
                    MetaLine(meta, "Date", FormatSessionDate(vm.CycleStartTime), "Coverage", p.Coverage);
                    MetaLine(meta, "Age", AgeYears(p.Age), null, null);
                });
        });
    }

    private static void MetaLine(
        ColumnDescriptor col,
        string label1,
        string? value1,
        string? label2,
        string? value2)
    {
        // MinHeight (not Height) so Thai glyphs can grow the row without page-splitting the cell.
        col.Item().MinHeight(HemosheetDefaultStyle.MetaRowHeightMm, Mm).AlignMiddle().Row(r =>
        {
            r.ConstantItem(22, Mm).AlignMiddle().Text(label1).Style(DefaultText.Bold);
            r.RelativeItem().AlignMiddle().Text(Dash(value1)).Style(DefaultText.Base);
            if (label2 is not null)
            {
                r.ConstantItem(24, Mm).AlignMiddle().Text(label2).Style(DefaultText.Bold);
                r.RelativeItem().AlignMiddle().Text(Dash(value2)).Style(DefaultText.Base);
            }
        });
    }

    private static string UnitTitle(HemosheetReportViewModel vm) =>
        string.IsNullOrWhiteSpace(vm.Unit.FullName) ? "Hemodialysis Record" : vm.Unit.FullName!;

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
                c.Width(36, Mm).Height(28, Mm).Image(Convert.FromBase64String(raw)).FitArea();
                return;
            }
            catch
            {
                // fall through
            }
        }

        c.Text("").FontSize(1);
    }

    private static string Allergies(HemosheetReportViewModel vm) =>
        vm.Patient.Allergies.Count == 0 ? "No Allergy" : string.Join(", ", vm.Patient.Allergies);

    private static string FormatBirth(DateTime? dt) =>
        dt is null ? "-" : dt.Value.ToString("dd MMM yyyy", EnGb);

    private static string FormatSessionDate(DateTime? dt)
    {
        if (dt is null) return "-";
        return dt.Value.ToString("ddd, dd MMM yyyy HH:mm", EnGb);
    }

    private static string AgeYears(int? age) =>
        age is null ? "-" : $"{age.Value} years";

    private static string Num(int? v) => v?.ToString(CultureInfo.InvariantCulture) ?? "-";

    private static string Dash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;
}

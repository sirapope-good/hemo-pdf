using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

public static class PdfComponentHelpers
{
    public static void RenderCheckbox(RowDescriptor row, bool isChecked, float size = 12f)
    {
        var container = row.ConstantItem(size)
            .MinHeight(size)
            .MaxHeight(size)
            .Height(size)
            .MinWidth(size)
            .MaxWidth(size)
            .Width(size)
            .Border(0.5f)
            .Background(Colors.White)
            .AlignMiddle()
            .AlignCenter();

        container.Text(text =>
        {
            if (isChecked)
            {
                text.Span("/")
                    .FontFamily("Arial")
                    .FontSize(size * 0.75f)
                    .FontColor(Colors.Black)
                    .Bold();
            }
            else
            {
                text.Span("\u200B")
                    .FontSize(size * 0.75f);
            }
        });
    }
}

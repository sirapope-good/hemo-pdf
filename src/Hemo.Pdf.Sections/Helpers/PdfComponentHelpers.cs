using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

public static class PdfComponentHelpers
{
    public static void RenderCheckbox(RowDescriptor row, bool isChecked, float size = 12f)
        => PdfCheckbox.Render(row, isChecked, size);
}

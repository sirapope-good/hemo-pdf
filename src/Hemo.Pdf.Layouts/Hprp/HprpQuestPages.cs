using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Rendering;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Hprp;

public static class HprpQuestPages
{
    public static QuestLayout Create(
        HprpResolvedPage page,
        Action<IContainer>? header,
        Action<IContainer>? content,
        Action<IContainer>? footer,
        bool landscape = false) =>
        new()
        {
            MarginMillimeters = page.Left,
            MarginTop = page.Top,
            MarginBottom = page.Bottom,
            MarginLeft = page.Left,
            MarginRight = page.Right,
            Header = header,
            Content = content,
            Footer = footer,
            Landscape = landscape,
        };
}

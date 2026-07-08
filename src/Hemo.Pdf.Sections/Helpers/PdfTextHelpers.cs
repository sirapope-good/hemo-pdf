using System.Globalization;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

public static class PdfTextHelpers
{
    public static void ComposeInlineLabelValue(
        TextDescriptor text,
        string label,
        string? value,
        bool showPlaceholderForEmpty = true)
    {
        text.Span($"{label} : ")
            .FontFamily(PdfStyleDefaults.Header.MetadataFontFamily)
            .FontSize(PdfStyleDefaults.Header.MetadataFontSize)
            .SemiBold();

        var displayValue = string.IsNullOrWhiteSpace(value) && !showPlaceholderForEmpty
            ? string.Empty
            : string.IsNullOrWhiteSpace(value) ? "—" : value;

        text.Span(displayValue)
            .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
            .FontSize(PdfStyleDefaults.Body.DataFontSize);
    }

    public static void RenderLabelValue(ColumnDescriptor column, string label, string? value)
    {
        column.Item().Text(text =>
        {
            text.DefaultTextStyle(style => style
                .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                .FontSize(PdfStyleDefaults.Body.DataFontSize));

            text.Span($"{label} : ").SemiBold();

            if (string.IsNullOrWhiteSpace(value))
            {
                text.Span("—");
                return;
            }

            var lines = value.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                text.Span("—");
                return;
            }

            text.Span(lines[0]);
            for (var i = 1; i < lines.Length; i++)
            {
                text.Line(lines[i]);
            }
        });
    }

    public static IContainer ApplyAlignment(IContainer container, HeaderAlignment alignment) =>
        alignment switch
        {
            HeaderAlignment.Center => container.AlignCenter(),
            HeaderAlignment.Right => container.AlignRight(),
            _ => container.AlignLeft(),
        };

    public static string FormatDate(DateTime? value) =>
        value.HasValue
            ? value.Value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("en-GB"))
            : "—";
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical.Clinical04_Prescription;

/// <summary>
/// Thai UR #04: two equal columns — Prescription / Review of Treatment | Physicians Order.
/// Pads empty writing lines so a blank print still has usable space.
/// </summary>
public sealed class Clinical04PrescriptionColumnsSection
{
    private const Unit Mm = Unit.Millimetre;
    private const float DefaultBorder = HemosheetThaiUrStyle.BorderWidth;
    private const float DateStripMm = 14f;
    private const float SignatureMm = 22f;
    private const float LinePadMm = 1.2f;
    private const float IndentStepMm = 3.5f;
    private const float MinLineMm = 5.2f;
    private const float MinHeaderMm = 8f;

    /// <summary>Minimum content lines per column when printing blank / sparse data.</summary>
    internal const int MinEmptyLines = 18;

    private enum LineKind
    {
        Blank,
        Text,
        Section,
        CheckItem,
    }

    private readonly record struct ContentLine(string Text, LineKind Kind, int Indent = 0);

    public void Compose(
        IContainer container,
        Clinical04PrescriptionReportViewModel vm,
        float blockHeightMm,
        IReadOnlyDictionary<string, string>? labels = null,
        HprpLayoutNode? node = null,
        PdfReportContext? context = null)
    {
        var chrome = node?.Chrome;
        var border = chrome?.Border is null ? DefaultBorder : HprpChrome.ResolveBorderWidth(chrome);
        var headerFill = HprpChrome.ResolveHeaderFill(
            chrome,
            context,
            HemosheetThaiUrStyle.HeaderBackground);
        var headerHeightMm = Math.Max(
            HprpChrome.ResolveHeaderHeightMm(chrome, HemosheetThaiUrStyle.HeaderBarHeightMm),
            MinHeaderMm);
        var fontSize = chrome?.FontSize is > 0 and < 48
            ? chrome.FontSize.Value
            : HemosheetThaiUrStyle.BaseFontSize;

        var contentHeightMm = Math.Max(
            blockHeightMm - headerHeightMm - SignatureMm,
            MinLineMm * 4f);

        var leftLines = BuildDialysisLines(vm.DialysisFields);
        var rightLines = BuildMedicineLines(vm.MedicinePrescriptionLines, vm.MedHistoryLines, labels);
        var lineCount = Math.Max(Math.Max(leftLines.Count, rightLines.Count), MinEmptyLines);
        // Fit exactly into the content band so blank prints still fill the page.
        var lineHeightMm = contentHeightMm / lineCount;

        container.Height(blockHeightMm, Mm).Row(row =>
        {
            row.RelativeItem().Element(c => ComposeColumn(
                c,
                dateLabel: vm.OrderDate,
                title: HprpLabels.Get(labels, "colPrescription", "Prescription / Review of Treatment"),
                lines: PadLines(leftLines, lineCount, LineKind.Blank),
                lineHeightMm,
                headerHeightMm,
                contentHeightMm,
                border,
                headerFill,
                fontSize,
                doctorName: vm.IsSigned ? vm.DoctorName : string.Empty,
                doctorUpdated: vm.IsSigned ? vm.DoctorUpdated : string.Empty,
                labels));

            row.RelativeItem().Element(c => ComposeColumn(
                c,
                dateLabel: vm.OrderDate,
                title: HprpLabels.Get(labels, "colPhysiciansOrder", "Physicians Order"),
                lines: PadLines(rightLines, lineCount, LineKind.Blank),
                lineHeightMm,
                headerHeightMm,
                contentHeightMm,
                border,
                headerFill,
                fontSize,
                doctorName: vm.IsSigned ? vm.DoctorName : string.Empty,
                doctorUpdated: vm.IsSigned ? vm.DoctorUpdated : string.Empty,
                labels));
        });
    }

    private static void ComposeColumn(
        IContainer container,
        string dateLabel,
        string title,
        IReadOnlyList<ContentLine> lines,
        float lineHeightMm,
        float headerHeightMm,
        float contentHeightMm,
        float border,
        string headerFill,
        float fontSize,
        string doctorName,
        string doctorUpdated,
        IReadOnlyDictionary<string, string>? labels)
    {
        container.Border(border).Column(col =>
        {
            col.Item().Height(headerHeightMm, Mm).Element(c =>
                ComposeHeader(c, title, border, headerFill, fontSize, labels));

            col.Item().Height(contentHeightMm, Mm).Element(c =>
                ComposeBody(c, dateLabel, lines, lineHeightMm, contentHeightMm, border, fontSize));

            col.Item().Height(SignatureMm, Mm).Element(c =>
                ComposeSignature(c, doctorName, doctorUpdated, border, fontSize, labels));
        });
    }

    private static void ComposeHeader(
        IContainer container,
        string title,
        float border,
        string headerFill,
        float fontSize,
        IReadOnlyDictionary<string, string>? labels)
    {
        container.Background(headerFill).Row(row =>
        {
            row.ConstantItem(DateStripMm, Mm)
                .BorderRight(border)
                .AlignMiddle()
                .AlignCenter()
                .Padding(0.3f, Mm)
                .Text(HprpLabels.Get(labels, "colDateTime", "Date / Time"))
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(Math.Max(fontSize - 1f, 6f))
                .SemiBold();

            row.RelativeItem()
                .AlignMiddle()
                .AlignCenter()
                .Padding(0.3f, Mm)
                .Text(title)
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(Math.Max(fontSize - 0.5f, 6.5f))
                .SemiBold();
        });
    }

    private static void ComposeBody(
        IContainer container,
        string dateLabel,
        IReadOnlyList<ContentLine> lines,
        float lineHeightMm,
        float contentHeightMm,
        float border,
        float fontSize)
    {
        container.Row(row =>
        {
            row.ConstantItem(DateStripMm, Mm)
                .BorderRight(border)
                .Padding(1, Mm)
                .AlignTop()
                .AlignCenter()
                .Text(dateLabel ?? string.Empty)
                .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                .FontSize(Math.Max(fontSize - 0.5f, 6f));

            row.RelativeItem().Column(col =>
            {
                var usedMm = 0f;
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    var display = line.Kind switch
                    {
                        LineKind.CheckItem => "☐  " + line.Text,
                        _ => line.Text,
                    };
                    // Last line absorbs float remainder so sum == parent Height exactly.
                    var h = i == lines.Count - 1
                        ? Math.Max(contentHeightMm - usedMm, MinLineMm)
                        : lineHeightMm;
                    usedMm += h;
                    var indentMm = Math.Max(line.Indent, 0) * IndentStepMm;
                    var cell = col.Item().Height(h, Mm)
                        .PaddingLeft(LinePadMm + indentMm, Mm)
                        .PaddingRight(LinePadMm, Mm)
                        .AlignMiddle();
                    var text = cell.Text(display)
                        .FontFamily(line.Kind == LineKind.Section
                            ? PdfStyleDefaults.Body.SectionTitleFontFamily
                            : PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(Math.Max(fontSize - 0.5f, 6.5f));
                    if (line.Kind == LineKind.Section)
                        text.SemiBold();
                }
            });
        });
    }

    private static void ComposeSignature(
        IContainer container,
        string doctorName,
        string doctorUpdated,
        float border,
        float fontSize,
        IReadOnlyDictionary<string, string>? labels)
    {
        var nameLine = string.IsNullOrWhiteSpace(doctorName)
            ? HprpLabels.Get(labels, "doctorSignatureBlank", "(นพ/พญ. ........................................)")
            : HprpLabels.Get(labels, "doctorSignatureNamed", "(นพ/พญ. {0})")
                .Replace("{0}", doctorName.Trim(), StringComparison.Ordinal);

        container
            .BorderTop(border)
            .Padding(2, Mm)
            .AlignMiddle()
            .Column(col =>
            {
                col.Item().AlignCenter().Text(nameLine)
                    .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                    .FontSize(fontSize);
                if (!string.IsNullOrWhiteSpace(doctorUpdated))
                {
                    col.Item().AlignCenter().Text(doctorUpdated)
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(Math.Max(fontSize - 1f, 6f));
                }
                else
                {
                    col.Item().AlignCenter().Text("........................................")
                        .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                        .FontSize(fontSize);
                }
            });
    }

    private static IReadOnlyList<ContentLine> BuildDialysisLines(IReadOnlyList<LabelValue> fields)
    {
        var lines = new List<ContentLine>();
        foreach (var field in fields ?? [])
        {
            var label = (field.Label ?? string.Empty).Trim();
            var value = (field.Value ?? string.Empty).Trim();
            if (label.Length == 0 && value.Length == 0)
                continue;

            string text;
            if (label.Length == 0)
                text = value;
            else if (value.Length == 0)
                text = label;
            else
                text = $"{label} {value}";

            lines.Add(new ContentLine(text, LineKind.Text, Math.Max(field.Indent, 0)));
        }

        return lines;
    }

    private static IReadOnlyList<ContentLine> BuildMedicineLines(
        IReadOnlyList<string> prescription,
        IReadOnlyList<string> history,
        IReadOnlyDictionary<string, string>? labels)
    {
        var lines = new List<ContentLine>();
        var presc = (prescription ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var hist = (history ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

        if (presc.Count > 0)
        {
            lines.Add(new ContentLine(
                HprpLabels.Get(labels, "sectionMedPresc", "Medication"),
                LineKind.Section));
            foreach (var item in presc)
                lines.Add(new ContentLine(item, LineKind.CheckItem));
        }

        if (hist.Count > 0)
        {
            if (lines.Count > 0)
                lines.Add(new ContentLine(string.Empty, LineKind.Blank));
            lines.Add(new ContentLine(
                HprpLabels.Get(labels, "sectionMedHist", "Medication History"),
                LineKind.Section));
            foreach (var item in hist)
                lines.Add(new ContentLine(item, LineKind.Text));
        }

        return lines;
    }

    private static IReadOnlyList<ContentLine> PadLines(
        IReadOnlyList<ContentLine> lines,
        int count,
        LineKind blankKind)
    {
        if (lines.Count >= count)
            return lines.Take(count).ToList();

        var padded = new List<ContentLine>(count);
        padded.AddRange(lines);
        while (padded.Count < count)
            padded.Add(new ContentLine(string.Empty, blankKind));
        return padded;
    }
}

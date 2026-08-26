using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Rendering;

public sealed class QuestPdfRenderer : IPdfRenderer
{
    private readonly ILogger<QuestPdfRenderer>? _logger;
    private const int MaxPdfBytes = 50 * 1024 * 1024;

    public QuestPdfRenderer(ILogger<QuestPdfRenderer>? logger = null)
    {
        _logger = logger;
        FontRegistration.EnsureRegistered(logger);
    }

    public Task<byte[]> RenderAsync(object layoutSchema, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (layoutSchema is QuestLayout questLayout)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    if (questLayout.Landscape)
                    {
                        page.Size(PageSizes.A4.Landscape());
                    }
                    else
                    {
                        page.Size(PageSizes.A4);
                    }

                    var marginTop = questLayout.MarginTop ?? questLayout.MarginMillimeters;
                    var marginBottom = questLayout.MarginBottom ?? questLayout.MarginMillimeters;
                    var marginLeft = questLayout.MarginLeft ?? questLayout.MarginMillimeters;
                    var marginRight = questLayout.MarginRight ?? questLayout.MarginMillimeters;

                    page.MarginTop(marginTop, Unit.Millimetre);
                    page.MarginBottom(marginBottom, Unit.Millimetre);
                    page.MarginLeft(marginLeft, Unit.Millimetre);
                    page.MarginRight(marginRight, Unit.Millimetre);

                    page.DefaultTextStyle(t => t.FontFamily(PdfStyleDefaults.Fonts.PrimaryFamily));

                    if (questLayout.Header is not null)
                        page.Header().Element(questLayout.Header);

                    if (questLayout.Content is not null)
                        page.Content().Element(questLayout.Content);

                    page.Footer().Element(f =>
                    {
                        if (questLayout.Footer is not null)
                        {
                            questLayout.Footer.Invoke(f);
                        }
                        else
                        {
                            f.AlignRight().Text(t =>
                            {
                                t.CurrentPageNumber();
                                t.Span(" / ");
                                t.TotalPages();
                            });
                        }
                    });
                });
            });

            cancellationToken.ThrowIfCancellationRequested();
            return GeneratePdfAsync(document, questLayout.SectionHeaderBackground, cancellationToken);
        }

        if (layoutSchema is Action<IDocumentContainer> build)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = Document.Create(build);
            return GeneratePdfAsync(document, sectionHeaderBackground: null, cancellationToken);
        }

        throw new NotSupportedException(
            "layoutSchema must be QuestLayout or Action<IDocumentContainer> for QuestPdfRenderer");
    }

    private Task<byte[]> GeneratePdfAsync(
        Document document,
        string? sectionHeaderBackground,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var chrome = ReportSectionHeaderChrome.Begin(sectionHeaderBackground);
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            cancellationToken.ThrowIfCancellationRequested();

            if (stream.Length > MaxPdfBytes)
            {
                throw new InvalidOperationException(
                    $"PDF exceeds maximum size of {MaxPdfBytes / (1024 * 1024)}MB. " +
                    $"Generated PDF size: {stream.Length / (1024.0 * 1024.0):F2}MB");
            }

            return stream.ToArray();
        }, cancellationToken);
    }
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Headers;

public sealed class HemosheetHeaderSection : IReportHeaderSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var vm = (HemosheetReportViewModel)viewModel;
        var branding = context.Branding;
        var title = context.Metadata.Title ?? branding?.DisplayName ?? "Hemosheet";

        var logoBytes = PdfImageHelpers.LoadLogoFromDataUrl(vm.LogoBase64)
            ?? PdfImageHelpers.LoadLogoBytes(branding?.Header.LogoPath, branding?.Header.LogoUrl);

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(52);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1.4f);
            });

            table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(left =>
            {
                if (logoBytes is { Length: > 0 })
                {
                    left.Item().Element(c =>
                        PdfImageHelpers.RenderLogo(c, logoBytes, 48, 28));
                }

                if (branding?.Header.CompanyLines is { Count: > 0 } lines)
                {
                    foreach (var line in lines)
                    {
                        left.Item().Text(line)
                            .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                            .FontSize(PdfStyleDefaults.Body.DataFontSize);
                    }
                }
            });

            table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).AlignMiddle().AlignCenter()
                .Text(title)
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Header.TitleFontSize)
                .SemiBold();

            table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(right =>
            {
                right.Spacing(1);
                foreach (var line in HemosheetHeaderLines.BuildPatientMeta(vm, context))
                {
                    right.Item().AlignRight().Text(text =>
                        PdfTextHelpers.ComposeInlineLabelValue(text, line.Label, line.Value, showPlaceholderForEmpty: true));
                }
            });
        });
    }
}

using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using Hemo.Pdf.Sections.Preview.Hemosheet;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Footers;

public sealed class HemosheetFooterSection : IReportFooterSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        var vm = (HemosheetReportViewModel)viewModel;
        var branding = context.Branding;
        var disclaimer = branding?.Footer.DisclaimerText;
        var showPageNumber = branding?.Header.ShowPageNumber ?? true;
        var signatures = context.Signatures?.Signatures ?? [];
        var staffSlots = HemosheetPreviewMappers.MapStaffSignatureSlots(vm);
        var nursesLine = HemosheetPreviewMappers.BuildNursesInShiftLine(vm, vm.LayoutContext.Features);

        container.BorderTop(0.5f).PaddingTop(PdfSectionMetrics.CellPadding).Column(col =>
        {
            col.Spacing(2);

            if (signatures.Count > 0 || staffSlots.Count > 0)
            {
                col.Item().Row(row =>
                {
                    foreach (var signature in signatures)
                    {
                        row.RelativeItem().PaddingHorizontal(2).MinHeight(36).Column(slot =>
                        {
                            var label = string.IsNullOrWhiteSpace(signature.SignerRole)
                                ? "ลายเซ็น"
                                : signature.SignerRole;
                            PdfSignatureHelpers.RenderSignatureBlock(slot, signature, label, includeDate: false);
                        });
                    }

                    foreach (var slot in staffSlots)
                    {
                        row.RelativeItem().PaddingHorizontal(2).MinHeight(36).Column(column =>
                        {
                            column.Item().Text(slot.Role)
                                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                                .FontSize(PdfStyleDefaults.Body.DataFontSize)
                                .SemiBold();
                            column.Item().Height(24);
                            column.Item().PaddingHorizontal(12).Height(3).LineHorizontal(0.4f);
                            column.Item().AlignCenter().Text($"( {slot.Name ?? "—"} )")
                                .FontFamily(PdfStyleDefaults.Body.DataFontFamily)
                                .FontSize(PdfStyleDefaults.Body.DataFontSize);
                        });
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(nursesLine))
            {
                col.Item().Text(text =>
                    PdfTextHelpers.ComposeInlineLabelValue(text, "พยาบาลเวร", nursesLine));
            }

            col.Item().Row(row =>
            {
                if (!string.IsNullOrWhiteSpace(disclaimer))
                {
                    row.RelativeItem()
                        .AlignLeft()
                        .Text(disclaimer)
                        .FontSize(PdfStyleDefaults.Footer.TextFontSize);
                }
                else
                {
                    row.RelativeItem();
                }

                if (showPageNumber)
                {
                    row.ConstantItem(72)
                        .AlignRight()
                        .DefaultTextStyle(style => style.FontSize(PdfStyleDefaults.Footer.TextFontSize))
                        .Text(text =>
                        {
                            text.Span("หน้า ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                }
            });
        });
    }
}

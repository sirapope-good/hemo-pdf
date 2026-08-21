using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class PatientInfoSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is PatientInfoReportBlock block)
        {
            ComposeBlock(container, block, context);
            return;
        }

        if (viewModel is not IPatientInfoSource source)
        {
            return;
        }

        // Legacy adapter path (template-01 etc.) — keep until callers migrate to ReportBlock.
        ComposeBlock(container, new PatientInfoReportBlock
        {
            Title = "ข้อมูลผู้ป่วย",
            Columns =
            [
                [
                    new LabelValue { Label = "ชื่อ-สกุล", Value = source.PatientInfo.Name ?? "—" },
                    new LabelValue { Label = "HN", Value = source.PatientInfo.HospitalNumber ?? "—" },
                    new LabelValue { Label = "เลขบัตรประชาชน", Value = source.PatientInfo.IdentityNumber ?? "—" },
                ],
                [
                    new LabelValue { Label = "วันเกิด", Value = source.PatientInfo.DateOfBirth ?? "—" },
                    new LabelValue { Label = "เพศ", Value = source.PatientInfo.Gender ?? "—" },
                    new LabelValue { Label = "หน่วย", Value = source.PatientInfo.Unit ?? "—" },
                ],
            ],
        }, context);
    }

    public static void ComposeBlock(
        IContainer container,
        PatientInfoReportBlock block,
        PdfReportContext? context = null)
    {
        var columns = block.Columns.Count == 0 ? 2 : block.Columns.Count;
        var headerBg = ReportSectionHeaderChrome.Resolve(context, PdfSectionMetrics.SectionHeaderBackground);

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                for (var i = 0; i < columns; i++)
                {
                    def.RelativeColumn();
                }
            });

            if (!string.IsNullOrWhiteSpace(block.Title))
            {
                table.Cell().ColumnSpan((uint)columns)
                    .Background(headerBg)
                    .Border(0.5f)
                    .Padding(PdfSectionMetrics.SectionTitlePadding)
                    .Text(block.Title)
                    .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                    .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                    .SemiBold();
            }

            for (var i = 0; i < columns; i++)
            {
                var fields = i < block.Columns.Count ? block.Columns[i] : [];
                table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(col =>
                {
                    col.Spacing(1);
                    foreach (var field in fields)
                    {
                        col.Item().Text(t =>
                            PdfTextHelpers.ComposeInlineLabelValue(t, field.Label, field.Value));
                    }
                });
            }
        });
    }
}

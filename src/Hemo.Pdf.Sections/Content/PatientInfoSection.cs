using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Content;

public sealed class PatientInfoSection : IContentSection
{
    public void Compose(IContainer container, object viewModel, PdfReportContext context)
    {
        if (viewModel is not IPatientInfoSource source)
        {
            return;
        }

        var info = source.PatientInfo;

        container.Border(0.5f).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().ColumnSpan(2)
                .Background(PdfSectionMetrics.SectionHeaderBackground)
                .Border(0.5f)
                .Padding(PdfSectionMetrics.SectionTitlePadding)
                .Text("ข้อมูลผู้ป่วย")
                .FontFamily(PdfStyleDefaults.Body.SectionTitleFontFamily)
                .FontSize(PdfStyleDefaults.Body.SectionTitleFontSize)
                .SemiBold();

            table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(left =>
            {
                left.Spacing(1);
                left.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "ชื่อ-สกุล", info.Name));
                left.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "HN", info.HospitalNumber));
                left.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "เลขบัตรประชาชน", info.IdentityNumber));
            });

            table.Cell().Border(0.5f).Padding(PdfSectionMetrics.CellPadding).Column(right =>
            {
                right.Spacing(1);
                right.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "วันเกิด", info.DateOfBirth));
                right.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "เพศ", info.Gender));
                right.Item().Text(t => PdfTextHelpers.ComposeInlineLabelValue(t, "หน่วย", info.Unit));
            });
        });
    }
}

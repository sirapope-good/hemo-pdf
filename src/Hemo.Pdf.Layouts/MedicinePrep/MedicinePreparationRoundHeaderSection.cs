using System.Globalization;
using Hemo.Pdf.Core.Models.MedicinePreparation;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.MedicinePrep;

/// <summary>
/// Reusable round header (Unit / Date / Title / Round / Report code / Time) — no patient fields.
/// Used by live reports and blank templates alike.
/// </summary>
public sealed class MedicinePreparationRoundHeaderSection
{
    private const Unit Mm = Unit.Millimetre;

    public void Compose(
        IContainer container,
        string title,
        string reportCode,
        MedicinePreparationRoundHeader header,
        bool isTemplate)
    {
        container.Column(column =>
        {
            column.Spacing(1.5f);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Unit: ").Style(ThaiUrText.Bold);
                    text.Span(Blank(header.UnitName, isTemplate)).Style(ThaiUrText.Base);
                });
                row.RelativeItem().AlignCenter().Text(title).Style(ThaiUrText.Title);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Report code: ").Style(ThaiUrText.Bold);
                    text.Span(reportCode).Style(ThaiUrText.Base);
                });
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Date: ").Style(ThaiUrText.Bold);
                    text.Span(FormatDate(header, isTemplate)).Style(ThaiUrText.Base);
                });
                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Round: ").Style(ThaiUrText.Bold);
                    text.Span(FormatRound(header, isTemplate)).Style(ThaiUrText.Base);
                });
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Time: ").Style(ThaiUrText.Bold);
                    text.Span(FormatTime(header, isTemplate)).Style(ThaiUrText.Base);
                });
            });
        });
    }

    private static string FormatDate(MedicinePreparationRoundHeader header, bool isTemplate)
    {
        if (!string.IsNullOrWhiteSpace(header.DateTimeDisplay) && header.Date is null)
        {
            return header.DateTimeDisplay;
        }

        if (header.Date is { } date && date != default)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        return isTemplate ? "____/____/________" : "-";
    }

    private static string FormatRound(MedicinePreparationRoundHeader header, bool isTemplate)
    {
        if (!string.IsNullOrWhiteSpace(header.RoundName))
        {
            return header.RoundName;
        }

        if (header.SectionId > 0)
        {
            return header.SectionId.ToString(CultureInfo.InvariantCulture);
        }

        return isTemplate ? "____" : "-";
    }

    private static string FormatTime(MedicinePreparationRoundHeader header, bool isTemplate)
    {
        if (header.StartTime is { } start && header.EndTime is { } end)
        {
            return $"{start:HH\\:mm} - {end:HH\\:mm}";
        }

        return isTemplate ? "____:____ - ____:____" : "-";
    }

    private static string Blank(string? value, bool isTemplate) =>
        string.IsNullOrWhiteSpace(value)
            ? (isTemplate ? "____________________" : "-")
            : value;
}

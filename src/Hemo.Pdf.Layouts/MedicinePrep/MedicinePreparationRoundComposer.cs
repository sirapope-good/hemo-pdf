using System.Globalization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.MedicinePreparation;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.MedicinePrep;

public sealed class MedicinePreparationRoundComposer : ILayoutComposer
{
    private const Unit Mm = Unit.Millimetre;
    private const float BorderWidth = HemosheetThaiUrStyle.BorderWidth;
    private readonly MedicinePreparationRoundHeaderSection _headerSection = new();

    public object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (MedicinePreparationRoundReportViewModel)dataModel;
        return new QuestLayout
        {
            MarginMillimeters = 5f,
            MarginTop = 5f,
            MarginBottom = 5f,
            MarginLeft = 5f,
            MarginRight = 5f,
            Header = null,
            Content = container => ComposeContent(container, viewModel),
            Footer = null,
        };
    }

    private void ComposeContent(
        IContainer container,
        MedicinePreparationRoundReportViewModel viewModel)
    {
        container.Column(column =>
        {
            column.Spacing(2f);
            column.Item().Element(c =>
                _headerSection.Compose(
                    c,
                    viewModel.Title,
                    viewModel.ReportCode,
                    viewModel.Header,
                    viewModel.IsTemplate));
            column.Item().Element(c => ComposeTable(c, viewModel));
        });
    }

    private static void ComposeTable(
        IContainer container,
        MedicinePreparationRoundReportViewModel viewModel)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(10f, Mm);
                columns.RelativeColumn(2.3f);
                columns.RelativeColumn(3.2f);
                columns.RelativeColumn(1.5f);
            });

            HeaderCell(table, "No.");
            HeaderCell(table, "Patient information");
            HeaderCell(table, "Medication");
            HeaderCell(table, "Signature");

            var displayNo = 0;
            foreach (var patient in viewModel.Patients)
            {
                var medicines = patient.Medicines is { Count: > 0 }
                    ? patient.Medicines
                    : [new MedicinePreparationMedicine()];
                var rowSpan = (uint)medicines.Count;
                displayNo++;
                var noText = (patient.OrderNumber ?? displayNo).ToString(CultureInfo.InvariantCulture);

                for (var i = 0; i < medicines.Count; i++)
                {
                    if (i == 0)
                    {
                        DataCell(table, viewModel.IsTemplate ? "" : noText, center: true, rowSpan: rowSpan);
                        DataCell(table, FormatPatient(patient, viewModel.IsTemplate), rowSpan: rowSpan);
                    }

                    DataCell(table, FormatMedicine(medicines[i], viewModel.IsTemplate));
                    DataCell(table, FormatSignature(medicines[i], viewModel.IsTemplate));
                }
            }
        });
    }

    private static string FormatPatient(MedicinePreparationPatient patient, bool isTemplate)
    {
        if (isTemplate || string.IsNullOrWhiteSpace(patient.Name))
        {
            return string.Join(
                "\n",
                "____________________",
                "DOB: ____/____/________",
                "Allergy: ____________________",
                "Coverage: ____________________");
        }

        var birthDate = patient.BirthDate.HasValue
            ? patient.BirthDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : "-";
        var nameLine = string.IsNullOrWhiteSpace(patient.HospitalNumber)
            ? patient.Name
            : $"{patient.Name} ({patient.HospitalNumber})";
        return string.Join(
            "\n",
            nameLine,
            $"DOB: {birthDate}",
            $"Allergy: {Fallback(patient.Allergies)}",
            $"Coverage: {Fallback(patient.Coverage)}");
    }

    private static string FormatMedicine(MedicinePreparationMedicine medicine, bool isTemplate)
    {
        if (isTemplate || string.IsNullOrWhiteSpace(medicine.MedicineName))
        {
            return string.Join(
                "\n",
                "____________________",
                "Dose: ____________________",
                "Frequency: ____________________",
                "Route: ____________________");
        }

        var name = string.IsNullOrWhiteSpace(medicine.MedicineCode)
            ? medicine.MedicineName
            : $"{medicine.MedicineName} ({medicine.MedicineCode})";
        return string.Join(
            "\n",
            name,
            $"Dose: {Fallback(medicine.Dose)}",
            $"Frequency: {Fallback(medicine.Frequency)}",
            $"Route: {Fallback(medicine.Route)}");
    }

    private static string FormatSignature(MedicinePreparationMedicine medicine, bool isTemplate)
    {
        if (isTemplate)
        {
            return "Executed:\n____________________\nCosign:\n____________________";
        }

        var executed = FirstNonEmpty(medicine.ExecutedByName, medicine.SignatureNames.ElementAtOrDefault(0));
        var cosign = FirstNonEmpty(medicine.CosignedByName, medicine.SignatureNames.ElementAtOrDefault(1));
        return string.Join(
            "\n",
            "Executed:",
            string.IsNullOrWhiteSpace(executed) ? "-" : executed,
            "Cosign:",
            string.IsNullOrWhiteSpace(cosign) ? "-" : cosign);
    }

    private static string? FirstNonEmpty(string? primary, string? fallback) =>
        !string.IsNullOrWhiteSpace(primary) ? primary : fallback;

    private static string Fallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static void HeaderCell(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(BorderWidth)
            .Background(HemosheetThaiUrStyle.HeaderBackground)
            .Height(7f, Mm)
            .AlignMiddle()
            .AlignCenter()
            .PaddingHorizontal(1f)
            .Text(text)
            .Style(ThaiUrText.Bold);
    }

    private static void DataCell(
        TableDescriptor table,
        string text,
        bool center = false,
        uint rowSpan = 1)
    {
        var cell = table.Cell()
            .RowSpan(rowSpan)
            .Border(BorderWidth)
            .MinHeight(14f, Mm)
            .Padding(1.2f, Mm)
            .AlignMiddle();
        if (center)
        {
            cell = cell.AlignCenter();
        }
        cell.Text(text).Style(ThaiUrText.Base);
    }
}

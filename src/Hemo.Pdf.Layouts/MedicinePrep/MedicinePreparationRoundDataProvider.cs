using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models.MedicinePreparation;

namespace Hemo.Pdf.Layouts.MedicinePrep;

public sealed class MedicinePreparationRoundDataProvider : IReportDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };

    public Task<object> GetDataAsync(
        PdfReportContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Data is not JsonElement json || json.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "medicine-preparation-round requires trusted report-data from Web.Api.");
        }

        var source = JsonSerializer.Deserialize<WireModel>(json.GetRawText(), JsonOptions)
            ?? new WireModel();

        var header = source.Header ?? new WireHeader();
        var patients = (source.Patients ?? [])
            .Select(p => new MedicinePreparationPatient
            {
                PatientId = p.PatientId ?? string.Empty,
                OrderNumber = p.OrderNumber,
                HospitalNumber = p.HospitalNumber ?? string.Empty,
                Name = p.Name ?? string.Empty,
                BirthDate = p.BirthDate,
                Allergies = p.Allergies ?? string.Empty,
                Coverage = p.Coverage ?? string.Empty,
                Medicines = (p.Medicines ?? [])
                    .Select(m => new MedicinePreparationMedicine
                    {
                        PrescriptionId = m.PrescriptionId,
                        MedicineId = m.MedicineId,
                        MedicineName = m.MedicineName ?? string.Empty,
                        MedicineCode = m.MedicineCode ?? string.Empty,
                        Dose = m.Dose ?? string.Empty,
                        Frequency = m.Frequency ?? string.Empty,
                        Route = m.Route ?? string.Empty,
                        ExecutedByName = m.ExecutedByName ?? string.Empty,
                        CosignedByName = m.CosignedByName ?? string.Empty,
                        SignatureNames = m.SignatureNames ?? [],
                    })
                    .ToList(),
            })
            .ToList();

        var result = new MedicinePreparationRoundReportViewModel
        {
            Title = string.IsNullOrWhiteSpace(source.Title)
                ? "Medicine Preparation Round"
                : source.Title!,
            ReportCode = string.IsNullOrWhiteSpace(source.ReportCode)
                ? "MED-PRESC-RP-001"
                : source.ReportCode!,
            IsTemplate = source.IsTemplate,
            Header = new MedicinePreparationRoundHeader
            {
                UnitId = header.UnitId,
                UnitName = header.UnitName ?? string.Empty,
                Date = header.Date,
                SectionId = header.SectionId,
                RoundName = header.RoundName ?? string.Empty,
                StartTime = header.StartTime,
                EndTime = header.EndTime,
                DateTimeDisplay = header.DateTimeDisplay ?? string.Empty,
            },
            Patients = patients,
        };

        return Task.FromResult<object>(result);
    }

    private sealed class WireModel
    {
        public string? Title { get; set; }
        public string? ReportCode { get; set; }
        public bool IsTemplate { get; set; }
        public WireHeader? Header { get; set; }
        public List<WirePatient>? Patients { get; set; }
    }

    private sealed class WireHeader
    {
        public int UnitId { get; set; }
        public string? UnitName { get; set; }
        public DateOnly? Date { get; set; }
        public int SectionId { get; set; }
        public string? RoundName { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? DateTimeDisplay { get; set; }
    }

    private sealed class WirePatient
    {
        public string? PatientId { get; set; }
        public int? OrderNumber { get; set; }
        public string? HospitalNumber { get; set; }
        public string? Name { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Allergies { get; set; }
        public string? Coverage { get; set; }
        public List<WireMedicine>? Medicines { get; set; }
    }

    private sealed class WireMedicine
    {
        public Guid PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public string? MedicineName { get; set; }
        public string? MedicineCode { get; set; }
        public string? Dose { get; set; }
        public string? Frequency { get; set; }
        public string? Route { get; set; }
        public string? ExecutedByName { get; set; }
        public string? CosignedByName { get; set; }
        public List<string>? SignatureNames { get; set; }
    }
}

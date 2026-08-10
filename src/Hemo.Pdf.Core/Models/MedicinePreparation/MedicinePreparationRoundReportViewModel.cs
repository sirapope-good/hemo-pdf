namespace Hemo.Pdf.Core.Models.MedicinePreparation;

public sealed class MedicinePreparationRoundReportViewModel
{
    public string Title { get; init; } = "Medicine Preparation Round";
    public string ReportCode { get; init; } = "MED-PRESC-RP-001";

    /// <summary>Blank printable form (no patient identity).</summary>
    public bool IsTemplate { get; init; }

    public MedicinePreparationRoundHeader Header { get; init; } = new();
    public IReadOnlyList<MedicinePreparationPatient> Patients { get; init; } = [];
}

/// <summary>
/// Round chrome shared by live reports and blank templates (no person fields).
/// </summary>
public sealed class MedicinePreparationRoundHeader
{
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public DateOnly? Date { get; init; }
    public int SectionId { get; init; }
    public string RoundName { get; init; } = string.Empty;
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public string DateTimeDisplay { get; init; } = string.Empty;
}

public sealed class MedicinePreparationPatient
{
    public string PatientId { get; init; } = string.Empty;
    public int? OrderNumber { get; init; }
    public string HospitalNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateOnly? BirthDate { get; init; }
    public string Allergies { get; init; } = string.Empty;
    public string Coverage { get; init; } = string.Empty;
    public IReadOnlyList<MedicinePreparationMedicine> Medicines { get; init; } = [];
}

public sealed class MedicinePreparationMedicine
{
    public Guid PrescriptionId { get; init; }
    public int MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string MedicineCode { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string ExecutedByName { get; init; } = string.Empty;
    public string CosignedByName { get; init; } = string.Empty;
    public IReadOnlyList<string> SignatureNames { get; init; } = [];
}

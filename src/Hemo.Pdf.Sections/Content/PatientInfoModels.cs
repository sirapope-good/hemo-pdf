namespace Hemo.Pdf.Sections.Content;

public sealed class PatientInfoModel
{
    public string? Name { get; init; }
    public string? HospitalNumber { get; init; }
    public string? IdentityNumber { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Unit { get; init; }
}

public interface IPatientInfoSource
{
    PatientInfoModel PatientInfo { get; }
}

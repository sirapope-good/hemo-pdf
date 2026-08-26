namespace Hemo.Pdf.Core.Hprp;

/// <summary>Named data fetch adapters the engine already implements in C#.</summary>
public static class HprpDataAdapterIds
{
    public const string FlattenDto = "flatten-dto";
    public const string HemosheetRecord = "hemosheet-record";
    public const string Clinical01HctEpo = "clinical-01-hct-epo";
    public const string Clinical02EpoDrug = "clinical-02-epo-drug";
    public const string Clinical04Prescription = "clinical-04-prescription";
    public const string Clinical05ProgressNote = "clinical-05-progress-note";
    public const string Clinical05ProgressNoteChecklist = "clinical-05-progress-note-checklist";
    public const string Consent = "consent";
    public const string MedicinePreparationRound = "medicine-preparation-round";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        FlattenDto,
        HemosheetRecord,
        Clinical01HctEpo,
        Clinical02EpoDrug,
        Clinical04Prescription,
        Clinical05ProgressNote,
        Clinical05ProgressNoteChecklist,
        Consent,
        MedicinePreparationRound,
    };
}

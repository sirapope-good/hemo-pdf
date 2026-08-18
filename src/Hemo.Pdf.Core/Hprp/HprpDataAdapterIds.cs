namespace Hemo.Pdf.Core.Hprp;

/// <summary>Named data fetch adapters the engine already implements in C#.</summary>
public static class HprpDataAdapterIds
{
    public const string FlattenDto = "flatten-dto";
    public const string HemosheetRecord = "hemosheet-record";
    public const string Clinical01HctEpo = "clinical-01-hct-epo";
    public const string Clinical02EpoDrug = "clinical-02-epo-drug";
    public const string Clinical05ProgressNote = "clinical-05-progress-note";
    public const string Consent = "consent";
    public const string MedicinePreparationRound = "medicine-preparation-round";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        FlattenDto,
        HemosheetRecord,
        Clinical01HctEpo,
        Clinical02EpoDrug,
        Clinical05ProgressNote,
        Consent,
        MedicinePreparationRound,
    };
}

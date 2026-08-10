namespace Hemo.Pdf.Core.Models.Clinical;

/// <summary>
/// Medicine prescription frequency (MedRx), mirrored from Hemodialysis Pro
/// <c>Frequency</c> enum (BS / BW / BM / PRN / ST).
/// </summary>
public enum MedRxFrequency
{
    /// <summary>By session.</summary>
    BS = 0,

    /// <summary>By weeks.</summary>
    BW = -7,

    /// <summary>By months.</summary>
    BM = -28,

    /// <summary>Use when needed.</summary>
    PRN = 99,

    /// <summary>Use immediately.</summary>
    ST = -99,
}

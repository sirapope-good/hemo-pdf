using System.Globalization;
using Hemo.Pdf.Core.Models.Clinical;

namespace Hemo.Pdf.Core.Formatting;

/// <summary>
/// Formats MedRx schedule text to match the clinician UI
/// (<c>formatFrequencyText</c> / <c>getLoopDurationFormatted</c> in hemo-front).
/// Example: <c>2 dose within 1 week(s)</c>.
/// </summary>
public static class MedRxFrequencyFormatter
{
    public static string Format(
        MedRxFrequency frequency,
        int dosePerTarget,
        int targetLoopAmount,
        int limitDose = 0)
    {
        if (IsPrnOrStat(frequency))
            return FormatLoopDuration(frequency, dosePerTarget, targetLoopAmount, limitDose, forceNotCal: true);

        var duration = IsLimit(limitDose)
            ? FormatLoopDuration(frequency, dosePerTarget, targetLoopAmount, limitDose, forceNotCal: false)
            : FormatLoopDuration(frequency, dosePerTarget, targetLoopAmount, limitDose, forceNotCal: true);

        var limitNote = IsLimit(limitDose) ? $" · Limit {limitDose}" : string.Empty;
        return $"{dosePerTarget} dose {duration}{limitNote}";
    }

    private static string FormatLoopDuration(
        MedRxFrequency frequency,
        int dosePerTarget,
        int targetLoopAmount,
        int limitDose,
        bool forceNotCal)
    {
        var freqType = FormatFreqType(frequency, dosePerTarget, targetLoopAmount, limitDose);
        if (IsPrnOrStat(frequency))
            return freqType;

        var formatAsNoLimit = !IsLimit(limitDose) || forceNotCal;
        if (formatAsNoLimit)
            return $"within {targetLoopAmount} {freqType}";

        var totalDuration = GetTotalDuration(dosePerTarget, targetLoopAmount, limitDose);
        var isApproximate = IsDecimalDuration(dosePerTarget, targetLoopAmount, limitDose);
        var formatted = totalDuration.ToString("0.##", CultureInfo.InvariantCulture);
        var tilde = isApproximate ? "~" : string.Empty;
        return $"over {tilde}{formatted} {freqType}";
    }

    private static string FormatFreqType(
        MedRxFrequency frequency,
        int dosePerTarget,
        int targetLoopAmount,
        int limitDose)
    {
        if (frequency == MedRxFrequency.PRN)
            return "Use when needed";
        if (frequency == MedRxFrequency.ST)
            return "Use immediately";

        var typeOfLoop = frequency switch
        {
            MedRxFrequency.BS => "session",
            MedRxFrequency.BW => "week",
            MedRxFrequency.BM => "month",
            _ => "dose",
        };

        var totalDuration = GetTotalDuration(dosePerTarget, targetLoopAmount, limitDose);
        var plural =
            (totalDuration > 1 || (limitDose <= 1 && targetLoopAmount > 1))
            && frequency is MedRxFrequency.BS or MedRxFrequency.BW or MedRxFrequency.BM;

        return plural ? $"{typeOfLoop}(s)" : typeOfLoop;
    }

    private static float GetTotalDuration(int dosePerTarget, int targetLoopAmount, int limitDose)
    {
        if (!IsLimit(limitDose) || dosePerTarget <= 0)
            return targetLoopAmount;

        return (limitDose / (float)dosePerTarget) * targetLoopAmount;
    }

    private static bool IsDecimalDuration(int dosePerTarget, int targetLoopAmount, int limitDose)
    {
        if (dosePerTarget <= 0 || targetLoopAmount <= 0)
            return false;

        return limitDose % dosePerTarget != 0 || limitDose % targetLoopAmount != 0;
    }

    private static bool IsLimit(int limitDose) => limitDose >= 1;

    private static bool IsPrnOrStat(MedRxFrequency frequency) =>
        frequency is MedRxFrequency.PRN or MedRxFrequency.ST;
}

using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Constants;

/// <summary>
/// Tenant branding override for table / section header fills.
/// Ambient value is set for the QuestPDF render thread; call sites keep layout defaults as fallback.
/// </summary>
public static class ReportSectionHeaderChrome
{
    private static readonly AsyncLocal<string?> Ambient = new();

    public static IDisposable Begin(string? sectionHeaderBackground)
    {
        var previous = Ambient.Value;
        Ambient.Value = Normalize(sectionHeaderBackground);
        return new Restore(previous);
    }

    public static string Resolve(string fallback) =>
        Ambient.Value ?? fallback;

    public static string Resolve(PdfReportContext? context, string fallback) =>
        Normalize(context?.Branding?.Style?.SectionHeaderBackground) ?? Ambient.Value ?? fallback;

    public static string? FromBranding(CustomerBrandingProfile? branding) =>
        Normalize(branding?.Style?.SectionHeaderBackground);

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length is not (7 or 9) || trimmed[0] != '#')
            return null;

        for (var i = 1; i < trimmed.Length; i++)
        {
            if (!Uri.IsHexDigit(trimmed[i]))
                return null;
        }

        return trimmed.ToUpperInvariant();
    }

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}

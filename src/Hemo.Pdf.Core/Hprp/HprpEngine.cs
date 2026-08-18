namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Compatibility window for <c>.hprp</c> packages.
/// Packages with <see cref="HprpManifest.EngineVersion"/> higher than
/// <see cref="CurrentVersion"/> are rejected; older versions stay readable.
/// </summary>
public static class HprpEngine
{
    public const int CurrentVersion = 1;
    public const int MinSupportedVersion = 1;
    public const string FileExtension = ".hprp";
}

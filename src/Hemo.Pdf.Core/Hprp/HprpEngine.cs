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

    /// <summary>Packed ZIP packages at repo root (<c>packages/*.hprp</c>).</summary>
    public const long MaxPackageBytes = 2 * 1024 * 1024;
}

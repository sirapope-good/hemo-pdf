using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Application.Hprp;

internal static class HprpDiskPaths
{
    public static string ResolveExistingOrConfigured(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return "";

        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        foreach (var candidate in Candidates(configuredPath))
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Candidates(configuredPath)[0];
    }

    /// <summary>Studio writes to repo <c>packages/</c> when the solution is found; otherwise the resolved packages path.</summary>
    public static string ResolvePackagesWriteRoot(HprpTemplateOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PackagesWritePath))
            return Path.GetFullPath(options.PackagesWritePath);

        var repo = HprpTemplatePaths.FindRepoRoot(
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory);
        if (!string.IsNullOrWhiteSpace(repo))
            return Path.Combine(repo, HprpTemplatePaths.PackagesFolder);

        var resolved = ResolveExistingOrConfigured(options.PackagesRootPath);
        return string.IsNullOrWhiteSpace(resolved)
            ? Path.GetFullPath(HprpTemplatePaths.PackagesFolder)
            : resolved;
    }

    private static string[] Candidates(string configuredPath) =>
    [
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath)),
    ];
}

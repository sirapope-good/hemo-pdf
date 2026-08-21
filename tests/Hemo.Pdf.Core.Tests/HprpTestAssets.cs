using Hemo.Pdf.Core.Hprp;

namespace Hemo.Pdf.Core.Tests;

internal static class HprpTestAssets
{
    public static string TemplatesRoot()
    {
        var rooted = Path.Combine(AppContext.BaseDirectory, "assets", "templates");
        if (HasReports(rooted))
            return rooted;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "templates");
            if (HasReports(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("assets/templates/reports not found.");
    }

    public static string PackageDir(string templateId, string? variant = null)
    {
        var reportDir = Path.Combine(TemplatesRoot(), HprpTemplatePaths.ReportsFolder, templateId);
        if (!string.IsNullOrWhiteSpace(variant)
            || Directory.Exists(Path.Combine(reportDir, HprpTemplatePaths.VariantsFolder)))
        {
            return Path.Combine(
                reportDir,
                HprpTemplatePaths.VariantsFolder,
                HprpTemplatePaths.NormalizeVariant(variant));
        }

        return reportDir;
    }

    private static bool HasReports(string templatesRoot) =>
        Directory.Exists(Path.Combine(templatesRoot, HprpTemplatePaths.ReportsFolder, "clinical-01-hct-epo"));
}

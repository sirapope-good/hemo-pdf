using Microsoft.Extensions.Logging;
using QuestPDF.Drawing;

namespace Hemo.Pdf.Rendering;

public static class FontRegistration
{
    private static bool _fontsRegistered;
    private static readonly object Lock = new();

    public static void EnsureRegistered(ILogger? logger = null)
    {
        if (_fontsRegistered)
            return;

        lock (Lock)
        {
            if (_fontsRegistered)
                return;

            var baseDir = AppContext.BaseDirectory;
            var fontsDirCandidates = new[]
            {
                Path.Combine(baseDir, "Fonts", "sarabun"),
                Path.Combine(baseDir, "assets", "fonts", "sarabun"),
                Path.GetFullPath(Path.Combine(baseDir, "../../assets/fonts/sarabun"))
            };

            var fontsDir = fontsDirCandidates.FirstOrDefault(Directory.Exists);
            if (fontsDir is not null)
            {
                var fontFiles = Directory
                    .EnumerateFiles(fontsDir, "Sarabun-*.ttf", SearchOption.AllDirectories)
                    .ToList();

                if (fontFiles.Count > 0)
                {
                    var registeredCount = 0;
                    foreach (var path in fontFiles)
                    {
                        try
                        {
                            using var stream = File.OpenRead(path);
                            FontManager.RegisterFont(stream);
                            registeredCount++;
                            logger?.LogDebug("Registered font file: {FontPath}", path);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Failed to register font file: {FontPath}", path);
                        }
                    }

                    logger?.LogInformation(
                        "Successfully registered {Count} Sarabun font file(s) from {FontsDir}",
                        registeredCount,
                        fontsDir);
                }
                else
                {
                    logger?.LogWarning(
                        "Font directory found but no Sarabun font files (*.ttf) were found in {FontsDir}",
                        fontsDir);
                }
            }
            else
            {
                var searchedPaths = string.Join(", ", fontsDirCandidates);
                logger?.LogWarning(
                    "Sarabun font directory not found. Searched paths: {SearchedPaths}. PDF generation will use default fonts.",
                    searchedPaths);
            }

            _fontsRegistered = true;
        }
    }
}

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Sections.Helpers;

public static class PdfImageHelpers
{
    public static byte[]? LoadLogoBytes(string? logoPath, string? logoUrl)
    {
        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            var resolvedPath = ResolvePath(logoPath);
            if (File.Exists(resolvedPath))
            {
                return File.ReadAllBytes(resolvedPath);
            }
        }

        if (!string.IsNullOrWhiteSpace(logoUrl) && Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    return client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                }
                catch
                {
                    return null;
                }
            }

            var localPath = ResolvePath(logoUrl);
            if (File.Exists(localPath))
            {
                return File.ReadAllBytes(localPath);
            }
        }

        return null;
    }

    public static void RenderLogo(IContainer container, byte[]? logoBytes, float width, float height)
    {
        if (logoBytes is null or { Length: 0 })
        {
            return;
        }

        container
            .MaxWidth(width)
            .MaxHeight(height)
            .AlignLeft()
            .Image(logoBytes);
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, path),
            Path.Combine(Directory.GetCurrentDirectory(), path),
            Path.Combine(AppContext.BaseDirectory, "..", path),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, path);
    }
}

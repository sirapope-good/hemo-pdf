using Hemo.Pdf.Application.Hprp;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hemo.Pdf.Core.Tests;

/// <summary>Re-packs repo <c>packages/</c> from <c>assets/templates</c> (keeps .hprp in sync with disk layouts).</summary>
public class HprpPackRepoPackagesTests
{
    [Fact]
    public async Task PackAll_WritesRepoPackagesFolder()
    {
        var templates = HprpTestAssets.TemplatesRoot();
        var packages = FindRepoPackagesRoot(templates);
        Assert.True(Directory.Exists(packages), packages);

        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = templates,
            PackagesRootPath = packages,
            PackagesWritePath = packages,
            EnableHprpStudioWrite = true,
        });
        var store = new FileHprpTemplateStore(options);
        var pack = new HprpPackService(options, store);

        var results = await pack.PackAllFromTemplatesAsync();
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.TemplateId.Contains("clinical-03", StringComparison.Ordinal)
            && r.Variant.Equals("thaiur", StringComparison.OrdinalIgnoreCase));
        Assert.All(results, r => Assert.True(File.Exists(r.OutputPath), r.OutputPath));
    }

    private static string FindRepoPackagesRoot(string templatesRoot)
    {
        var dir = new DirectoryInfo(templatesRoot);
        while (dir is not null)
        {
            var packages = Path.Combine(dir.FullName, "packages");
            if (File.Exists(Path.Combine(packages, "clinical-01-hct-epo.hprp"))
                || File.Exists(Path.Combine(packages, "clinical-03-hemodialysis-record.thaiur.hprp")))
            {
                return packages;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("repo packages/ folder not found from " + templatesRoot);
    }
}

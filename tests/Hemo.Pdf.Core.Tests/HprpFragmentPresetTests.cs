using Hemo.Pdf.Application.Hprp;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Core.Tests;

public class HprpFragmentPresetTests
{
    [Fact]
    public void Validator_RejectsBesideAsFirstElement()
    {
        var frag = new HprpFragmentPreset
        {
            Id = "bad",
            DisplayName = "Bad",
            Elements =
            [
                new HprpDesignerElement
                {
                    Id = "a",
                    Type = HprpDesignerElementTypes.BoxText,
                    Place = "beside",
                    Text = "x",
                },
            ],
        };

        var errors = HprpFragmentValidator.Validate(frag);
        Assert.Contains(errors, e => e.Contains("beside", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_AcceptsCopayShape()
    {
        var frag = new HprpFragmentPreset
        {
            Id = "copay-duo-v1",
            DisplayName = "Co-pay",
            Tags = ["clinical", "copay"],
            Elements =
            [
                new HprpDesignerElement
                {
                    Id = "copay-banner",
                    Type = HprpDesignerElementTypes.BoxText,
                    Place = "below",
                    Text = "title",
                },
                new HprpDesignerElement
                {
                    Id = "copay-nhso",
                    Type = HprpDesignerElementTypes.ConfigTable,
                    Place = "below",
                    PresetId = "copay-nhso-v1",
                },
                new HprpDesignerElement
                {
                    Id = "copay-sso",
                    Type = HprpDesignerElementTypes.ConfigTable,
                    Place = "beside",
                    PresetId = "copay-sso-v1",
                },
            ],
        };

        Assert.Empty(HprpFragmentValidator.Validate(frag));
    }

    [Fact]
    public void Store_LoadsSeedCopayDuo()
    {
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = Path.Combine(HprpTestAssets.TemplatesRoot(), "_no-packages"),
        });
        var store = new HprpFragmentPresetStore(options);
        var frag = store.TryGet("copay-duo-v1");
        Assert.NotNull(frag);
        Assert.Equal(3, frag!.Elements.Count);
        Assert.Empty(HprpFragmentValidator.Validate(frag));
        Assert.Contains(frag.Tags, t => t == "copay");
    }

    [Fact]
    public void HeaderStore_ListsThaiUr()
    {
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = Path.Combine(HprpTestAssets.TemplatesRoot(), "_no-packages"),
        });
        var store = new HprpHeaderPresetStore(options);
        var hdr = store.TryGet("clinical-header-thaiur");
        Assert.NotNull(hdr);
        Assert.Equal("clinical-header-thaiur", hdr!.Id);
        Assert.Contains("ThaiUr", hdr.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HeaderStore_ResolvesLegacyAlias()
    {
        var options = Options.Create(new HprpTemplateOptions
        {
            RootPath = HprpTestAssets.TemplatesRoot(),
            PackagesRootPath = Path.Combine(HprpTestAssets.TemplatesRoot(), "_no-packages"),
        });
        var store = new HprpHeaderPresetStore(options);
        var hdr = store.TryGet("thaiur-header-v1");
        Assert.NotNull(hdr);
        Assert.Equal("clinical-header-thaiur", hdr!.Id);
    }

    [Fact]
    public async Task HeaderStore_SaveWritesLibraryFolder()
    {
        var packagesRoot = Path.Combine(Path.GetTempPath(), "hprp-lib-headers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packagesRoot);
        try
        {
            var options = Options.Create(new HprpTemplateOptions
            {
                RootPath = HprpTestAssets.TemplatesRoot(),
                PackagesRootPath = packagesRoot,
                PackagesWritePath = packagesRoot,
            });
            var store = new HprpHeaderPresetStore(options);
            var seed = store.TryGet("clinical-header-thaiur");
            Assert.NotNull(seed);

            var json = System.Text.Json.JsonSerializer.Serialize(seed, HprpJson.Options);
            json = json
                .Replace("\"displayName\":\"Clinical header ThaiUr\"", "\"displayName\":\"Clinical header ThaiUr (edited)\"", StringComparison.Ordinal)
                .Replace("\"displayName\": \"Clinical header ThaiUr\"", "\"displayName\": \"Clinical header ThaiUr (edited)\"", StringComparison.Ordinal);
            var edited = System.Text.Json.JsonSerializer.Deserialize<Hemo.Pdf.Core.Hprp.Header.HprpHeaderPreset>(json, HprpJson.Options)!;
            await store.SaveAsync(edited);

            var path = Path.Combine(packagesRoot, "library", "headers", "clinical-header-thaiur.json");
            Assert.True(File.Exists(path), "expected write under PackagesWritePath: " + path);
            var again = store.TryGet("clinical-header-thaiur");
            Assert.Equal("Clinical header ThaiUr (edited)", again!.DisplayName);
        }
        finally
        {
            try { Directory.Delete(packagesRoot, recursive: true); } catch { /* ignore */ }
        }
    }
}

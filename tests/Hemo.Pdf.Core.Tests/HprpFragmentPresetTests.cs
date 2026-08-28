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

    [Fact]
    public async Task HeaderStore_DeleteLibraryRemovesOverrideOnly()
    {
        var packagesRoot = Path.Combine(Path.GetTempPath(), "hprp-lib-del-" + Guid.NewGuid().ToString("N"));
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

            // Library-only preset
            await store.SaveAsync(new Hemo.Pdf.Core.Hprp.Header.HprpHeaderPreset
            {
                Id = "clinical-header-thaiur2",
                DisplayName = "Clinical header ThaiUr 2",
                Tags = ["clinical", "thaiur"],
            });
            Assert.True(store.IsInLibrary("clinical-header-thaiur2"));
            var del = store.DeleteLibrary("clinical-header-thaiur2");
            Assert.True(del.Ok);
            Assert.False(del.FellBackToSeed);
            Assert.Null(store.TryGet("clinical-header-thaiur2"));

            // Seed-only cannot delete
            var seedOnly = store.DeleteLibrary("clinical-header-thaiur");
            Assert.True(seedOnly.IsSeedOnly);

            // Override then delete → fall back to seed
            var seed = store.TryGet("clinical-header-thaiur")!;
            var json = System.Text.Json.JsonSerializer.Serialize(seed, HprpJson.Options);
            json = json
                .Replace("\"displayName\":\"Clinical header ThaiUr\"", "\"displayName\":\"override\"", StringComparison.Ordinal)
                .Replace("\"displayName\": \"Clinical header ThaiUr\"", "\"displayName\": \"override\"", StringComparison.Ordinal);
            var edited = System.Text.Json.JsonSerializer.Deserialize<Hemo.Pdf.Core.Hprp.Header.HprpHeaderPreset>(json, HprpJson.Options)!;
            await store.SaveAsync(edited);
            Assert.Equal("override", store.TryGet("clinical-header-thaiur")!.DisplayName);
            var delOverride = store.DeleteLibrary("clinical-header-thaiur");
            Assert.True(delOverride.Ok);
            Assert.True(delOverride.FellBackToSeed);
            Assert.Contains("ThaiUr", store.TryGet("clinical-header-thaiur")!.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(packagesRoot, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task TableStore_SaveAndDeleteLibrary()
    {
        var packagesRoot = Path.Combine(Path.GetTempPath(), "hprp-lib-tables-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packagesRoot);
        try
        {
            var options = Options.Create(new HprpTemplateOptions
            {
                RootPath = HprpTestAssets.TemplatesRoot(),
                PackagesRootPath = packagesRoot,
                PackagesWritePath = packagesRoot,
            });
            var store = new HprpTablePresetStore(options);
            Assert.NotNull(store.TryGet("hct-epo-annual-v1"));

            await store.SaveAsync(new Hemo.Pdf.Core.Hprp.Table.HprpTablePreset
            {
                Id = "my-table-lib-v1",
                DisplayName = "My table",
                Tags = ["test"],
            });
            Assert.True(File.Exists(Path.Combine(packagesRoot, "library", "tables", "my-table-lib-v1.json")));
            Assert.True(store.IsInLibrary("my-table-lib-v1"));

            var del = store.DeleteLibrary("my-table-lib-v1");
            Assert.True(del.Ok);
            Assert.Null(store.TryGet("my-table-lib-v1"));

            var seedOnly = store.DeleteLibrary("hct-epo-annual-v1");
            Assert.True(seedOnly.IsSeedOnly);
        }
        finally
        {
            try { Directory.Delete(packagesRoot, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task FragmentStore_SaveAndDeleteLibrary()
    {
        var packagesRoot = Path.Combine(Path.GetTempPath(), "hprp-lib-frags-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packagesRoot);
        try
        {
            var options = Options.Create(new HprpTemplateOptions
            {
                RootPath = HprpTestAssets.TemplatesRoot(),
                PackagesRootPath = packagesRoot,
                PackagesWritePath = packagesRoot,
            });
            var store = new HprpFragmentPresetStore(options);
            Assert.NotNull(store.TryGet("copay-duo-v1"));

            await store.SaveAsync(new HprpFragmentPreset
            {
                Id = "my-frag-lib-v1",
                DisplayName = "My frag",
                Tags = ["test"],
                Elements =
                [
                    new Hemo.Pdf.Core.Hprp.Table.HprpDesignerElement
                    {
                        Id = "a",
                        Type = Hemo.Pdf.Core.Hprp.Table.HprpDesignerElementTypes.BoxText,
                        Place = "below",
                        Text = "x",
                    },
                ],
            });
            Assert.True(File.Exists(Path.Combine(packagesRoot, "library", "fragments", "my-frag-lib-v1.json")));

            var del = store.DeleteLibrary("my-frag-lib-v1");
            Assert.True(del.Ok);
            Assert.Null(store.TryGet("my-frag-lib-v1"));

            var seedOnly = store.DeleteLibrary("copay-duo-v1");
            Assert.True(seedOnly.IsSeedOnly);
        }
        finally
        {
            try { Directory.Delete(packagesRoot, recursive: true); } catch { /* ignore */ }
        }
    }
}

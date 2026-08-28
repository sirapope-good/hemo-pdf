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
        var hdr = store.TryGet("thaiur-header-v1");
        Assert.NotNull(hdr);
        Assert.False(string.IsNullOrWhiteSpace(hdr!.DisplayName));
    }
}

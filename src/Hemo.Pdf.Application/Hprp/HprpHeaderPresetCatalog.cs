using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Hprp.Header;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpHeaderPresetCatalog : IHprpHeaderPresetCatalog
{
    private readonly HprpHeaderPresetStore _store;

    public HprpHeaderPresetCatalog(HprpHeaderPresetStore store)
    {
        _store = store;
    }

    public IReadOnlyDictionary<string, HprpHeaderPreset> LoadAll() => _store.LoadDictionary();

    public HprpHeaderPreset? TryGet(string presetId) => _store.TryGet(presetId);
}

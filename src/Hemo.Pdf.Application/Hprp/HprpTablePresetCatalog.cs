using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpTablePresetCatalog : IHprpTablePresetCatalog
{
    private readonly HprpTablePresetStore _store;

    public HprpTablePresetCatalog(HprpTablePresetStore store)
    {
        _store = store;
    }

    public IReadOnlyDictionary<string, HprpTablePreset> LoadAll() => _store.LoadDictionary();

    public HprpTablePreset? TryGet(string presetId) => _store.TryGet(presetId);
}

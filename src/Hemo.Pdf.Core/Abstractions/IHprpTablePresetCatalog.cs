using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Abstractions;

public interface IHprpTablePresetCatalog
{
    IReadOnlyDictionary<string, HprpTablePreset> LoadAll();

    HprpTablePreset? TryGet(string presetId);
}

using Hemo.Pdf.Core.Hprp.Header;

namespace Hemo.Pdf.Core.Abstractions;

public interface IHprpHeaderPresetCatalog
{
    IReadOnlyDictionary<string, HprpHeaderPreset> LoadAll();

    HprpHeaderPreset? TryGet(string presetId);
}

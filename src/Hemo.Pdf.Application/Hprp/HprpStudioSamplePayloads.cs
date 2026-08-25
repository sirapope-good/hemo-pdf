using System.Text.Json;
using Hemo.Pdf.Core.Constants;

namespace Hemo.Pdf.Application.Hprp;

public static class HprpStudioSamplePayloads
{
    public static readonly IReadOnlyList<string> KnownTemplateIds = [ClinicalReportCatalog.HctEpo];

    public static JsonElement? TryLoad(string templatesRoot, string templateId)
    {
        if (string.IsNullOrWhiteSpace(templatesRoot) || string.IsNullOrWhiteSpace(templateId))
            return null;

        var id = ClinicalReportCatalog.ResolveEngineTemplateId(templateId);
        var path = Path.Combine(templatesRoot, "reports", id, "sample.json");
        if (!File.Exists(path))
            return null;

        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        return doc.RootElement.Clone();
    }
}

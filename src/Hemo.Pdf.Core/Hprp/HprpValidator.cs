using System.Text.Json;

namespace Hemo.Pdf.Core.Hprp;

public static class HprpValidator
{
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "code", "eval", "csharp", "javascript", "lambda",
    };

    public static HprpValidationResult Validate(HprpPackage package)
    {
        var errors = new List<string>();
        ValidateManifest(package.Manifest, errors);
        ValidateLayout(package.Layout, errors);
        return new HprpValidationResult { Errors = errors };
    }

    public static HprpValidationResult Validate(HprpManifest manifest, HprpLayout layout)
    {
        var errors = new List<string>();
        ValidateManifest(manifest, errors);
        ValidateLayout(layout, errors);
        return new HprpValidationResult { Errors = errors };
    }

    private static void ValidateManifest(HprpManifest manifest, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
            errors.Add("manifest.id is required.");

        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            errors.Add("manifest.displayName is required.");

        if (manifest.EngineVersion < HprpEngine.MinSupportedVersion)
            errors.Add($"manifest.engineVersion {manifest.EngineVersion} is below minimum {HprpEngine.MinSupportedVersion}.");

        if (manifest.EngineVersion > HprpEngine.CurrentVersion)
            errors.Add($"manifest.engineVersion {manifest.EngineVersion} is newer than engine {HprpEngine.CurrentVersion}.");

        if (!HprpDataAdapterIds.All.Contains(manifest.DataAdapter))
            errors.Add($"Unknown dataAdapter '{manifest.DataAdapter}'.");

        if (!string.IsNullOrWhiteSpace(manifest.LayoutKind)
            && !HprpLayoutKinds.All.Contains(manifest.LayoutKind))
        {
            errors.Add($"Unknown layoutKind '{manifest.LayoutKind}'.");
        }

        if (manifest.Ui is not null)
            ValidateUi(manifest.Ui, errors);
    }

    private static void ValidateUi(HprpManifestUi ui, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(ui.Role) && !HprpManifestUi.Roles.Contains(ui.Role))
            errors.Add($"manifest.ui.role '{ui.Role}' is not supported.");

        if (!HprpManifestUi.EntryModes.Contains(ui.EntryMode))
            errors.Add($"manifest.ui.entryMode '{ui.EntryMode}' is not supported.");

        for (var i = 0; i < ui.Parameters.Count; i++)
        {
            var param = ui.Parameters[i];
            var path = $"manifest.ui.parameters[{i}]";
            if (string.IsNullOrWhiteSpace(param.Name))
                errors.Add($"{path}.name is required.");

            if (!HprpManifestUi.ParameterSources.Contains(param.Source))
                errors.Add($"{path}.source '{param.Source}' is not supported.");

            if (string.Equals(param.Source, "default", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(param.Generator)
                && !HprpManifestUi.Generators.Contains(param.Generator))
            {
                errors.Add($"{path}.generator '{param.Generator}' is not supported.");
            }

            if (string.Equals(param.Source, "constant", StringComparison.OrdinalIgnoreCase)
                && param.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                errors.Add($"{path}.value is required when source is constant.");
            }
        }
    }

    private static void ValidateLayout(HprpLayout layout, List<string> errors)
    {
        if (layout.Header is not null)
            ValidateNode(layout.Header, "header", errors);

        for (var i = 0; i < layout.Body.Count; i++)
            ValidateNode(layout.Body[i], $"body[{i}]", errors);

        for (var i = 0; i < layout.Sections.Count; i++)
            ValidateSection(layout.Sections[i], $"sections[{i}]", errors);
    }

    private static void ValidateNode(HprpLayoutNode node, string path, List<string> errors)
    {
        RejectForbidden(node.When, path + ".when", errors);
        RejectForbidden(node.Title, path + ".title", errors);
        RejectForbidden(node.Content, path + ".content", errors);

        var hasType = !string.IsNullOrWhiteSpace(node.Type);
        var hasWidget = !string.IsNullOrWhiteSpace(node.Widget);
        if (!hasType && !hasWidget)
            errors.Add($"{path} must have type or widget.");

        if (hasType && !HprpWidgetIds.BlockTypes.Contains(node.Type!))
            errors.Add($"{path} unknown block type '{node.Type}'.");

        if (hasWidget && !HprpWidgetIds.All.Contains(node.Widget!))
            errors.Add($"{path} unknown widget '{node.Widget}'.");

        HprpChrome.Validate(node.Chrome, path + ".chrome", errors);
        ValidateColumnPlan(node, path, errors);
    }

    private static void ValidateColumnPlan(HprpLayoutNode node, string path, List<string> errors)
    {
        if (node.ColumnPlan is not { Count: > 0 })
            return;

        var recipe = HprpWidgetRecipes.TryGet(node.Widget);
        if (recipe is null || recipe.BindFields.Count == 0)
        {
            errors.Add($"{path}.columnPlan is only valid on widgets that declare bindFields.");
            return;
        }

        for (var i = 0; i < node.ColumnPlan.Count; i++)
        {
            var col = node.ColumnPlan[i];
            var itemPath = $"{path}.columnPlan[{i}]";
            if (col.Weight is <= 0)
                errors.Add($"{itemPath}.weight must be greater than 0.");

            var bind = col.Bind?.Trim();
            if (string.IsNullOrEmpty(bind))
                continue;

            if (!recipe.AllowsBind(bind))
                errors.Add($"{itemPath}.bind '{bind}' is not in the widget recipe.");
        }
    }

    private static void ValidateSection(HprpSectionNode section, string path, List<string> errors)
    {
        RejectForbidden(section.When, path + ".when", errors);

        if (string.IsNullOrWhiteSpace(section.Widget))
        {
            errors.Add($"{path}.widget is required.");
            return;
        }

        if (!HprpWidgetIds.All.Contains(section.Widget)
            && !HprpWidgetIds.TryMapHemosheetSection(section.Widget, out _))
        {
            errors.Add($"{path} unknown widget '{section.Widget}'.");
        }

        HprpChrome.Validate(section.Chrome, path + ".chrome", errors);
    }

    private static void RejectForbidden(JsonElement element, string path, List<string> errors)
    {
        if (element.ValueKind == JsonValueKind.Undefined || element.ValueKind == JsonValueKind.Null)
            return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ForbiddenKeys.Contains(property.Name))
                    errors.Add($"{path} must not contain executable key '{property.Name}'.");

                RejectForbidden(property.Value, path, errors);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                RejectForbidden(item, path, errors);
        }
    }
}

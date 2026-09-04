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
        if (HprpLayoutModes.IsDesigner(package.Manifest))
            ValidateDesignerLayout(package.Layout, errors);
        else if (HprpLayoutModes.IsAbsolute(package.Manifest))
            ValidateAbsoluteLayout(package.Layout, errors);
        else
            ValidateLayout(package.Layout, errors);
        HprpPageLayout.Validate(package.Layout.Page, errors);
        return new HprpValidationResult { Errors = errors };
    }

    public static HprpValidationResult Validate(HprpManifest manifest, HprpLayout layout)
    {
        var errors = new List<string>();
        ValidateManifest(manifest, errors);
        if (HprpLayoutModes.IsDesigner(manifest))
            ValidateDesignerLayout(layout, errors);
        else if (HprpLayoutModes.IsAbsolute(manifest))
            ValidateAbsoluteLayout(layout, errors);
        else
            ValidateLayout(layout, errors);
        HprpPageLayout.Validate(layout.Page, errors);
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

        if (!string.IsNullOrWhiteSpace(manifest.LayoutMode)
            && !HprpLayoutModes.All.Contains(manifest.LayoutMode))
        {
            errors.Add($"Unknown layoutMode '{manifest.LayoutMode}'.");
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

    private static void ValidateAbsoluteLayout(HprpLayout layout, List<string> errors)
    {
        if (layout.Widgets.Count == 0)
            errors.Add("layout.widgets must contain at least one widget when layoutMode is absolute.");

        for (var i = 0; i < layout.Widgets.Count; i++)
        {
            var w = layout.Widgets[i];
            var path = $"layout.widgets[{i}]";
            if (string.IsNullOrWhiteSpace(w.Id))
                errors.Add($"{path}.id is required.");

            ValidateAbsoluteWidgetType(w, path, errors);
            HprpChrome.Validate(w.Chrome, path + ".chrome", errors);
            ValidateAbsoluteColumnPlan(w, path, errors);

            if (w.WMm <= 0 || w.HMm <= 0)
                errors.Add($"{path} wMm/hMm must be > 0.");
            if (w.XMm < 0 || w.YMm < 0 || w.XMm > 400 || w.YMm > 400)
                errors.Add($"{path} xMm/yMm out of range.");
            if (w.WMm > 400 || w.HMm > 400)
                errors.Add($"{path} wMm/hMm out of range.");
        }
    }

    private static void ValidateAbsoluteWidgetType(HprpAbsoluteWidget w, string path, List<string> errors)
    {
        var type = w.Type?.Trim() ?? "";
        if (HprpAbsoluteWidget.PrimitiveTypes.Contains(type))
            return;

        if (string.Equals(type, HprpAbsoluteWidget.TypeDense, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(w.Widget) || !HprpWidgetIds.All.Contains(w.Widget))
                errors.Add($"{path}.widget must be a known dense widget id when type is dense.");
            return;
        }

        // Compact form: type = known dense widget id (optional .widget mirror).
        if (HprpWidgetIds.All.Contains(type))
        {
            if (!string.IsNullOrWhiteSpace(w.Widget)
                && !string.Equals(w.Widget, type, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{path}.widget must match type when type is a dense widget id.");
            }

            return;
        }

        errors.Add($"{path}.type must be text, frame, table, dense, or a known widget id.");
    }

    private static void ValidateAbsoluteColumnPlan(HprpAbsoluteWidget w, string path, List<string> errors)
    {
        if (w.ColumnPlan is null || w.ColumnPlan.Count == 0)
            return;

        for (var i = 0; i < w.ColumnPlan.Count; i++)
        {
            var item = w.ColumnPlan[i];
            var itemPath = $"{path}.columnPlan[{i}]";
            if (string.IsNullOrWhiteSpace(item.Bind))
                errors.Add($"{itemPath}.bind is required.");
        }
    }

    private static void ValidateDesignerLayout(HprpLayout layout, List<string> errors)
    {
        if (layout.Elements.Count == 0)
            errors.Add("layout.elements must contain at least one element when layoutMode is designer.");

        for (var i = 0; i < layout.Elements.Count; i++)
        {
            var el = layout.Elements[i];
            var path = $"layout.elements[{i}]";
            if (string.IsNullOrWhiteSpace(el.Id))
                errors.Add($"{path}.id is required.");

            var type = el.Type?.Trim() ?? "";
            if (!Hprp.Table.HprpDesignerElementTypes.All.Contains(type))
                errors.Add($"{path}.type must be header, config-table, box-text, page-of, dense, data-grid, narrative, field-row, or group.");

            if (el.Box.WMm <= 0 || el.Box.HMm <= 0)
                errors.Add($"{path}.box wMm/hMm must be > 0.");

            if (!string.IsNullOrWhiteSpace(el.Band)
                && !HprpDesignerBands.All.Contains(el.Band.Trim()))
            {
                errors.Add($"{path}.band must be super-header, header, content, footer, or super-footer.");
            }

            HprpChrome.Validate(el.Chrome, path + ".chrome", errors);

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase))
            {
                ValidateDesignerGroup(el, path, errors, depth: 0);
                continue;
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.ConfigTable, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(el.PresetId) && el.TablePreset is null)
                    errors.Add($"{path} presetId or tablePreset is required for config-table.");

                ValidateTableBindings(el.Bindings, path, errors);
                ValidateTableColumnOverrides(el.ColumnOverrides, path, errors);
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.BoxText, StringComparison.OrdinalIgnoreCase))
            {
                var hasItems = el.Items is { Count: > 0 };
                if (!hasItems && string.IsNullOrWhiteSpace(el.Text) && string.IsNullOrWhiteSpace(el.Bind))
                    errors.Add($"{path} text, bind, or items is required for box-text.");

                if (hasItems)
                    ValidateBoxTextItems(el.Items!, path, errors);
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.FieldRow, StringComparison.OrdinalIgnoreCase))
            {
                if (el.Segments is not { Count: > 0 })
                    errors.Add($"{path} segments is required for field-row.");
                else
                    ValidateFieldRowSegments(el.Segments, path, errors);
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.Dense, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(el.Widget) || !HprpWidgetIds.All.Contains(el.Widget)))
            {
                errors.Add($"{path}.widget must be a known widget id for dense elements.");
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.DataGrid, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(el.BindRows))
            {
                errors.Add($"{path}.bindRows is required for data-grid.");
            }

            if (string.Equals(type, Hprp.Table.HprpDesignerElementTypes.Narrative, StringComparison.OrdinalIgnoreCase))
            {
                var hasPack = el.Paragraphs is { Count: > 0 };
                var hasBind = !string.IsNullOrWhiteSpace(el.BindParagraphs);
                if (!hasPack && !hasBind)
                    errors.Add($"{path} paragraphs or bindParagraphs is required for narrative.");
            }
        }
    }

    private static void ValidateDesignerGroup(
        Hprp.Table.HprpDesignerElement el,
        string path,
        List<string> errors,
        int depth)
    {
        if (depth > 0)
        {
            errors.Add($"{path}: nested group is not supported (one column stack level only).");
            return;
        }

        var dir = (el.Direction ?? Hprp.Table.HprpDesignerGroupLimits.DirectionColumn).Trim().ToLowerInvariant();
        if (dir != Hprp.Table.HprpDesignerGroupLimits.DirectionColumn)
            errors.Add($"{path}.direction must be column (v1).");

        var kids = el.Children ?? Array.Empty<Hprp.Table.HprpDesignerElement>();
        if (kids.Count == 0)
            errors.Add($"{path}.children must contain at least one element.");
        if (kids.Count > Hprp.Table.HprpDesignerGroupLimits.MaxChildren)
            errors.Add($"{path}.children max is {Hprp.Table.HprpDesignerGroupLimits.MaxChildren}.");

        for (var c = 0; c < kids.Count; c++)
        {
            var child = kids[c];
            var cpath = $"{path}.children[{c}]";
            if (string.IsNullOrWhiteSpace(child.Id))
                errors.Add($"{cpath}.id is required.");
            var ctype = child.Type?.Trim() ?? "";
            if (string.Equals(ctype, Hprp.Table.HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{cpath}: nested group is not supported.");
                continue;
            }
            if (!Hprp.Table.HprpDesignerElementTypes.All.Contains(ctype)
                || string.Equals(ctype, Hprp.Table.HprpDesignerElementTypes.Group, StringComparison.OrdinalIgnoreCase))
            {
                if (!Hprp.Table.HprpDesignerElementTypes.All.Contains(ctype))
                    errors.Add($"{cpath}.type is invalid.");
            }
            if (string.Equals(ctype, Hprp.Table.HprpDesignerElementTypes.ConfigTable, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(child.PresetId) && child.TablePreset is null)
                    errors.Add($"{cpath} presetId or tablePreset is required for config-table.");
            }
            if (string.Equals(ctype, Hprp.Table.HprpDesignerElementTypes.BoxText, StringComparison.OrdinalIgnoreCase))
            {
                var hasItems = child.Items is { Count: > 0 };
                if (!hasItems && string.IsNullOrWhiteSpace(child.Text) && string.IsNullOrWhiteSpace(child.Bind))
                    errors.Add($"{cpath} text, bind, or items is required for box-text.");
            }
            if (string.Equals(ctype, Hprp.Table.HprpDesignerElementTypes.FieldRow, StringComparison.OrdinalIgnoreCase))
            {
                if (child.Segments is not { Count: > 0 })
                    errors.Add($"{cpath} segments is required for field-row.");
                else
                    ValidateFieldRowSegments(child.Segments, cpath, errors);
            }
        }
    }

    private static void ValidateTableBindings(
        IReadOnlyList<Hprp.Table.HprpTableBinding> bindings,
        string path,
        List<string> errors)
    {
        for (var i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            var itemPath = $"{path}.bindings[{i}]";
            if (string.IsNullOrWhiteSpace(b.Path))
                errors.Add($"{itemPath}.path is required.");
            if (string.IsNullOrWhiteSpace(b.Column))
                errors.Add($"{itemPath}.column is required.");
            if (!Hprp.Table.HprpTableBindingContexts.All.Contains(b.Context))
                errors.Add($"{itemPath}.context is invalid.");
        }
    }

    private static void ValidateTableColumnOverrides(
        IReadOnlyList<Hprp.Table.HprpTableColumnDef>? overrides,
        string path,
        List<string> errors)
    {
        if (overrides is null)
            return;

        for (var i = 0; i < overrides.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(overrides[i].Id))
                errors.Add($"{path}.columnOverrides[{i}].id is required.");
        }
    }

    private static void ValidateBoxTextItems(
        IReadOnlyList<Hprp.Table.HprpBoxTextItem> items,
        string path,
        List<string> errors)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var itemPath = $"{path}.items[{i}]";
            var hasContent = !string.IsNullOrWhiteSpace(item.Label)
                || !string.IsNullOrWhiteSpace(item.Text)
                || !string.IsNullOrWhiteSpace(item.Bind)
                || !string.IsNullOrWhiteSpace(item.Label2)
                || !string.IsNullOrWhiteSpace(item.Text2)
                || !string.IsNullOrWhiteSpace(item.Bind2);
            if (!hasContent)
                errors.Add($"{itemPath} needs label, text, bind, or a second value pair.");

            if (item.Align is { Length: > 0 } align)
            {
                var a = align.Trim().ToLowerInvariant();
                if (a is not ("left" or "center" or "right"))
                    errors.Add($"{itemPath}.align must be left, center, or right.");
            }
        }
    }

    private static void ValidateFieldRowSegments(
        IReadOnlyList<Hprp.Table.HprpFieldRowSegment> segments,
        string path,
        List<string> errors)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            var segPath = $"{path}.segments[{i}]";
            var kind = (seg.Kind ?? Hprp.Table.HprpFieldRowSegmentKinds.Text).Trim().ToLowerInvariant();
            if (kind is not (Hprp.Table.HprpFieldRowSegmentKinds.Options or Hprp.Table.HprpFieldRowSegmentKinds.Text))
                errors.Add($"{segPath}.kind must be options or text.");

            if (kind == Hprp.Table.HprpFieldRowSegmentKinds.Options)
            {
                if (seg.Options is not { Count: > 0 })
                    errors.Add($"{segPath}.options is required for options kind.");
                else
                {
                    for (var j = 0; j < seg.Options.Count; j++)
                    {
                        if (string.IsNullOrWhiteSpace(seg.Options[j].Value)
                            && string.IsNullOrWhiteSpace(seg.Options[j].Label))
                        {
                            errors.Add($"{segPath}.options[{j}] needs value or label.");
                        }
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(seg.Label)
                     && string.IsNullOrWhiteSpace(seg.Bind)
                     && string.IsNullOrWhiteSpace(seg.Text))
            {
                errors.Add($"{segPath} text segment needs label, bind, or text.");
            }
        }
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
        HprpBox.Validate(node.Box, path + ".box", errors);
        ValidateColumnPlan(node, path, errors);

        if (node.GapMm is < 0 or > HprpBox.MaxMm)
            errors.Add($"{path}.gapMm must be between 0 and {HprpBox.MaxMm}.");

        var type = node.Type?.Trim().ToLowerInvariant();
        if (type == "row")
        {
            var cells = node.Cells ?? [];
            if (cells.Count == 0)
                errors.Add($"{path}.cells is required for type row.");

            for (var i = 0; i < cells.Count; i++)
            {
                var cellPath = $"{path}.cells[{i}]";
                if (cells[i].Nodes.Count == 0)
                    errors.Add($"{cellPath}.nodes must not be empty.");
                for (var n = 0; n < cells[i].Nodes.Count; n++)
                    ValidateNode(cells[i].Nodes[n], $"{cellPath}.nodes[{n}]", errors);
            }
        }

        if (type == "column-stack")
        {
            var children = node.Nodes ?? [];
            if (children.Count == 0)
                errors.Add($"{path}.nodes is required for type column-stack.");
            for (var i = 0; i < children.Count; i++)
                ValidateNode(children[i], $"{path}.nodes[{i}]", errors);
        }
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

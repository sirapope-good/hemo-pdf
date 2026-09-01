using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Header;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Absolute;
using Hemo.Pdf.Layouts.Header;
using Hemo.Pdf.Layouts.Table;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Designer;

/// <summary>Composes designer-mode packages: mm-placed elements + config-table engine (multi-page).</summary>
public static class DesignerPageComposer
{
    public static object Compose(DesignerCanvasViewModel vm, PdfReportContext context)
    {
        _ = context;
        var landscape = vm.Landscape;
        var slices = vm.Pages.Count > 0
            ? vm.Pages
            : new[]
            {
                new HprpDesignerPageSlice { PageIndex = 0, Elements = vm.Elements },
            };

        return (Action<IDocumentContainer>)(container =>
        {
            foreach (var slice in slices)
            {
                container.Page(page =>
                {
                    if (landscape)
                        page.Size(PageSizes.A4.Landscape());
                    else
                        page.Size(PageSizes.A4);

                    page.Margin(0);
                    page.DefaultTextStyle(t => t.FontFamily(Hemo.Pdf.Core.Constants.PdfStyleDefaults.Fonts.PrimaryFamily));
                    page.Content().Element(c => c.Layers(layers =>
                    {
                        layers.PrimaryLayer().Element(e =>
                        {
                            if (string.Equals(vm.PageBorder, "thin", StringComparison.OrdinalIgnoreCase))
                                e.Background(Colors.White).Border(0.5f).BorderColor(Colors.Grey.Darken2);
                            else
                                e.Background(Colors.White);
                        });

                        // Boxes are page-absolute (0,0 = sheet top-left); supers sit outside the margin guide.
                        foreach (var element in slice.Elements)
                        {
                            var box = element.Box;
                            layers.Layer()
                                .Width(Math.Max(1f, box.WMm), Unit.Millimetre)
                                .Height(Math.Max(1f, box.HMm), Unit.Millimetre)
                                .TranslateX(Math.Max(0, box.XMm), Unit.Millimetre)
                                .TranslateY(Math.Max(0, box.YMm), Unit.Millimetre)
                                .Element(inner => DrawElement(inner, element, vm));
                        }
                    }));
                });
            }
        });
    }

    private static void DrawElement(
        IContainer container,
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        var type = element.Type?.Trim().ToLowerInvariant() ?? "";

        switch (type)
        {
            case HprpDesignerElementTypes.Header:
                DrawHeader(container, element, vm);
                break;

            case HprpDesignerElementTypes.ConfigTable:
                DrawConfigTable(container, element, vm);
                break;

            case HprpDesignerElementTypes.BoxText:
                ConfigurableBoxTextComposer.Compose(container, element, vm.Data);
                break;

            case HprpDesignerElementTypes.PageOf:
                ConfigurablePageOfComposer.Compose(container, element);
                break;

            case HprpDesignerElementTypes.Dense:
                DrawDense(container, element, vm);
                break;
        }
    }

    private static void DrawHeader(
        IContainer container,
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        var preset = ResolveHeaderPreset(element, vm);
        if (preset is not null)
        {
            var model = HprpHeaderLayoutEngine.Build(
                preset,
                vm.Data,
                ResolveReportTitle(vm));
            ConfigurableHeaderComposer.Compose(container, model);
            return;
        }

        var header = vm.ReadHeader() ?? new HemosheetReportViewModel();
        ThaiUrReportHeader.Compose(container, header, ResolveReportTitle(vm));
    }

    private static string ResolveReportTitle(DesignerCanvasViewModel vm)
    {
        if (vm.BoundModel is Clinical05ProgressNoteReportViewModel soap
            && !string.IsNullOrWhiteSpace(soap.Title))
        {
            return soap.Title;
        }

        if (vm.BoundModel is Clinical05ProgressNoteChecklistReportViewModel checklist
            && !string.IsNullOrWhiteSpace(checklist.Title))
        {
            return checklist.Title;
        }

        var fromHct = vm.ReadHctEpo()?.Title;
        if (!string.IsNullOrWhiteSpace(fromHct))
            return fromHct!;

        if (vm.Data is System.Text.Json.JsonElement json
            && json.ValueKind == System.Text.Json.JsonValueKind.Object
            && json.TryGetProperty("title", out var titleEl)
            && titleEl.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var title = titleEl.GetString();
            if (!string.IsNullOrWhiteSpace(title))
                return title!;
        }

        return vm.Title;
    }

    private static HprpHeaderPreset? ResolveHeaderPreset(
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        if (element.HeaderPreset is not null
            && (!string.IsNullOrWhiteSpace(element.HeaderPreset.Id)
                || element.HeaderPreset.Columns.Count > 0))
        {
            return element.HeaderPreset;
        }

        if (!string.IsNullOrWhiteSpace(element.Preset)
            && vm.HeaderPresets.TryGetValue(element.Preset, out var loaded))
        {
            return loaded;
        }

        return element.HeaderPreset;
    }

    private static void DrawConfigTable(
        IContainer container,
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        var preset = ResolveTablePreset(element, vm);
        if (preset is null)
        {
            container.Border(0.5f).Padding(2)
                .Text("Missing table preset")
                .FontSize(8);
            return;
        }

        var resolved = HprpTablePresetResolver.Resolve(preset, element);
        var layout = HprpTableLayoutEngine.Build(
            resolved,
            element.Bindings,
            vm.Labels,
            vm.Data,
            element.Box.HMm);

        ConfigurableTableComposer.Compose(container, layout);
    }

    private static void DrawDense(
        IContainer container,
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        var absolute = new HprpAbsoluteWidget
        {
            Id = element.Id,
            Type = HprpAbsoluteWidget.TypeDense,
            Widget = element.Widget,
            WMm = element.Box.WMm,
            HMm = element.Box.HMm,
            Chrome = element.Chrome,
        };

        var canvas = new AbsoluteCanvasViewModel
        {
            Title = ResolveReportTitle(vm),
            Page = vm.Page,
            BoundModel = vm.BoundModel ?? vm.ReadHctEpo(),
            Labels = vm.Labels,
        };

        if (!AbsoluteDenseWidgetHost.TryCompose(container, absolute, canvas))
        {
            container.Text($"Unknown dense widget: {element.Widget}").FontSize(8);
        }
    }

    private static HprpTablePreset? ResolveTablePreset(
        HprpDesignerElement element,
        DesignerCanvasViewModel vm)
    {
        if (element.TablePreset is not null
            && (!string.IsNullOrWhiteSpace(element.TablePreset.Id)
                || element.TablePreset.Columns is { Count: > 0 }))
        {
            return element.TablePreset;
        }

        if (!string.IsNullOrWhiteSpace(element.PresetId)
            && vm.Presets.TryGetValue(element.PresetId, out var loaded))
        {
            return loaded;
        }

        return element.TablePreset;
    }
}

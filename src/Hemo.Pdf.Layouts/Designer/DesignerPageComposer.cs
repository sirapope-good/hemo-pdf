using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Clinical;
using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Layouts.Absolute;
using Hemo.Pdf.Layouts.Clinical.Clinical01_HctEpo;
using Hemo.Pdf.Layouts.Table;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Designer;

/// <summary>Composes designer-mode packages: mm-placed elements + config-table engine.</summary>
public static class DesignerPageComposer
{
    public static QuestLayout Compose(DesignerCanvasViewModel vm, PdfReportContext context)
    {
        _ = context;
        var landscape = vm.Landscape;
        var originX = vm.Page.Left;
        var originY = vm.Page.Top;

        return new QuestLayout
        {
            Landscape = landscape,
            MarginTop = 0,
            MarginBottom = 0,
            MarginLeft = 0,
            MarginRight = 0,
            MarginMillimeters = 0,
            Header = null,
            Footer = _ => { },
            Content = c => c.Layers(layers =>
            {
                layers.PrimaryLayer().Element(e =>
                {
                    if (string.Equals(vm.PageBorder, "thin", StringComparison.OrdinalIgnoreCase))
                        e.Background(Colors.White).Border(0.5f).BorderColor(Colors.Grey.Darken2);
                    else
                        e.Background(Colors.White);
                });
                foreach (var element in vm.Elements)
                {
                    var box = element.Box;
                    layers.Layer()
                        .Width(Math.Max(1f, box.WMm), Unit.Millimetre)
                        .Height(Math.Max(1f, box.HMm), Unit.Millimetre)
                        .TranslateX(Math.Max(0, originX + box.XMm), Unit.Millimetre)
                        .TranslateY(Math.Max(0, originY + box.YMm), Unit.Millimetre)
                        .Element(inner => DrawElement(inner, element, vm));
                }
            }),
        };
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
        var header = vm.ReadHeader() ?? new HemosheetReportViewModel();
        var title = vm.ReadHctEpo()?.Title ?? vm.Title;
        ThaiUrReportHeader.Compose(container, header, title);
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
            Title = vm.Title,
            Page = vm.Page,
            BoundModel = vm.ReadHctEpo(),
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
        if (element.TablePreset is not null && !string.IsNullOrWhiteSpace(element.TablePreset.Id))
            return element.TablePreset;

        if (!string.IsNullOrWhiteSpace(element.PresetId)
            && vm.Presets.TryGetValue(element.PresetId, out var loaded))
        {
            return loaded;
        }

        return element.TablePreset;
    }
}

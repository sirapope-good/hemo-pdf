using System.Text.Json;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Hprp.Table;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Clinical;

namespace Hemo.Pdf.Core.Tests;

public class HprpDesignerDataGridTests
{
    [Fact]
    public void Validate_DesignerDataGrid_RequiresBindRows()
    {
        var package = new HprpPackage
        {
            Manifest = new HprpManifest
            {
                Id = ClinicalReportCatalog.Lab,
                LayoutMode = HprpLayoutModes.Designer,
            },
            Layout = new HprpLayout
            {
                Elements =
                [
                    new HprpDesignerElement
                    {
                        Id = "grid",
                        Type = HprpDesignerElementTypes.DataGrid,
                        Box = new HprpDesignerBox { WMm = 194, HMm = 200 },
                    },
                ],
            },
        };

        var result = HprpValidator.Validate(package);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("bindRows", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BindDesignerDataGrid_UsesColumnHeadersBind()
    {
        var element = new HprpDesignerElement
        {
            Id = "lab",
            Type = HprpDesignerElementTypes.DataGrid,
            BindRows = "$.rows",
            ColumnHeadersBind = "$.columnHeaders",
            Chrome = new HprpChrome { Border = "thin", FontSize = 7 },
            Box = new HprpDesignerBox { WMm = 194, HMm = 200 },
        };

        var data = JsonSerializer.SerializeToElement(new
        {
            columnHeaders = new[] { "", "DATE", "DATE" },
            rows = new[] { new[] { "1 Month", "", "" } },
        });

        var grid = HprpBinder.BindDesignerDataGrid(element, data, new Dictionary<string, string>());
        Assert.NotNull(grid);
        Assert.Equal(["", "DATE", "DATE"], grid!.Columns);
        Assert.Equal("1 Month", grid.Rows[0][0]);
    }

    [Fact]
    public void ToLayoutNode_MapsDataGridFields()
    {
        var element = new HprpDesignerElement
        {
            Type = HprpDesignerElementTypes.DataGrid,
            BindRows = "$.rows",
            ColumnHeadersBind = "$.columnHeaders",
            Chrome = new HprpChrome { FontSize = 7 },
        };

        var node = element.ToLayoutNode();
        Assert.Equal(HprpDesignerElementTypes.DataGrid, node.Type);
        Assert.Equal("$.rows", node.BindRows);
        Assert.Equal("$.columnHeaders", node.ColumnHeadersBind);
        Assert.Equal(7f, node.Chrome!.FontSize);
    }

    [Fact]
    public void BudgetRowHeightForTable_MatchesCompositionWhenAvailableHeightEqual()
    {
        const int rowCount = 45;
        const float verticalMarginMm = 16f;
        const float headerMm = 21.6f + 5.4f;
        const float spacingMm = 2f;
        const float safetyMm = 1.5f;
        var availableMm = 297f - verticalMarginMm - headerMm - spacingMm - safetyMm;

        var direct = ClinicalDefaultComposer.BudgetRowHeightForTable(rowCount, availableMm);
        var fromPage = ClinicalDefaultComposer.BudgetLabRowHeightMm(rowCount, verticalMarginMm);

        Assert.Equal(direct, fromPage, precision: 3);
    }

    [Fact]
    public void ApplyRowHeightToGrid_SetsChromeRowHeight()
    {
        var grid = new DataGridReportBlock
        {
            Columns = ["Lab", "Date"],
            Rows = [["HCT", "32"]],
            Chrome = new HprpChrome { Border = "thin", FontSize = 7 },
        };

        var drawn = ClinicalDefaultComposer.ApplyRowHeightToGrid(grid, 5.5f);
        Assert.Equal(5.5f, drawn.Chrome!.RowHeightMm);
        Assert.Equal("thin", drawn.Chrome.Border);
    }
}

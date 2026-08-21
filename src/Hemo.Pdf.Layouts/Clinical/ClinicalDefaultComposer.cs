using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Context;
using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models;
using Hemo.Pdf.Core.Models.Preview;
using Hemo.Pdf.Layouts.Base;
using Hemo.Pdf.Rendering;
using Hemo.Pdf.Sections.Abstractions;
using Hemo.Pdf.Sections.Content;
using Hemo.Pdf.Sections.ThaiUr;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hemo.Pdf.Layouts.Clinical;

public sealed class ClinicalDefaultComposer : BaseReportComposer<HprpBoundViewModel>
{
    private const float A4HeightMm = 297f;
    private const float LayoutSafetyMm = 1.5f;
    private const float ThaiUrSectionSpacingMm = 2f;
    private const float MinLabRowHeightMm = 4.6f;

    public ClinicalDefaultComposer(
        ISectionResolver<IReportHeaderSection> headerResolver,
        ISectionResolver<IReportFooterSection> footerResolver)
        : base(headerResolver, footerResolver)
    {
    }

    public override object Compose(object dataModel, PdfReportContext context)
    {
        var viewModel = (HprpBoundViewModel)dataModel;
        if (!viewModel.UseThaiUrHeader)
            return base.Compose(dataModel, context);

        PrepareContext(context, viewModel);
        var margin = HemosheetThaiUrStyle.PageMarginMm;
        return new QuestLayout
        {
            MarginMillimeters = margin,
            MarginTop = margin,
            MarginBottom = margin,
            MarginLeft = margin,
            MarginRight = margin,
            Header = null,
            Footer = null,
            Content = c => ComposeThaiUrContent(c, viewModel, context),
        };
    }

    protected override void ComposeContent(
        IContainer container,
        HprpBoundViewModel viewModel,
        PdfReportContext context)
    {
        ComposeBody(container, viewModel, context, includeThaiUrHeader: false);
    }

    private static void ComposeThaiUrContent(
        IContainer container,
        HprpBoundViewModel viewModel,
        PdfReportContext context)
    {
        ComposeBody(container, viewModel, context, includeThaiUrHeader: true);
    }

    private static void ComposeBody(
        IContainer container,
        HprpBoundViewModel viewModel,
        PdfReportContext context,
        bool includeThaiUrHeader)
    {
        container.Column(col =>
        {
            col.Spacing(includeThaiUrHeader ? ThaiUrSectionSpacingMm : 6);

            if (includeThaiUrHeader)
            {
                col.Item().Element(c =>
                    ThaiUrReportHeader.Compose(c, viewModel.Header ?? new(), viewModel.Title));
            }

            foreach (var block in viewModel.Blocks)
            {
                var drawn = includeThaiUrHeader ? WithPageFillRowHeight(block) : block;
                col.Item().Element(c => ReportBlockPdfComposer.Compose(c, drawn, context));
            }
        });
    }

    private static ReportBlock WithPageFillRowHeight(ReportBlock block)
    {
        if (block is not DataGridReportBlock grid || grid.Rows.Count == 0)
            return block;

        var rowHeightMm = BudgetLabRowHeightMm(grid.Rows.Count + 1);
        return new DataGridReportBlock
        {
            Title = grid.Title,
            Columns = grid.Columns,
            ColumnWeights = grid.ColumnWeights,
            Rows = grid.Rows,
            Chrome = WithRowHeight(grid.Chrome, rowHeightMm),
        };
    }

    private static HprpChrome WithRowHeight(HprpChrome? chrome, float rowHeightMm) =>
        new()
        {
            HeaderFill = chrome?.HeaderFill,
            Border = chrome?.Border,
            FontSize = chrome?.FontSize,
            RowHeightMm = rowHeightMm,
            ColumnWidths = chrome?.ColumnWidths,
        };

    /// <summary>
    /// Split leftover A4 height across DATE header + body rows so a blank lab form
    /// sits flush at the bottom of the page.
    /// </summary>
    internal static float BudgetLabRowHeightMm(int tableRowCount)
    {
        var pageContentMm = A4HeightMm - 2f * HemosheetThaiUrStyle.PageMarginMm;
        var headerMm = HemosheetThaiUrStyle.TitleHeightMm + HemosheetThaiUrStyle.MetaRowHeightMm;
        var availableMm = pageContentMm
            - headerMm
            - ThaiUrSectionSpacingMm
            - LayoutSafetyMm;

        if (tableRowCount <= 0)
            return MinLabRowHeightMm;

        return Math.Max(availableMm / tableRowCount, MinLabRowHeightMm);
    }
}

using Hemo.Pdf.Core.Models.Hemosheet;
using Hemo.Pdf.Core.Models.Preview;

namespace Hemo.Pdf.Core.Models;

/// <summary>Bound layout produced by <c>.hprp</c> for default/scaffold clinical reports.</summary>
public sealed class HprpBoundViewModel
{
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public IReadOnlyList<ReportBlock> Blocks { get; init; } = [];
    /// <summary>File hex <c>chrome.headerFill</c> for DOM preview branding overlay.</summary>
    public string? SectionHeaderFill { get; init; }
    /// <summary>When set, PDF uses shared ThaiUR header instead of configurable branding chrome.</summary>
    public bool UseThaiUrHeader { get; init; }
    public HemosheetReportViewModel? Header { get; init; }
}

using Hemo.Pdf.Core.Hprp;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public interface IHemosheetLayoutPlanner
{
    /// <param name="packageOverlay">
    /// Studio preview draft (or other in-memory package). Preferred over the template store.
    /// </param>
    IReadOnlyList<HemosheetSectionPlan> Plan(
        HemosheetReportViewModel viewModel,
        HprpPackage? packageOverlay = null);
}

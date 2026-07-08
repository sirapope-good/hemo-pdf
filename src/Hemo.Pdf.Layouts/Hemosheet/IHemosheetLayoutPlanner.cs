using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public interface IHemosheetLayoutPlanner
{
    IReadOnlyList<HemosheetSectionPlan> Plan(HemosheetReportViewModel viewModel);
}

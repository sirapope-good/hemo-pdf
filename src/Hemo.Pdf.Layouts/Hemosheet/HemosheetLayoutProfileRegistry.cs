using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Layouts.Hemosheet;

public sealed class HemosheetLayoutProfileRegistry
{
    /// <summary>
    /// Profile-gated sections. Planner builds order inline; only Consent is Rama-only today.
    /// </summary>
    public bool IsProfileSection(HemosheetSectionId sectionId, HemosheetLayoutProfile profile) =>
        sectionId switch
        {
            HemosheetSectionId.Consent => profile == HemosheetLayoutProfile.Rama,
            _ => true,
        };
}

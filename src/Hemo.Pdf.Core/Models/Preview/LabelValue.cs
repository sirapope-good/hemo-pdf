namespace Hemo.Pdf.Core.Models.Preview;

public sealed class LabelValue
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";

    /// <summary>0 = top-level; 1+ = nested under a parent topic (paper-form tab).</summary>
    public int Indent { get; init; }
}

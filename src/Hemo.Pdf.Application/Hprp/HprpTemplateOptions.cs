namespace Hemo.Pdf.Application.Hprp;

public sealed class HprpTemplateOptions
{
    public string RootPath { get; set; } = "assets/templates";

    /// <summary>Packed <c>.hprp</c> directory. Empty skips package scan.</summary>
    public string PackagesRootPath { get; set; } = "packages";

    /// <summary>When set, Studio/pack writes here instead of the repo <c>packages/</c> folder.</summary>
    public string? PackagesWritePath { get; set; }

    public bool EnableHprpStudioWrite { get; set; }
}

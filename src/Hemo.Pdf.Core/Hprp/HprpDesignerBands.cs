namespace Hemo.Pdf.Core.Hprp;

/// <summary>Designer page band roles for repeating chrome vs flowing content.</summary>
public static class HprpDesignerBands
{
    public const string SuperHeader = "super-header";
    public const string Header = "header";
    public const string Content = "content";
    public const string Footer = "footer";
    public const string SuperFooter = "super-footer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SuperHeader,
        Header,
        Content,
        Footer,
        SuperFooter,
    };

    public static string Normalize(string? band) =>
        string.IsNullOrWhiteSpace(band) ? Content
        : All.Contains(band.Trim()) ? band.Trim().ToLowerInvariant()
        : Content;

    public static string Resolve(Hprp.Table.HprpDesignerElement element)
    {
        if (!string.IsNullOrWhiteSpace(element.Band) && All.Contains(element.Band.Trim()))
            return element.Band.Trim().ToLowerInvariant();

        if (string.Equals(element.Type, Hprp.Table.HprpDesignerElementTypes.Header, StringComparison.OrdinalIgnoreCase))
            return Header;

        if (string.Equals(element.Type, Hprp.Table.HprpDesignerElementTypes.PageOf, StringComparison.OrdinalIgnoreCase))
            return SuperFooter;

        return Content;
    }

    public static bool IsChrome(string? band)
    {
        var b = Normalize(band);
        return b is SuperHeader or Header or Footer or SuperFooter;
    }
}

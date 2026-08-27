using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

public static class HprpTableRowModes
{
    public const string Freedom = "freedom";
    public const string Monthly = "monthly";
    public const string Annual = "annual";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Freedom,
        Monthly,
        Annual,
    };
}

public static class HprpTableBindingContexts
{
    public const string Entry = "entry";
    public const string GroupLabel = "group-label";
    public const string FreedomRow = "freedom-row";
    public const string LabHistorical = "lab-historical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Entry,
        GroupLabel,
        FreedomRow,
        LabHistorical,
    };
}

public static class HprpDesignerElementTypes
{
    public const string Header = "header";
    public const string ConfigTable = "config-table";
    public const string BoxText = "box-text";
    public const string Dense = "dense";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Header,
        ConfigTable,
        BoxText,
        Dense,
    };
}

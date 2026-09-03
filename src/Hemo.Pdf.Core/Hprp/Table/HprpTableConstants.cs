using System.Text.Json.Serialization;

namespace Hemo.Pdf.Core.Hprp.Table;

public static class HprpTableRowModes
{
    public const string Freedom = "freedom";
    public const string Monthly = "monthly";
    public const string Annual = "annual";

    /// <summary>
    /// Item × month cross-tab (e.g. progress-note Default checklist).
    /// Time on the X axis; rows are checklist items (not month groups).
    /// </summary>
    public const string Matrix = "matrix";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Freedom,
        Monthly,
        Annual,
        Matrix,
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

public static class HprpTableCellKinds
{
    /// <summary>Default plain text cell.</summary>
    public const string Text = "text";

    /// <summary>
    /// Progress-note SOAP nested cell (S/O/A/P + Objective checkboxes).
    /// Drawn by clinical SOAP section, not plain text.
    /// </summary>
    public const string SoapProgress = "soap-progress";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Text,
        SoapProgress,
    };
}

public static class HprpDesignerElementTypes
{
    public const string Header = "header";
    public const string ConfigTable = "config-table";
    public const string BoxText = "box-text";
    public const string PageOf = "page-of";
    public const string Dense = "dense";
    /// <summary>Column (or future row) container; children stack in <c>direction</c>.</summary>
    public const string Group = "group";

    /// <summary>Bound tabular block (<c>bindRows</c> / <c>columnHeadersBind</c>) — clinical lab matrix.</summary>
    public const string DataGrid = "data-grid";

    /// <summary>
    /// Multi-paragraph Word-lite block (<c>paragraphs[]</c> editable in Studio;
    /// optional <c>bindParagraphs</c> for live report-data override).
    /// </summary>
    public const string Narrative = "narrative";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Header,
        ConfigTable,
        BoxText,
        PageOf,
        Dense,
        Group,
        DataGrid,
        Narrative,
    };
}

/// <summary>Soft/hard limits for designer <c>type: group</c> column stacks.</summary>
public static class HprpDesignerGroupLimits
{
    /// <summary>Max children in one column stack (inner section).</summary>
    public const int MaxChildren = 4;

    public const string DirectionColumn = "column";
}

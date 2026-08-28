namespace Hemo.Pdf.Application.Hprp;

public sealed class HeaderLibraryDeleteResult
{
    public string Id { get; init; } = "";
    public bool Ok { get; init; }
    public bool FellBackToSeed { get; init; }
    public bool IsSeedOnly { get; init; }
    public bool IsNotFound { get; init; }
    public string? DeletedPath { get; init; }
    public string? Message { get; init; }

    public static HeaderLibraryDeleteResult Deleted(string id, string path, bool fellBackToSeed) => new()
    {
        Id = id,
        Ok = true,
        FellBackToSeed = fellBackToSeed,
        DeletedPath = path,
        Message = fellBackToSeed
            ? $"Deleted library override; seed {id} remains."
            : $"Deleted library header {id}.",
    };

    public static HeaderLibraryDeleteResult SeedOnly(string id) => new()
    {
        Id = id,
        IsSeedOnly = true,
        Message = $"Cannot delete seed header '{id}' (assets). Only packages/library/headers overrides can be removed.",
    };

    public static HeaderLibraryDeleteResult NotFound(string id) => new()
    {
        Id = id,
        IsNotFound = true,
        Message = $"Header '{id}' not found in packages/library/headers.",
    };
}

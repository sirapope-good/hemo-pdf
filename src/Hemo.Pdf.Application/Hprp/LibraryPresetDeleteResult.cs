namespace Hemo.Pdf.Application.Hprp;

/// <summary>Result of deleting a Studio library override under <c>packages/library/…</c>.</summary>
public sealed class LibraryPresetDeleteResult
{
    public string Id { get; init; } = "";
    public bool Ok { get; init; }
    public bool FellBackToSeed { get; init; }
    public bool IsSeedOnly { get; init; }
    public bool IsNotFound { get; init; }
    public string? DeletedPath { get; init; }
    public string? Message { get; init; }

    public static LibraryPresetDeleteResult Deleted(string id, string path, bool fellBackToSeed, string kind) => new()
    {
        Id = id,
        Ok = true,
        FellBackToSeed = fellBackToSeed,
        DeletedPath = path,
        Message = fellBackToSeed
            ? $"Deleted library {kind} override; seed {id} remains."
            : $"Deleted library {kind} {id}.",
    };

    public static LibraryPresetDeleteResult SeedOnly(string id, string kind) => new()
    {
        Id = id,
        IsSeedOnly = true,
        Message = $"Cannot delete seed {kind} '{id}' (assets). Only packages/library overrides can be removed.",
    };

    public static LibraryPresetDeleteResult NotFound(string id, string kind) => new()
    {
        Id = id,
        IsNotFound = true,
        Message = $"{kind} '{id}' not found in packages/library.",
    };
}

namespace Hemo.Pdf.Core.Hprp;

public sealed class HprpValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static HprpValidationResult Ok() => new();

    public static HprpValidationResult Fail(params string[] errors) =>
        new() { Errors = errors };
}

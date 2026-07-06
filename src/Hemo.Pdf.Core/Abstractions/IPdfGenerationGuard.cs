using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Core.Abstractions;

public interface IPdfGenerationGuard
{
    Task EnsureCanGenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken);
}

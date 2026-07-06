using Hemo.Pdf.Core.Models;

namespace Hemo.Pdf.Application;

public interface IPdfGenerationService
{
    Task<byte[]> GenerateAsync(GeneratePdfRequest request, CancellationToken cancellationToken);
}

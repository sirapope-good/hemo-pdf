namespace Hemo.Pdf.Core.Abstractions;

public interface IPdfRenderer
{
    Task<byte[]> RenderAsync(object layoutSchema, CancellationToken cancellationToken);
}

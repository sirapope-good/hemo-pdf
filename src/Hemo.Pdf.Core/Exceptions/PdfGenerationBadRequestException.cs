namespace Hemo.Pdf.Core.Exceptions;

public sealed class PdfGenerationBadRequestException : Exception
{
    public PdfGenerationBadRequestException(string message)
        : base(message)
    {
    }

    public PdfGenerationBadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

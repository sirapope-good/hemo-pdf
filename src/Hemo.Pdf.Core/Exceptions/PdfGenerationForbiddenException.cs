namespace Hemo.Pdf.Core.Exceptions;

public sealed class PdfGenerationForbiddenException : Exception
{
    public PdfGenerationForbiddenException(string message)
        : base(message)
    {
    }

    public PdfGenerationForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

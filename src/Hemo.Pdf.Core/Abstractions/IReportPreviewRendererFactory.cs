namespace Hemo.Pdf.Core.Abstractions;

public interface IReportPreviewRendererFactory
{
    IReportPreviewRenderer Create(string reportTemplateId);
}

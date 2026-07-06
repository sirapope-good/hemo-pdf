namespace Hemo.Pdf.Core.Abstractions;

public interface IReportRendererFactory
{
    IReportRenderer Create(string reportTemplateId);
}

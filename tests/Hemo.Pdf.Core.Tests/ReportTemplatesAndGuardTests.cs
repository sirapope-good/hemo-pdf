using Hemo.Pdf.Application.Guards;
using Hemo.Pdf.Application.Mock;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;
using System.Text.Json;

namespace Hemo.Pdf.Core.Tests;

public class ReportTemplatesTests
{
    [Theory]
    [InlineData(ReportTemplates.DialysisSession, true)]
    [InlineData(ReportTemplates.LabResult, false)]
    [InlineData(ReportTemplates.Prescription, true)]
    [InlineData(ReportTemplates.Summary, false)]
    public void RequiresSignature_ReturnsExpected(string templateId, bool expected)
    {
        Assert.Equal(expected, ReportTemplates.RequiresSignature(templateId));
    }

    [Fact]
    public void All_ContainsTwelveTemplates()
    {
        Assert.Equal(12, ReportTemplates.All.Count);
    }
}

public class SignatureRequiredGuardTests
{
    [Fact]
    public async Task UnsignedTemplate_ThrowsForbidden()
    {
        var guard = new SignatureRequiredGuard(new UnsignedSignatureStore());
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.DialysisSession,
            TenantCode = "tenant-demo-a",
            EntityId = "session-1",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        await Assert.ThrowsAsync<PdfGenerationForbiddenException>(
            () => guard.EnsureCanGenerateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UnsignedTemplate_WithMockStore_Passes()
    {
        var guard = new SignatureRequiredGuard(new MockSignatureStore());
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.DialysisSession,
            TenantCode = "tenant-demo-a",
            EntityId = "session-1",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        await guard.EnsureCanGenerateAsync(request, CancellationToken.None);
    }

    private sealed class UnsignedSignatureStore : Core.Abstractions.ISignatureStore
    {
        public Task<ReportSignatureContext> GetAsync(
            string reportTemplateId,
            string entityId,
            string tenantCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ReportSignatureContext { IsFullySigned = false });
    }
}

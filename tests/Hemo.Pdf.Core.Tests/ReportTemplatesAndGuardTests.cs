using System.Security.Claims;
using System.Text.Json;
using Hemo.Pdf.Application;
using Hemo.Pdf.Application.Guards;
using Hemo.Pdf.Application.Mock;
using Hemo.Pdf.Core.Abstractions;
using Hemo.Pdf.Core.Constants;
using Hemo.Pdf.Core.Exceptions;
using Hemo.Pdf.Core.Models;

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
        var guard = new SignatureRequiredGuard(new ReportSignatureResolver(new UnsignedSignatureStore()));
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
        var guard = new SignatureRequiredGuard(new ReportSignatureResolver(new MockSignatureStore()));
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.DialysisSession,
            TenantCode = "tenant-demo-a",
            EntityId = "session-1",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        await guard.EnsureCanGenerateAsync(request, CancellationToken.None);
    }

    private sealed class UnsignedSignatureStore : ISignatureStore
    {
        public Task<ReportSignatureContext> GetAsync(
            string reportTemplateId,
            string entityId,
            string tenantCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ReportSignatureContext { IsFullySigned = false });
    }
}

public class ReportSignatureResolverTests
{
    [Fact]
    public async Task RequestSignatures_Wins()
    {
        var expected = new ReportSignatureContext { IsFullySigned = true };
        var resolver = new ReportSignatureResolver(new UnsignedSignatureStore());
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.Hemosheet,
            TenantCode = "tenant-demo-a",
            EntityId = "hemo-1",
            Data = JsonDocument.Parse("""{"doctorSignatureBase64":"abc"}""").RootElement,
            Signatures = expected,
        };

        var result = await resolver.ResolveAsync(request, CancellationToken.None);
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task FallsBackToTryResolveFromData()
    {
        var resolver = new ReportSignatureResolver(new UnsignedSignatureStore());
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.Hemosheet,
            TenantCode = "tenant-demo-a",
            EntityId = "hemo-1",
            Data = JsonDocument.Parse("""{"doctorSignatureBase64":"abc","doctorName":"Dr A"}""").RootElement,
        };

        var result = await resolver.ResolveAsync(request, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result!.IsFullySigned);
    }

    [Fact]
    public async Task FallsBackToStore_WhenNoDataSignatures()
    {
        var store = new CapturingSignatureStore();
        var resolver = new ReportSignatureResolver(store);
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.LabResult,
            TenantCode = "tenant-demo-a",
            EntityId = "lab-1",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        var result = await resolver.ResolveAsync(request, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(store.Called);
    }

    private sealed class UnsignedSignatureStore : ISignatureStore
    {
        public Task<ReportSignatureContext> GetAsync(
            string reportTemplateId,
            string entityId,
            string tenantCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ReportSignatureContext { IsFullySigned = false });
    }

    private sealed class CapturingSignatureStore : ISignatureStore
    {
        public bool Called { get; private set; }

        public Task<ReportSignatureContext> GetAsync(
            string reportTemplateId,
            string entityId,
            string tenantCode,
            CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(new ReportSignatureContext { IsFullySigned = true });
        }
    }
}

public class TenantRequestResolverTests
{
    [Fact]
    public void Claim_WithMatchingHeader_UsesClaim()
    {
        var user = CreateUser("local");
        var tenant = TenantRequestResolver.ResolveEffectiveTenant(user, "local", requireTenantClaim: true);
        Assert.Equal("local", tenant);
    }

    [Fact]
    public void Claim_WithMismatchedHeader_Throws()
    {
        var user = CreateUser("local");
        Assert.Throws<PdfGenerationForbiddenException>(() =>
            TenantRequestResolver.ResolveEffectiveTenant(user, "other", requireTenantClaim: true));
    }

    [Fact]
    public void Claim_WithoutHeader_UsesClaim()
    {
        var user = CreateUser("LocalHost");
        var tenant = TenantRequestResolver.ResolveEffectiveTenant(user, null, requireTenantClaim: true);
        Assert.Equal("local", tenant);
    }

    [Fact]
    public void MissingClaim_WhenRequired_Throws()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")], "jwt"));
        Assert.Throws<PdfGenerationForbiddenException>(() =>
            TenantRequestResolver.ResolveEffectiveTenant(user, "local", requireTenantClaim: true));
    }

    [Fact]
    public void MockPath_WithoutClaim_UsesHeader()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "mock")], "mock"));
        var tenant = TenantRequestResolver.ResolveEffectiveTenant(user, "tenant-demo-a", requireTenantClaim: false);
        Assert.Equal("tenant-demo-a", tenant);
    }

    private static ClaimsPrincipal CreateUser(string tenantCode) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(TenantRequestResolver.TenantCodeClaimType, tenantCode),
            ],
            "jwt"));
}

public class GeneratePdfRequestValidatorTests
{
    [Fact]
    public void MismatchedTenant_ThrowsForbidden()
    {
        var accessor = new MockTenantContextAccessor();
        accessor.SetTenantCode("local");
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.LabResult,
            TenantCode = "other",
            EntityId = "e1",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        Assert.Throws<PdfGenerationForbiddenException>(() =>
            GeneratePdfRequestValidator.Validate(request, accessor));
    }

    [Fact]
    public void MissingEntityId_ThrowsBadRequest()
    {
        var accessor = new MockTenantContextAccessor();
        accessor.SetTenantCode("local");
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.LabResult,
            TenantCode = "local",
            Data = JsonDocument.Parse("{}").RootElement,
        };

        Assert.Throws<PdfGenerationBadRequestException>(() =>
            GeneratePdfRequestValidator.Validate(request, accessor));
    }

    [Fact]
    public void EntityIdMismatchWithDataId_ThrowsBadRequest()
    {
        var accessor = new MockTenantContextAccessor();
        accessor.SetTenantCode("local");
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.LabResult,
            TenantCode = "local",
            EntityId = "a",
            Data = JsonDocument.Parse("""{"id":"b"}""").RootElement,
        };

        Assert.Throws<PdfGenerationBadRequestException>(() =>
            GeneratePdfRequestValidator.Validate(request, accessor));
    }

    [Fact]
    public void MatchingEntityAndTenant_Passes()
    {
        var accessor = new MockTenantContextAccessor();
        accessor.SetTenantCode("local");
        var request = new GeneratePdfRequest
        {
            ReportTemplateId = ReportTemplates.LabResult,
            TenantCode = "local",
            EntityId = "hemo-1",
            Data = JsonDocument.Parse("""{"id":"hemo-1"}""").RootElement,
        };

        GeneratePdfRequestValidator.Validate(request, accessor);
    }
}

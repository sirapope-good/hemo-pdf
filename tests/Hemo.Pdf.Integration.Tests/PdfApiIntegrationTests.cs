using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hemo.Pdf.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hemo.Pdf.Integration.Tests;

public class PdfApiIntegrationTests : IClassFixture<PdfApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PdfApiIntegrationTests(PdfApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePdf_WithTenantDemoA_ReturnsPdfBytes()
    {
        var response = await PostGenerateAsync("tenant-demo-a", "template-02-lab-result");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public async Task GeneratePdf_TenantA_And_TenantB_ProduceDifferentPdfs()
    {
        var responseA = await PostGenerateAsync("tenant-demo-a", "template-02-lab-result");
        var responseB = await PostGenerateAsync("tenant-demo-b", "template-02-lab-result");

        var bytesA = await responseA.Content.ReadAsByteArrayAsync();
        var bytesB = await responseB.Content.ReadAsByteArrayAsync();

        Assert.NotEqual(bytesA.Length, 0);
        Assert.NotEqual(bytesB.Length, 0);
        Assert.NotEqual(Convert.ToBase64String(bytesA), Convert.ToBase64String(bytesB));
    }

    [Fact]
    public async Task GeneratePdf_AllTwelveTemplates_ReturnPdf()
    {
        var templates = new[]
        {
            "template-01-dialysis-session",
            "template-02-lab-result",
            "template-03-prescription",
            "template-04-hemosheet",
            "template-05-nurse-record",
            "template-06-doctor-record",
            "template-07-med-history",
            "template-08-adequacy",
            "template-09-assessment",
            "template-10-admission",
            "template-11-progress-note",
            "template-12-summary",
        };

        foreach (var templateId in templates)
        {
            var response = await PostGenerateAsync("tenant-demo-a", templateId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 50, $"Template {templateId} produced empty PDF");
        }
    }

    [Fact]
    public async Task GeneratePdf_WithEmptySignatureImageBytes_ReturnsPdf()
    {
        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "template-02-lab-result",
            new
            {
                reportTemplateId = "template-02-lab-result",
                tenantCode = "tenant-demo-a",
                entityId = "test-entity-1",
                data = new { patientName = "Test Patient" },
                signatures = new
                {
                    isFullySigned = true,
                    signatures = new[]
                    {
                        new { signerName = "Signer", imageBytes = "" },
                    },
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PostGenerateAsync(string tenantCode, string templateId)
    {
        return await PostGenerateAsync(tenantCode, templateId, new
        {
            reportTemplateId = templateId,
            tenantCode,
            entityId = "test-entity-1",
            data = new { patientName = "Test Patient", value = 42 },
        });
    }

    private async Task<HttpResponseMessage> PostGenerateAsync(
        string tenantCode,
        string templateId,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pdf/generate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "dev");
        request.Headers.Add("X-Tenant-Code", tenantCode);
        request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Preview_ReturnsJson_WithBlocks()
    {
        var response = await PostPreviewAsync("tenant-demo-a", "template-02-lab-result");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("template-02-lab-result", root.GetProperty("meta").GetProperty("templateId").GetString());
        Assert.True(root.GetProperty("pages")[0].GetProperty("blocks").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Preview_AllTwelveTemplates_ReturnDocument()
    {
        var templates = new[]
        {
            "template-01-dialysis-session",
            "template-02-lab-result",
            "template-03-prescription",
            "template-04-hemosheet",
            "template-05-nurse-record",
            "template-06-doctor-record",
            "template-07-med-history",
            "template-08-adequacy",
            "template-09-assessment",
            "template-10-admission",
            "template-11-progress-note",
            "template-12-summary",
        };

        foreach (var templateId in templates)
        {
            var response = await PostPreviewAsync("tenant-demo-a", templateId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            Assert.Equal(templateId, document.RootElement.GetProperty("meta").GetProperty("templateId").GetString());
        }
    }

    [Fact]
    public async Task Preview_TenantA_And_TenantB_DifferentBranding()
    {
        var responseA = await PostPreviewAsync("tenant-demo-a", "template-02-lab-result");
        var responseB = await PostPreviewAsync("tenant-demo-b", "template-02-lab-result");

        var jsonA = await responseA.Content.ReadAsStringAsync();
        var jsonB = await responseB.Content.ReadAsStringAsync();

        using var docA = JsonDocument.Parse(jsonA);
        using var docB = JsonDocument.Parse(jsonB);

        var linesA = docA.RootElement.GetProperty("branding").GetProperty("companyLines").EnumerateArray()
            .Select(x => x.GetString()).ToArray();
        var linesB = docB.RootElement.GetProperty("branding").GetProperty("companyLines").EnumerateArray()
            .Select(x => x.GetString()).ToArray();

        Assert.NotEqual(linesA[0], linesB[0]);
    }

    [Fact]
    public async Task Preview_UnsignedRequiredTemplate_Returns403()
    {
        var response = await PostPreviewAsync(
            "tenant-demo-a",
            "template-01-dialysis-session",
            new
            {
                reportTemplateId = "template-01-dialysis-session",
                tenantCode = "tenant-demo-a",
                data = new { patientName = "Test Patient" },
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("template-04-hemosheet-hd-av.json", "vascular-access")]
    [InlineData("template-04-hemosheet-hd-av.json", "checklist-table")]
    [InlineData("template-04-hemosheet-hd-av.json", "field-grid")]
    [InlineData("template-04-hemosheet-hd-av.json", "sub-header-bar")]
    [InlineData("template-04-hemosheet-hd-av.json", "section-row")]
    [InlineData("template-04-hemosheet-hdf-av.json", "data-grid")]
    [InlineData("template-04-hemosheet-hd-perm.json", "vascular-access")]
    public async Task Preview_HemosheetLayoutVariants_ContainExpectedBlocks(string mockFile, string expectedBlockType)
    {
        var mockPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "assets", "mock-data", mockFile));

        var jsonText = await File.ReadAllTextAsync(mockPath);
        using var data = JsonDocument.Parse(jsonText);

        var response = await PostPreviewAsync(
            "tenant-demo-a",
            "template-04-hemosheet",
            new
            {
                reportTemplateId = "template-04-hemosheet",
                tenantCode = "tenant-demo-a",
                entityId = "hemosheet-test",
                data = data.RootElement,
                signatures = new
                {
                    isFullySigned = true,
                    signatures = new[] { new { signerName = "Nurse" } },
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseJson = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseJson);
        var blocks = document.RootElement.GetProperty("pages")[0].GetProperty("blocks");
        var types = blocks.EnumerateArray().Select(b => b.GetProperty("type").GetString()).ToList();
        Assert.Contains(expectedBlockType, types);
    }

    [Fact]
    public async Task GeneratePdf_HemosheetThaiUr_ReturnsSinglePagePdf()
    {
        var mockPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "assets", "mock-data", "template-04-hemosheet-thaiur.json"));

        var jsonText = await File.ReadAllTextAsync(mockPath);
        using var data = JsonDocument.Parse(jsonText);

        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "template-04-hemosheet",
            new
            {
                reportTemplateId = "template-04-hemosheet",
                tenantCode = "tenant-demo-a",
                entityId = "hemosheet-thaiur",
                data = data.RootElement,
                signatures = new
                {
                    isFullySigned = true,
                    signatures = new[] { new { signerName = "Nurse" } },
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);

        var pdfAscii = System.Text.Encoding.ASCII.GetString(bytes);
        var pageCount = System.Text.RegularExpressions.Regex.Matches(pdfAscii, @"/Type\s*/Page[^s]").Count;
        Assert.Equal(1, pageCount);

        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output"));
        Directory.CreateDirectory(outDir);
        await File.WriteAllBytesAsync(Path.Combine(outDir, "hemosheet-thaiur.pdf"), bytes);
    }

    private async Task<HttpResponseMessage> PostPreviewAsync(string tenantCode, string templateId)
    {
        return await PostPreviewAsync(tenantCode, templateId, new
        {
            reportTemplateId = templateId,
            tenantCode,
            entityId = "test-entity-1",
            data = new { patientName = "Test Patient", value = 42 },
        });
    }

    private async Task<HttpResponseMessage> PostPreviewAsync(
        string tenantCode,
        string templateId,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/report/preview");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "dev");
        request.Headers.Add("X-Tenant-Code", tenantCode);
        request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }
}

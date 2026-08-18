using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Hemo.Pdf.Api;
using Hemo.Pdf.Api.Auth;
using Hemo.Pdf.Application;
using Hemo.Pdf.Core.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

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
        var response = await PostGenerateAsync("tenant-demo-a", "clinical-07-lab");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public async Task GeneratePdf_TenantA_And_TenantB_ProduceDifferentPdfs()
    {
        var responseA = await PostGenerateAsync("tenant-demo-a", "clinical-07-lab");
        var responseB = await PostGenerateAsync("tenant-demo-b", "clinical-07-lab");

        var bytesA = await responseA.Content.ReadAsByteArrayAsync();
        var bytesB = await responseB.Content.ReadAsByteArrayAsync();

        Assert.NotEqual(bytesA.Length, 0);
        Assert.NotEqual(bytesB.Length, 0);
        Assert.NotEqual(Convert.ToBase64String(bytesA), Convert.ToBase64String(bytesB));
    }

    [Fact]
    public async Task GeneratePdf_ClinicalPack_ReturnPdf()
    {
        var templates = new[]
        {
            ClinicalReportCatalog.HctEpo,
            ClinicalReportCatalog.EpoDrug,
            ClinicalReportCatalog.HemodialysisRecord,
            ClinicalReportCatalog.Prescription,
            ClinicalReportCatalog.ProgressNote,
            ClinicalReportCatalog.Medication,
            ClinicalReportCatalog.Lab,
            ClinicalReportCatalog.ConsentTh,
            ClinicalReportCatalog.ConsentEn,
            ClinicalReportCatalog.PatientData,
            ClinicalReportCatalog.Admission,
            ClinicalReportCatalog.EducationTh,
            ClinicalReportCatalog.EducationEn,
            ClinicalReportCatalog.MarMonth,
            ClinicalReportCatalog.HdSummary,
            ClinicalReportCatalog.AdequacySummary,
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
            "clinical-07-lab",
            new
            {
                reportTemplateId = "clinical-07-lab",
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

    [Fact]
    public async Task GeneratePdf_EntityIdMismatch_Returns400()
    {
        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "clinical-07-lab",
            new
            {
                reportTemplateId = "clinical-07-lab",
                tenantCode = "tenant-demo-a",
                entityId = "a",
                data = new { id = "b", patientName = "Test Patient" },
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePdf_BodyTenantMismatch_Returns403()
    {
        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "clinical-07-lab",
            new
            {
                reportTemplateId = "clinical-07-lab",
                tenantCode = "tenant-demo-b",
                entityId = "test-entity-1",
                data = new { patientName = "Test Patient" },
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
        var response = await PostPreviewAsync("tenant-demo-a", "clinical-07-lab");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("clinical-07-lab", root.GetProperty("meta").GetProperty("templateId").GetString());
        Assert.True(root.GetProperty("pages")[0].GetProperty("blocks").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Preview_ClinicalPack_ReturnDocument()
    {
        var templates = new[]
        {
            ClinicalReportCatalog.Lab,
            ClinicalReportCatalog.Prescription,
            ClinicalReportCatalog.HemodialysisRecord,
            ClinicalReportCatalog.Medication,
            ClinicalReportCatalog.ProgressNote,
            ClinicalReportCatalog.AdequacySummary,
            ClinicalReportCatalog.Admission,
            ClinicalReportCatalog.HdSummary,
            ClinicalReportCatalog.ConsentTh,
            ClinicalReportCatalog.HctEpo,
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
        var responseA = await PostPreviewAsync("tenant-demo-a", "clinical-07-lab");
        var responseB = await PostPreviewAsync("tenant-demo-b", "clinical-07-lab");

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
    public async Task Preview_UnsignedRequiredTemplate_ReturnsOk()
    {
        var response = await PostPreviewAsync(
            "tenant-demo-a",
            ClinicalReportCatalog.HemodialysisRecord,
            new
            {
                reportTemplateId = ClinicalReportCatalog.HemodialysisRecord,
                tenantCode = "tenant-demo-a",
                entityId = "session-1",
                data = new { patientName = "Test Patient" },
                signatures = new
                {
                    isFullySigned = false,
                    signatures = Array.Empty<object>(),
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePdf_UnsignedRequiredTemplate_Returns403()
    {
        var response = await PostGenerateAsync(
            "tenant-demo-a",
            ClinicalReportCatalog.HemodialysisRecord,
            new
            {
                reportTemplateId = ClinicalReportCatalog.HemodialysisRecord,
                tenantCode = "tenant-demo-a",
                entityId = "session-1",
                data = new { patientName = "Test Patient" },
                signatures = new
                {
                    isFullySigned = false,
                    signatures = Array.Empty<object>(),
                },
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
        using var original = JsonDocument.Parse(jsonText);
        using var data = JsonDocument.Parse(ForceUniquePlannerProfile(original.RootElement));
        var entityId = data.RootElement.GetProperty("id").GetString()!;

        var response = await PostPreviewAsync(
            "tenant-demo-a",
            "clinical-03-hemodialysis-record",
            new
            {
                reportTemplateId = "clinical-03-hemodialysis-record",
                tenantCode = "tenant-demo-a",
                entityId,
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
        var entityId = data.RootElement.GetProperty("id").GetString()!;

        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "clinical-03-hemodialysis-record",
            new
            {
                reportTemplateId = "clinical-03-hemodialysis-record",
                tenantCode = "tenant-demo-a",
                entityId,
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

        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output"));
        Directory.CreateDirectory(outDir);
        await File.WriteAllBytesAsync(Path.Combine(outDir, "hemosheet-thaiur.pdf"), bytes);

        var pageCount = CountPdfPages(bytes);
        // ThaiUR mock has 8 dialysis records; dense form may use a 2nd page when footer is tall.
        Assert.InRange(pageCount, 1, 2);
    }

    [Fact]
    public async Task GeneratePdf_HemosheetDefault_ReturnsSinglePagePdf()
    {
        var mockPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "assets", "mock-data", "template-04-hemosheet-hd-av.json"));

        var jsonText = await File.ReadAllTextAsync(mockPath);
        using var data = JsonDocument.Parse(jsonText);
        var entityId = data.RootElement.GetProperty("id").GetString()!;

        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "clinical-03-hemodialysis-record",
            new
            {
                reportTemplateId = "clinical-03-hemodialysis-record",
                tenantCode = "tenant-demo-a",
                entityId,
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

        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output"));
        Directory.CreateDirectory(outDir);
        await File.WriteAllBytesAsync(Path.Combine(outDir, "hemosheet-default.pdf"), bytes);

        var pageCount = CountPdfPages(bytes);
        Assert.Equal(1, pageCount);
    }

    [Fact]
    public async Task GeneratePdf_HemosheetDefaultHdf_IncludesSubstituteColumns_SinglePage()
    {
        var mockPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "assets", "mock-data", "template-04-hemosheet-hdf-av.json"));

        var jsonText = await File.ReadAllTextAsync(mockPath);
        using var data = JsonDocument.Parse(jsonText);
        var entityId = data.RootElement.GetProperty("id").GetString()!;

        var response = await PostGenerateAsync(
            "tenant-demo-a",
            "clinical-03-hemodialysis-record",
            new
            {
                reportTemplateId = "clinical-03-hemodialysis-record",
                tenantCode = "tenant-demo-a",
                entityId,
                data = data.RootElement,
                signatures = new
                {
                    isFullySigned = true,
                    signatures = new[] { new { signerName = "Nurse" } },
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'%', bytes[0]);

        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output"));
        Directory.CreateDirectory(outDir);
        await File.WriteAllBytesAsync(Path.Combine(outDir, "hemosheet-default-hdf.pdf"), bytes);

        Assert.Equal(1, CountPdfPages(bytes));
    }

    private static int CountPdfPages(byte[] pdf)
    {
        var text = System.Text.Encoding.ASCII.GetString(pdf);
        // Root page-tree count is reliable; "/Type /Page" can appear more than once per page.
        var tree = System.Text.RegularExpressions.Regex.Match(
            text,
            @"/Type\s*/Pages\b.*?/Count\s+(\d+)",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        if (tree.Success && int.TryParse(tree.Groups[1].Value, out var count) && count > 0)
            return count;

        return System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page[^s]").Count;
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

    /// <summary>
    /// Default/ThaiUr hemosheet preview is PDF-as-preview (empty pages). DOM block checks need UniquePlanner (Rama).
    /// </summary>
    private static string ForceUniquePlannerProfile(JsonElement root)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("layoutContext") || property.NameEquals("LayoutContext"))
                {
                    writer.WritePropertyName(property.Name);
                    writer.WriteStartObject();
                    var wroteProfile = false;
                    foreach (var inner in property.Value.EnumerateObject())
                    {
                        if (inner.NameEquals("layoutProfile") || inner.NameEquals("LayoutProfile"))
                        {
                            writer.WriteString(inner.Name, "Rama");
                            wroteProfile = true;
                        }
                        else
                        {
                            inner.WriteTo(writer);
                        }
                    }

                    if (!wroteProfile)
                        writer.WriteString("layoutProfile", "Rama");

                    writer.WriteEndObject();
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

public class JwtTokenValidationTests
{
    private const string TestIssuer = "http://localhost/";
    private const string TestKey = "NAmO0mtmIV4ZWSZ92vRlwj810XzFXsnH";

    [Fact]
    public void CreateParameters_ValidatesMatchingToken()
    {
        var parameters = JwtTokenValidation.CreateParameters(new JwtOptions
        {
            Issuer = TestIssuer,
            Key = TestKey,
            Audience = "",
        });

        var token = CreateToken(TestIssuer, TestIssuer, TestKey);
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, parameters, out _);
        Assert.Equal("user-1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void CreateParameters_RejectsWrongKey()
    {
        var parameters = JwtTokenValidation.CreateParameters(new JwtOptions
        {
            Issuer = TestIssuer,
            Key = TestKey,
        });

        var token = CreateToken(TestIssuer, TestIssuer, "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void CreateParameters_RejectsAudienceMismatch()
    {
        var parameters = JwtTokenValidation.CreateParameters(new JwtOptions
        {
            Issuer = TestIssuer,
            Key = TestKey,
            Audience = "",
        });

        var token = CreateToken(TestIssuer, "hemo-pdf", TestKey);
        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void EnsureProductionReady_Throws_WhenMockOutsideDevelopment()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtTokenValidation.EnsureProductionReady(
                new HemoPdfOptions { UseMockServices = true },
                isDevelopment: false));
    }

    private static string CreateToken(string issuer, string audience, string key)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim("tenant_code", "local")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>HTTP-level JWT auth tests (UseMockServices=false).</summary>
public class JwtHttpIntegrationTests : IClassFixture<JwtPdfApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string TestIssuer = "http://localhost/";
    private const string TestKey = "NAmO0mtmIV4ZWSZ92vRlwj810XzFXsnH";

    public JwtHttpIntegrationTests(JwtPdfApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Generate_WithoutToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pdf/generate")
        {
            Content = JsonContent.Create(new
            {
                reportTemplateId = "clinical-07-lab",
                tenantCode = "local",
                entityId = "e1",
                data = new { patientName = "A" },
            }),
        };
        request.Headers.Add("X-Tenant-Code", "local");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generate_WithValidToken_ReturnsPdf()
    {
        var token = CreateToken("local");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pdf/generate")
        {
            Content = JsonContent.Create(new
            {
                reportTemplateId = "clinical-07-lab",
                tenantCode = "local",
                entityId = "e1",
                data = new { patientName = "A" },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-Code", "local");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Generate_WithTenantHeaderMismatch_Returns403()
    {
        var token = CreateToken("local");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pdf/generate")
        {
            Content = JsonContent.Create(new
            {
                reportTemplateId = "clinical-07-lab",
                tenantCode = "local",
                entityId = "e1",
                data = new { patientName = "A" },
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-Code", "other-tenant");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateToken(string tenantCode)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestIssuer,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("tenant_code", tenantCode),
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class JwtPdfApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        var brandingPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "branding"));

        builder.UseSetting("HemoPdf:UseMockServices", "false");
        builder.UseSetting("HemoPdf:UseServerFetch", "false");
        builder.UseSetting("HemoPdf:BrandingRootPath", brandingPath);
        var templatesPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "templates"));
        builder.UseSetting("HemoPdf:TemplatesRootPath", templatesPath);
        builder.UseSetting("HemoPdf:CorsOrigins:0", "http://localhost:4200");
        builder.UseSetting("HemoPdf:Jwt:Issuer", "http://localhost/");
        builder.UseSetting("HemoPdf:Jwt:Key", "NAmO0mtmIV4ZWSZ92vRlwj810XzFXsnH");
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Production");
    }
}

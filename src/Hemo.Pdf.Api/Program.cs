using Hemo.Pdf.Application;
using Hemo.Pdf.Api.Auth;
using Hemo.Pdf.Api.Swagger;
using Hemo.Pdf.Api.Middleware;
using Hemo.Pdf.Core.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Hemo.Pdf.Api;

public sealed class Program
{
    /// <summary>Max JSON body size for generate/preview (base64 signatures inflate payloads).</summary>
    public const long MaxRequestBodyBytes = 8 * 1024 * 1024;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        QuestPDF.Settings.License = LicenseType.Community;

        builder.Services.AddHemoPdf(builder.Configuration);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxRequestBodyBytes;
        });
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
        });
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(swagger =>
        {
            swagger.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Development mock: ใส่ค่าอะไรก็ได้ เช่น `dev`. Production: Hemopro access token.",
            });
            swagger.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            });
            swagger.OperationFilter<GeneratePdfOperationFilter>();
            swagger.OperationFilter<ReportPreviewOperationFilter>();
        });

        var hemoPdfOptions = builder.Configuration
            .GetSection(HemoPdfOptions.SectionName)
            .Get<HemoPdfOptions>() ?? new HemoPdfOptions();

        JwtTokenValidation.EnsureProductionReady(hemoPdfOptions, builder.Environment.IsDevelopment());
        ConfigureAuthentication(builder, hemoPdfOptions);
        ConfigureCors(builder, hemoPdfOptions);
        ConfigureRateLimiting(builder.Services);

        var app = builder.Build();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                context.Response.Headers.CacheControl = "no-store";

                if (exception is PdfGenerationForbiddenException forbidden)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync(forbidden.Message);
                    return;
                }

                if (exception is PdfGenerationBadRequestException badRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(badRequest.Message);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var message = context.Request.Path.StartsWithSegments("/api/report")
                    ? "An unexpected error occurred while building the report preview."
                    : "An unexpected error occurred while generating the PDF.";
                await context.Response.WriteAsync(message);
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            });
            await next();
        });

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<TenantMiddleware>();
        app.UseRateLimiter();

        app.MapControllers();
        app.MapHealthChecks("/health");

        app.Run();
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder, HemoPdfOptions options)
    {
        var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        if (builder.Environment.IsDevelopment() && options.UseMockServices)
        {
            authBuilder.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, MockAuthHandler>(
                JwtBearerDefaults.AuthenticationScheme,
                _ => { });
            return;
        }

        var validationParameters = JwtTokenValidation.CreateParameters(options.Jwt);
        authBuilder.AddJwtBearer(jwtOptions =>
        {
            jwtOptions.TokenValidationParameters = validationParameters;
            jwtOptions.MapInboundClaims = false;
        });
    }

    private static void ConfigureCors(WebApplicationBuilder builder, HemoPdfOptions options)
    {
        builder.Services.AddCors(cors =>
        {
            cors.AddDefaultPolicy(policy =>
            {
                var origins = options.CorsOrigins;
                if (origins.Length == 0)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    private static void ConfigureRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiterOptions.AddPolicy("PdfGeneration", context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                var tenant = context.Items[Application.Mock.MockTenantContextAccessor.HttpContextItemKey] as string
                    ?? context.Request.Headers["X-Tenant-Code"].ToString();
                if (string.IsNullOrWhiteSpace(tenant))
                    tenant = "unknown-tenant";

                var partitionKey = $"{tenant}:{userId}";

                return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 3,
                    AutoReplenishment = true,
                });
            });
        });
    }
}

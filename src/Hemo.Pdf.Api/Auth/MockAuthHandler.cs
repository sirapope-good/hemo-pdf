using System.Security.Claims;
using System.Text.Encodings.Web;
using Hemo.Pdf.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Api.Auth;

public sealed class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly HemoPdfOptions _hemoPdfOptions;

    public MockAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment,
        IOptions<HemoPdfOptions> hemoPdfOptions)
        : base(options, logger, encoder)
    {
        _environment = environment;
        _hemoPdfOptions = hemoPdfOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        var hasBearer = !string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (!hasBearer
            && !(_environment.IsDevelopment() && _hemoPdfOptions.UseMockServices))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid Bearer token."));
        }

        return Task.FromResult(AuthenticateResult.Success(CreateMockTicket()));
    }

    private AuthenticationTicket CreateMockTicket()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mock-user"),
            new Claim(ClaimTypes.Name, "mock-user"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationTicket(principal, Scheme.Name);
    }
}

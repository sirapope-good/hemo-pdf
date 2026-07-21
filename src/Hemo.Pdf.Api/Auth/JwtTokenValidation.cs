using System.Text;
using Hemo.Pdf.Application;
using Microsoft.IdentityModel.Tokens;

namespace Hemo.Pdf.Api.Auth;

public static class JwtTokenValidation
{
    public const int MinimumKeyLength = 16;

    public static TokenValidationParameters CreateParameters(JwtOptions jwt)
    {
        ArgumentNullException.ThrowIfNull(jwt);

        if (string.IsNullOrWhiteSpace(jwt.Issuer))
            throw new InvalidOperationException("HemoPdf:Jwt:Issuer is required when mock auth is disabled.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"HemoPdf:Jwt:Key is required (min length {MinimumKeyLength}) when mock auth is disabled.");
        }

        var audience = string.IsNullOrWhiteSpace(jwt.Audience) ? jwt.Issuer : jwt.Audience;

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    }

    public static void EnsureProductionReady(HemoPdfOptions options, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.UseMockServices && !isDevelopment)
        {
            throw new InvalidOperationException(
                "HemoPdf:UseMockServices cannot be enabled outside the Development environment.");
        }

        if (!isDevelopment || !options.UseMockServices)
        {
            // Validate JWT options eagerly so misconfiguration fails at startup (non-mock path).
            _ = CreateParameters(options.Jwt);
        }
    }
}

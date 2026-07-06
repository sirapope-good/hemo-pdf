using System.Text.Json;
using System.Text.Json.Serialization;
using Hemo.Pdf.Branding.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hemo.Pdf.Branding;

public sealed class JsonFileBrandingStore : IBrandingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _rootPath;
    private readonly ILogger<JsonFileBrandingStore>? _logger;

    public JsonFileBrandingStore(IOptions<BrandingOptions> options, ILogger<JsonFileBrandingStore>? logger = null)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
        _logger = logger;
    }

    public async Task<CustomerBrandingProfile> GetByTenantCodeAsync(string tenantCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);

        var filePath = Path.Combine(_rootPath, $"{tenantCode}.json");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Branding profile not found for tenant '{tenantCode}'. Expected file: {filePath}");
        }

        await using var stream = File.OpenRead(filePath);
        var profile = await JsonSerializer.DeserializeAsync<CustomerBrandingProfile>(stream, JsonOptions, ct);
        if (profile is null)
        {
            throw new InvalidOperationException($"Branding profile file is empty or invalid: {filePath}");
        }

        _logger?.LogDebug("Loaded branding profile for tenant {TenantCode} from {FilePath}", tenantCode, filePath);
        return profile;
    }

    private static string ResolveRootPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}

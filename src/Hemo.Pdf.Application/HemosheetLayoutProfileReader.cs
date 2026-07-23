using System.Text.Json;
using Hemo.Pdf.Core.Models.Hemosheet;

namespace Hemo.Pdf.Application;

public static class HemosheetLayoutProfileReader
{
    public static bool IsThaiUr(JsonElement data) =>
        string.Equals(ReadLayoutProfile(data), nameof(HemosheetLayoutProfile.ThaiUr), StringComparison.OrdinalIgnoreCase);

    public static string? ReadLayoutProfile(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            return null;

        if (!data.TryGetProperty("layoutContext", out var layoutContext)
            && !data.TryGetProperty("LayoutContext", out layoutContext))
        {
            return null;
        }

        if (layoutContext.ValueKind != JsonValueKind.Object)
            return null;

        if (!layoutContext.TryGetProperty("layoutProfile", out var profile)
            && !layoutContext.TryGetProperty("LayoutProfile", out profile))
        {
            return null;
        }

        if (profile.ValueKind == JsonValueKind.String)
        {
            var text = profile.GetString()?.Trim();
            if (string.IsNullOrEmpty(text))
                return null;

            if (string.Equals(text, "2", StringComparison.Ordinal))
                return nameof(HemosheetLayoutProfile.ThaiUr);

            return text;
        }

        if (profile.ValueKind == JsonValueKind.Number && profile.TryGetInt32(out var numeric))
        {
            if (Enum.IsDefined(typeof(HemosheetLayoutProfile), numeric))
                return ((HemosheetLayoutProfile)numeric).ToString();
        }

        return null;
    }
}

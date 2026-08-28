using System.Text.Json.Serialization;
using Hemo.Pdf.Core.Hprp.Table;

namespace Hemo.Pdf.Core.Hprp;

/// <summary>
/// Multi-element recipe for Studio Library (e.g. co-pay banner + NHSO/SSO tables).
/// Insert flattens <see cref="Elements"/> into the pack layout — not a runtime group wrapper.
/// </summary>
public sealed class HprpFragmentPreset
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>Optional filters e.g. <c>clinical</c>, <c>tenant:hogwarts</c>.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [JsonPropertyName("elements")]
    public IReadOnlyList<HprpDesignerElement> Elements { get; init; } = [];
}

public static class HprpFragmentValidator
{
    public static IReadOnlyList<string> Validate(HprpFragmentPreset? fragment)
    {
        var errors = new List<string>();
        if (fragment is null)
        {
            errors.Add("fragment is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(fragment.Id))
            errors.Add("fragment.id is required.");

        if (fragment.Elements is not { Count: > 0 })
        {
            errors.Add("fragment.elements must contain at least one element.");
            return errors;
        }

        for (var i = 0; i < fragment.Elements.Count; i++)
        {
            var el = fragment.Elements[i];
            var path = $"fragment.elements[{i}]";
            if (string.IsNullOrWhiteSpace(el.Type))
                errors.Add($"{path}.type is required.");
            else if (!HprpDesignerElementTypes.All.Contains(el.Type))
                errors.Add($"{path}.type '{el.Type}' is unknown.");

            var place = (el.Place ?? "below").Trim().ToLowerInvariant();
            if (place is not ("below" or "beside"))
                errors.Add($"{path}.place must be below or beside.");

            if (place == "beside" && i == 0)
                errors.Add($"{path} cannot start a fragment with place=beside.");
        }

        return errors;
    }
}

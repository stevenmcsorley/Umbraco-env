namespace SiteKit.Core.Projects;

using SiteKit.Core.Components;

public interface ISiteKitProjectRegistry
{
    Task<IReadOnlyList<Components.SiteKitProjectManifest>> ListAsync(CancellationToken cancellationToken = default);
    Task<Components.SiteKitProjectManifest?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<Components.SiteKitProjectManifest> SaveAsync(Components.SiteKitProjectManifest manifest, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public static class SiteKitProjectValidator
{
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
        { "website", "application", "combined", "api", "content" };

    public static IReadOnlyList<string> Validate(Components.SiteKitProjectManifest manifest)
    {
        if (manifest is null) return ["project is required."];
        var errors = new List<string>();
        if (manifest.SchemaVersion != 1) errors.Add("schemaVersion must be 1.");
        if (!IsAlias(manifest.Id)) errors.Add("id must be a lowercase URL-safe identifier.");
        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 120) errors.Add("name is required and must be at most 120 characters.");
        if (!Categories.Contains(manifest.Category)) errors.Add("category must be website, application, combined, api or content.");
        if ((manifest.Description?.Length ?? 0) > 2000) errors.Add("description must be at most 2000 characters.");
        if (!IsAlias(manifest.Theme)) errors.Add("theme must be a lowercase URL-safe identifier.");
        ValidateIds(manifest.Modules, "modules", errors);
        ValidateIds(manifest.Components, "components", errors);
        return errors;
    }

    public static bool IsValidId(string? value) => IsAlias(value);

    private static void ValidateIds(string[]? values, string field, List<string> errors)
    {
        if (values is null) { errors.Add($"{field} is required."); return; }
        if (values.Length > 500) errors.Add($"{field} cannot contain more than 500 identifiers.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
            if (!IsAlias(value) || !seen.Add(value)) errors.Add($"{field} contains an invalid or duplicate identifier.");
    }

    private static bool IsAlias(string? value)
        => value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z][a-z0-9-]{1,63}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}

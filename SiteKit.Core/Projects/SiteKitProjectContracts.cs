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
        if (manifest.ThemeSettings is null) errors.Add("themeSettings are required.");
        if ((manifest.Description?.Length ?? 0) > 2000) errors.Add("description must be at most 2000 characters.");
        if (!IsAlias(manifest.Theme)) errors.Add("theme must be a lowercase URL-safe identifier.");
        if (manifest.ThemeSettings is not null && (!System.Text.RegularExpressions.Regex.IsMatch(manifest.ThemeSettings.PrimaryColor, "^#[0-9a-fA-F]{6}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(manifest.ThemeSettings.SurfaceColor, "^#[0-9a-fA-F]{6}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(manifest.ThemeSettings.TextColor, "^#[0-9a-fA-F]{6}$")))
            errors.Add("theme colors must be six-digit hexadecimal values.");
        if (manifest.ThemeSettings is not null && !System.Text.RegularExpressions.Regex.IsMatch(manifest.ThemeSettings.Radius, "^(?:0|[0-9]{1,2}(?:\\.[0-9]{1,2})?(?:px|rem|em))$"))
            errors.Add("theme radius must be zero or a bounded CSS length.");
        if (manifest.ThemeSettings is not null && manifest.ThemeSettings.FontFamily is not ("system" or "serif" or "sans-serif" or "monospace"))
            errors.Add("theme fontFamily must be system, serif, sans-serif or monospace.");
        ValidateIds(manifest.Modules, "modules", errors);
        if (manifest.ModuleVersions is null) errors.Add("moduleVersions are required.");
        else
        {
            if (manifest.Modules is null || manifest.ModuleVersions.Keys.Any(id => !manifest.Modules.Contains(id, StringComparer.Ordinal))) errors.Add("moduleVersions may only pin modules listed on the project.");
            foreach (var (id, version) in manifest.ModuleVersions)
                if (!IsAlias(id) || !System.Text.RegularExpressions.Regex.IsMatch(version ?? string.Empty, "^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?$"))
                    errors.Add("moduleVersions contains an invalid module id or semantic version.");
        }
        ValidateIds(manifest.Components, "components", errors);
        ValidateIds(manifest.Entities, "entities", errors);
        ValidateIds(manifest.Screens, "screens", errors);
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

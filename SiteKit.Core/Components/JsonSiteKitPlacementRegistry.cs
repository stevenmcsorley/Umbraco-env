using System.Text.Json;

namespace SiteKit.Core.Components;

/// <summary>Stores references to shared components and project-local placement data.</summary>
public sealed class JsonSiteKitPlacementRegistry : ISiteKitPlacementRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSiteKitPlacementRegistry(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<SiteKitComponentPlacement>> ListAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var file = GetFile(projectId, "placements");
        if (!File.Exists(file)) return [];
        try
        {
            await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 8192, FileOptions.Asynchronous);
            return await JsonSerializer.DeserializeAsync<SiteKitComponentPlacement[]>(stream, JsonOptions, cancellationToken) ?? [];
        }
        catch (JsonException) { return []; }
    }

    public async Task<SiteKitComponentPlacement?> GetAsync(string projectId, string placementId, CancellationToken cancellationToken = default)
        => (await ListAsync(projectId, cancellationToken)).FirstOrDefault(x => x.Id == placementId);

    public async Task<SiteKitComponentPlacement> SaveAsync(string projectId, SiteKitComponentPlacement placement, CancellationToken cancellationToken = default)
    {
        ValidatePlacement(placement);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var existing = (await ListAsync(projectId, cancellationToken)).ToDictionary(x => x.Id, StringComparer.Ordinal);
            existing[placement.Id] = placement;
            await WriteAsync(projectId, existing.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(), cancellationToken);
            return placement;
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string projectId, string placementId, CancellationToken cancellationToken = default)
    {
        ValidateId(placementId, nameof(placementId));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var existing = (await ListAsync(projectId, cancellationToken)).ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (!existing.Remove(placementId)) return false;
            await WriteAsync(projectId, existing.Values.ToArray(), cancellationToken);
            return true;
        }
        finally { _gate.Release(); }
    }

    public static IReadOnlyList<string> ValidatePlacement(SiteKitComponentPlacement placement)
    {
        var errors = new List<string>();
        if (placement is null) return ["placement is required."];
        if (!IsId(placement.Id)) errors.Add("placement id must be a URL-safe identifier.");
        if (!IsId(placement.ComponentAlias)) errors.Add("componentAlias must be a URL-safe identifier.");
        if (placement.Version is not null && !Version.TryParse(placement.Version, out _)) errors.Add("version must be a numeric semantic version.");
        if (placement.Variant is not null && !IsId(placement.Variant)) errors.Add("variant must be a URL-safe identifier.");
        if (placement.StyleClass is { Length: > 120 } || (placement.StyleClass is not null && !System.Text.RegularExpressions.Regex.IsMatch(placement.StyleClass, "^[a-zA-Z0-9 _-]{0,120}$")))
            errors.Add("styleClass contains unsupported characters.");
        if (placement.Appearance is { } appearance)
            errors.AddRange(ValidateAppearance(appearance));
        if (placement.Data.ValueKind != JsonValueKind.Object) errors.Add("data must be a JSON object.");
        return errors;
    }

    public static IReadOnlyList<string> ValidateAppearance(SiteKitComponentAppearance appearance)
    {
        var errors = new List<string>();
        if (appearance.Color is not null && !SiteKitComponentValidator.IsSafeColor(appearance.Color)) errors.Add("appearance.color is invalid.");
        if (appearance.BackgroundColor is not null && !SiteKitComponentValidator.IsSafeColor(appearance.BackgroundColor)) errors.Add("appearance.backgroundColor is invalid.");
        if (appearance.BorderColor is not null && !SiteKitComponentValidator.IsSafeColor(appearance.BorderColor)) errors.Add("appearance.borderColor is invalid.");
        if (!IsLength(appearance.BorderRadius) || !IsLength(appearance.Padding)) errors.Add("appearance lengths must use a safe CSS unit or zero.");
        return errors;
    }

    private async Task WriteAsync(string projectId, SiteKitComponentPlacement[] placements, CancellationToken cancellationToken)
    {
        var file = GetFile(projectId, "placements");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                await JsonSerializer.SerializeAsync(stream, placements, JsonOptions, cancellationToken);
            File.Move(temp, file, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private string GetFile(string projectId, string fileName)
    {
        ValidateId(projectId, nameof(projectId));
        return Path.Combine(_root, projectId, fileName + ".json");
    }

    private static void ValidateId(string value, string name)
    {
        if (!IsId(value)) throw new ArgumentException("Identifier is invalid.", name);
    }

    private static bool IsId(string? value)
        => value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z][a-z0-9-]{1,63}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static bool IsLength(string? value)
        => value is null || System.Text.RegularExpressions.Regex.IsMatch(value, "^(?:0|[0-9]{1,3}(?:\\.[0-9]{1,2})?(?:px|rem|em|%))$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}

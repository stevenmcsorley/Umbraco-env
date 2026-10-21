using System.Text.Json;
using System.Text.RegularExpressions;

namespace SiteKit.Core.Data;

public sealed record SiteKitEntityDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public required string ProjectId { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public int Version { get; init; } = 1;
    public string[] Permissions { get; init; } = ["read", "create", "update", "delete"];
    public SiteKitEntityField[] Fields { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record SiteKitEntityField
{
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public bool Unique { get; init; }
    public string? RelatedEntity { get; init; }
    public int? MaximumLength { get; init; }
}

public sealed record SiteKitEntityRecord
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string EntityAlias { get; init; }
    public int SchemaVersion { get; init; }
    public JsonElement Data { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SiteKitEntityPermissionGrant
{
    public required string ProjectId { get; init; }
    public required string EntityAlias { get; init; }
    public required string Principal { get; init; }
    public required string[] Operations { get; init; }
}

public interface ISiteKitPermissionRegistry
{
    Task<IReadOnlyList<SiteKitEntityPermissionGrant>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default);
    Task<SiteKitEntityPermissionGrant?> GetAsync(string projectId, string entityAlias, string principal, CancellationToken cancellationToken = default);
    Task<SiteKitEntityPermissionGrant> SaveAsync(SiteKitEntityPermissionGrant grant, CancellationToken cancellationToken = default);
}

public static partial class SiteKitEntityValidator
{
    private static readonly HashSet<string> FieldTypes = new(StringComparer.Ordinal)
        { "text", "email", "number", "boolean", "date", "datetime", "json", "relationship", "relationships" };

    public static IReadOnlyList<string> Validate(SiteKitEntityDefinition definition)
    {
        var errors = new List<string>();
        if (definition.SchemaVersion != 1) errors.Add("schemaVersion must be 1.");
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(definition.ProjectId)) errors.Add("projectId is invalid.");
        if (!IsAlias(definition.Alias)) errors.Add("entity alias must be lowercase and URL safe.");
        if (string.IsNullOrWhiteSpace(definition.Name) || definition.Name.Length > 120) errors.Add("entity name is required and must be at most 120 characters.");
        if (definition.Version < 1) errors.Add("entity version must be positive.");
        if (definition.Fields is null || definition.Fields.Length is 0 or > 100) errors.Add("an entity must contain between 1 and 100 fields.");
        else
        {
            var aliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in definition.Fields)
            {
                if (!IsAlias(field.Alias) || !aliases.Add(field.Alias)) errors.Add($"field alias '{field.Alias}' is invalid or duplicated.");
                if (string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > 120) errors.Add($"field '{field.Alias}' needs a valid name.");
                if (!FieldTypes.Contains(field.Type)) errors.Add($"field '{field.Alias}' has an unsupported type.");
                if (field.Type is "relationship" or "relationships" && !IsAlias(field.RelatedEntity)) errors.Add($"relationship field '{field.Alias}' needs a related entity alias.");
                if (field.MaximumLength is < 1 or > 4000) errors.Add($"field '{field.Alias}' has an invalid maximum length.");
            }
            if (!aliases.Contains("id")) errors.Add("an entity must declare an id field.");
        }
        if (definition.Permissions is null || definition.Permissions.Length == 0 || definition.Permissions.Any(p => p is not ("read" or "create" or "update" or "delete")) || definition.Permissions.Distinct(StringComparer.Ordinal).Count() != definition.Permissions.Length)
            errors.Add("permissions must be unique values from read, create, update and delete.");
        return errors;
    }

    public static IReadOnlyList<string> ValidateRecord(SiteKitEntityDefinition definition, JsonElement data)
    {
        var errors = new List<string>();
        if (data.ValueKind != JsonValueKind.Object) return ["record data must be an object."];
        var declared = definition.Fields.Select(f => f.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var property in data.EnumerateObject())
        {
            var field = definition.Fields.FirstOrDefault(f => f.Alias == property.Name);
            if (field is null) { errors.Add($"field '{property.Name}' is not declared in this schema."); continue; }
            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                if (field.Required) errors.Add($"field '{field.Alias}' is required.");
                continue;
            }
            var valid = field.Type switch
            {
                "text" => property.Value.ValueKind == JsonValueKind.String && (field.MaximumLength is null || property.Value.GetString()!.Length <= field.MaximumLength),
                "email" => property.Value.ValueKind == JsonValueKind.String && property.Value.GetString()!.Length <= 320 && EmailRegex().IsMatch(property.Value.GetString()!),
                "number" => property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out _),
                "boolean" => property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "date" => property.Value.ValueKind == JsonValueKind.String && DateOnly.TryParse(property.Value.GetString(), out _),
                "datetime" => property.Value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(property.Value.GetString(), out _),
                "json" => property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False,
                "relationship" => property.Value.ValueKind == JsonValueKind.String && Guid.TryParse(property.Value.GetString(), out _),
                "relationships" => property.Value.ValueKind == JsonValueKind.Array && property.Value.EnumerateArray().All(value => value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out _)),
                _ => false
            };
            if (!valid) errors.Add($"field '{field.Alias}' is not a valid {field.Type} value.");
        }
        foreach (var required in definition.Fields.Where(f => f.Alias != "id" && f.Required && !data.TryGetProperty(f.Alias, out _))) errors.Add($"field '{required.Alias}' is required.");
        if (data.TryGetProperty("id", out var id) && (id.ValueKind != JsonValueKind.String || !Guid.TryParse(id.GetString(), out _))) errors.Add("field 'id' must be a UUID string.");
        return errors;
    }

    private static bool IsAlias(string? alias) => alias is not null && AliasRegex().IsMatch(alias);

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)] private static partial Regex AliasRegex();
    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.CultureInvariant)] private static partial Regex EmailRegex();
}

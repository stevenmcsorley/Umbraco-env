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

public sealed record SiteKitProjectRole
{
    public int SchemaVersion { get; init; } = 1;
    public required string ProjectId { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public string[] Members { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class SiteKitProjectRoleValidator
{
    public static IReadOnlyList<string> Validate(SiteKitProjectRole role)
    {
        var errors = new List<string>();
        if (role.SchemaVersion != 1) errors.Add("Role schemaVersion must be 1.");
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(role.ProjectId)) errors.Add("Role projectId is invalid.");
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(role.Alias)) errors.Add("Role alias is invalid.");
        if (string.IsNullOrWhiteSpace(role.Name) || role.Name.Length > 120) errors.Add("Role name is required and must be at most 120 characters.");
        if (role.Members is null || role.Members.Length > 500 || role.Members.Any(member => string.IsNullOrWhiteSpace(member) || member.Length > 320 || !Regex.IsMatch(member, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.CultureInvariant)) || role.Members.Distinct(StringComparer.OrdinalIgnoreCase).Count() != role.Members.Length)
            errors.Add("Role members must be at most 500 unique email addresses.");
        return errors;
    }
}

public sealed record SiteKitApplicationScreenDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public required string ProjectId { get; init; }
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? EntityAlias { get; init; }
    public string[] Columns { get; init; } = [];
    public string[] FormFields { get; init; } = [];
    public string? GroupField { get; init; }
    public string? ChartValueField { get; init; }
    public string[] Components { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class SiteKitScreenValidator
{
    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
        { "table", "detail", "form", "dashboard", "kanban", "calendar", "search", "approval", "chart" };

    public static IReadOnlyList<string> Validate(SiteKitApplicationScreenDefinition screen, SiteKitEntityDefinition? entity = null)
    {
        var errors = new List<string>();
        if (screen.SchemaVersion != 1) errors.Add("screen schemaVersion must be 1.");
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(screen.ProjectId)) errors.Add("screen projectId is invalid.");
        if (!SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(screen.Alias)) errors.Add("screen alias is invalid.");
        if (string.IsNullOrWhiteSpace(screen.Name) || screen.Name.Length > 120) errors.Add("screen name is required and must be at most 120 characters.");
        if (!Types.Contains(screen.Type)) errors.Add("screen type is unsupported.");
        if (screen.EntityAlias is not null && !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(screen.EntityAlias)) errors.Add("screen entityAlias is invalid.");
        if (screen.Type is "table" or "detail" or "form" or "kanban" or "calendar" or "search" or "approval" or "chart" && screen.EntityAlias is null) errors.Add($"{screen.Type} screens require an entityAlias.");
        if (screen.Columns is null || screen.FormFields is null || screen.Components is null) errors.Add("screen columns, formFields and components are required arrays.");
        else
        {
            if (screen.Columns.Length > 100 || screen.Columns.Distinct(StringComparer.Ordinal).Count() != screen.Columns.Length || screen.Columns.Any(field => !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(field))) errors.Add("screen columns contain an invalid or duplicate field alias.");
            if (screen.FormFields.Length > 100 || screen.FormFields.Distinct(StringComparer.Ordinal).Count() != screen.FormFields.Length || screen.FormFields.Any(field => !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(field))) errors.Add("screen formFields contain an invalid or duplicate field alias.");
            if (screen.Components.Length > 100 || screen.Components.Distinct(StringComparer.Ordinal).Count() != screen.Components.Length || screen.Components.Any(component => !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(component))) errors.Add("screen components contain an invalid or duplicate alias.");
        }
        if (screen.GroupField is not null && !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(screen.GroupField)) errors.Add("screen groupField is invalid.");
        if (screen.Type == "kanban" && screen.GroupField is null) errors.Add("kanban screens require a groupField.");
        if (screen.Type == "chart" && screen.GroupField is null) errors.Add("chart screens require a groupField.");
        if (screen.ChartValueField is not null && !SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(screen.ChartValueField)) errors.Add("screen chartValueField is invalid.");
        if (entity is not null && screen.Columns is not null && screen.FormFields is not null)
        {
            if (screen.EntityAlias != entity.Alias || screen.ProjectId != entity.ProjectId) errors.Add("screen entity must belong to the same project.");
            var fields = entity.Fields.Select(field => field.Alias).ToHashSet(StringComparer.Ordinal);
            if (screen.Columns.Concat(screen.FormFields).Any(field => !fields.Contains(field))) errors.Add("screen references a field that is not defined on its entity.");
            if (screen.GroupField is not null && !fields.Contains(screen.GroupField)) errors.Add("screen groupField is not defined on its entity.");
            if (screen.ChartValueField is not null && !entity.Fields.Any(field => field.Alias == screen.ChartValueField && field.Type == "number")) errors.Add("screen chartValueField must reference a numeric field on its entity.");
        }
        return errors;
    }
}

public interface ISiteKitPermissionRegistry
{
    Task<IReadOnlyList<SiteKitEntityPermissionGrant>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default);
    Task<SiteKitEntityPermissionGrant?> GetAsync(string projectId, string entityAlias, string principal, CancellationToken cancellationToken = default);
    Task<SiteKitEntityPermissionGrant> SaveAsync(SiteKitEntityPermissionGrant grant, CancellationToken cancellationToken = default);
}

public interface ISiteKitProjectRoleRegistry
{
    Task<IReadOnlyList<SiteKitProjectRole>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<SiteKitProjectRole?> GetAsync(string projectId, string alias, CancellationToken cancellationToken = default);
    Task<SiteKitProjectRole> SaveAsync(SiteKitProjectRole role, CancellationToken cancellationToken = default);
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

using System.Text.Json;
using System.Text.RegularExpressions;

namespace SiteKit.Core.Components;

public static partial class SiteKitComponentValidator
{
    private static readonly HashSet<string> PropertyTypes = new(StringComparer.Ordinal)
    {
        "text", "richText", "number", "boolean", "color", "media", "link", "reference", "collection", "object"
    };

    private static readonly HashSet<string> LayoutTypes = new(StringComparer.Ordinal)
    {
        "section", "container", "stack", "grid", "card", "heading", "text", "button", "image", "field", "list", "repeat", "accordion"
    };

    private static readonly Regex AliasPattern = AliasRegex();
    private static readonly Regex StyleClassPattern = StyleClassRegex();
    private static readonly Regex HexColorPattern = HexColorRegex();
    private static readonly HashSet<string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "white", "red", "green", "blue", "gray", "grey", "orange", "purple", "transparent", "currentColor"
    };

    public static IReadOnlyList<string> ValidateDefinition(SiteKitComponentDefinition definition)
    {
        var errors = new List<string>();
        if (definition is null)
            return ["definition is required."];

        if (definition.SchemaVersion != 1)
            errors.Add("schemaVersion must be 1.");
        if (string.IsNullOrWhiteSpace(definition.Alias) || !AliasPattern.IsMatch(definition.Alias))
            errors.Add("alias must be a lowercase URL-safe identifier between 2 and 64 characters.");
        if (string.IsNullOrWhiteSpace(definition.Name) || definition.Name.Length > 100)
            errors.Add("name is required and must be at most 100 characters.");
        if (string.IsNullOrWhiteSpace(definition.Category) || definition.Category.Length > 60)
            errors.Add("category is required and must be at most 60 characters.");
        if (definition.Description?.Length > 2000)
            errors.Add("description must be at most 2000 characters.");
        if (!IsVersion(definition.Version ?? string.Empty))
            errors.Add("version must be a numeric semantic version such as 1.0.0.");
        if (definition.Tags is null || definition.Tags.Length > 50)
            errors.Add("tags are required and cannot contain more than 50 values.");
        else
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in definition.Tags)
                if (string.IsNullOrWhiteSpace(tag) || tag.Length > 64 || tag != tag.Trim() || tag.Any(char.IsControl) || !tags.Add(tag))
                    errors.Add("tags must be unique, trimmed, non-empty values of at most 64 characters.");
        }
        if (definition.Dependencies is null || definition.Dependencies.Length > 50)
            errors.Add("dependencies are required and cannot contain more than 50 component aliases.");
        else
        {
            var dependencies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var dependency in definition.Dependencies)
                if (!AliasPattern.IsMatch(dependency) || dependency == definition.Alias || !dependencies.Add(dependency))
                    errors.Add("dependencies must be unique valid component aliases and cannot include the component itself.");
        }
        if (definition.Properties is null)
            return errors.Append("properties is required.").ToArray();
        if (definition.Layout is null)
            return errors.Append("layout is required.").ToArray();
        if (definition.Properties.Count > 50)
            errors.Add("A component can define at most 50 top-level properties.");

        ValidateProperties(definition.Properties, "properties", errors, 0);
        ValidateLayout(definition.Layout, definition.Properties, "layout", errors, 0, new HashSet<string>(StringComparer.Ordinal));

        var variantAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variant in definition.Variants ?? [])
        {
            if (variant is null) { errors.Add("variants cannot contain null entries."); continue; }
            if (!AliasPattern.IsMatch(variant.Alias) || !variantAliases.Add(variant.Alias))
                errors.Add($"variant alias '{variant.Alias}' is invalid or duplicated.");
            if (string.IsNullOrWhiteSpace(variant.Label) || variant.Label.Length > 80)
                errors.Add($"variant '{variant.Alias}' requires a label of at most 80 characters.");
            if (variant.StyleClass is not null && !StyleClassPattern.IsMatch(variant.StyleClass))
                errors.Add($"variant '{variant.Alias}' has an invalid styleClass.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateData(SiteKitComponentDefinition definition, JsonElement data)
    {
        var errors = new List<string>();
        if (data.ValueKind != JsonValueKind.Object)
        {
            errors.Add("data must be a JSON object.");
            return errors;
        }

        ValidateObject(data, definition.Properties, "data", errors, 0);
        return errors;
    }

    public static bool IsSafeLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Contains('\\'))
            return false;
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal))
            return true;
        if (value.StartsWith('#') || value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    public static bool IsSafeMediaUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Contains('\\'))
            return false;
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal))
            return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    public static bool IsSafeColor(string? value)
        => value is not null && (HexColorPattern.IsMatch(value) || NamedColors.Contains(value));

    private static void ValidateProperties(IReadOnlyList<SiteKitComponentProperty> properties, string path, List<string> errors, int depth)
    {
        if (properties is null)
        {
            errors.Add($"{path} is required.");
            return;
        }
        if (depth > 4)
        {
            errors.Add($"{path} exceeds the maximum nested property depth of 4.");
            return;
        }

        var aliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (property is null) { errors.Add($"{path} cannot contain null entries."); continue; }
            var currentPath = $"{path}.{property.Alias}";
            if (!AliasPattern.IsMatch(property.Alias) || !aliases.Add(property.Alias))
                errors.Add($"{currentPath} has an invalid or duplicated alias.");
            if (string.IsNullOrWhiteSpace(property.Label) || property.Label.Length > 100)
                errors.Add($"{currentPath} needs a label of at most 100 characters.");
            if (!PropertyTypes.Contains(property.Type))
                errors.Add($"{currentPath} uses unsupported property type '{property.Type}'.");
            if (property.HelpText?.Length > 500)
                errors.Add($"{currentPath}.helpText must be at most 500 characters.");
            if (property.MinLength < 0 || property.MaxLength < 0 ||
                (property.MinLength.HasValue && property.MaxLength.HasValue && property.MinLength > property.MaxLength))
                errors.Add($"{currentPath} has invalid length constraints.");
            if (property.Minimum.HasValue && property.Maximum.HasValue && property.Minimum > property.Maximum)
                errors.Add($"{currentPath} has an invalid numeric range.");
            if (property.Type is "object" or "collection")
            {
                if (property.Children is null || property.Children.Count == 0)
                    errors.Add($"{currentPath} requires child properties.");
                if (property.Children is not null) ValidateProperties(property.Children, currentPath, errors, depth + 1);
            }
            else if (property.Children is { Count: > 0 })
            {
                errors.Add($"{currentPath} cannot define child properties unless it is an object or collection.");
            }
        }
    }

    private static void ValidateLayout(
        SiteKitLayoutNode node,
        IReadOnlyList<SiteKitComponentProperty> rootProperties,
        string path,
        List<string> errors,
        int depth,
        HashSet<string> itemAliases)
    {
        if (node is null)
        {
            errors.Add($"{path} is required.");
            return;
        }
        if (depth > 8)
        {
            errors.Add($"{path} exceeds the maximum layout depth of 8.");
            return;
        }
        if (!LayoutTypes.Contains(node.Type))
            errors.Add($"{path} uses unsupported layout type '{node.Type}'.");
        if (node.StyleClass is not null && !StyleClassPattern.IsMatch(node.StyleClass))
            errors.Add($"{path}.styleClass contains unsupported characters.");
        if (node.Children is null)
        {
            errors.Add($"{path}.children is required.");
            return;
        }
        if (node.Children.Count > 100)
            errors.Add($"{path} cannot contain more than 100 child nodes.");

        if (node.Type == "grid" && node.Columns is not (>= 1 and <= 6))
            errors.Add($"{path}.columns must be between 1 and 6.");
        if (node.Type == "heading" && node.HeadingLevel is not (>= 1 and <= 6))
            errors.Add($"{path}.headingLevel must be between 1 and 6.");
        if (node.Type == "repeat")
        {
            var repeatProperty = FindProperty(node.Bind, rootProperties, itemAliases);
            if (repeatProperty?.Type != "collection")
                errors.Add($"{path}.bind must reference a collection property.");
            if (string.IsNullOrWhiteSpace(node.ItemAlias) || !AliasPattern.IsMatch(node.ItemAlias))
                errors.Add($"{path}.itemAlias must be a valid alias.");
        }
        else if (node.Bind is not null && FindProperty(node.Bind, rootProperties, itemAliases) is null)
        {
            errors.Add($"{path}.bind references an unknown property '{node.Bind}'.");
        }
        if (node.Type is "text" or "heading" or "button" && node.Bind is null && string.IsNullOrWhiteSpace(node.Text))
            errors.Add($"{path} requires either bind or text.");
        if (node.Type == "accordion")
        {
            if (node.Bind is null && string.IsNullOrWhiteSpace(node.Text))
                errors.Add($"{path} requires a text binding or static summary text.");
            if (node.Bind is not null && FindProperty(node.Bind, rootProperties, itemAliases)?.Type != "text")
                errors.Add($"{path}.bind must reference a text property.");
        }
        if (node.Type == "image" && FindProperty(node.Bind, rootProperties, itemAliases)?.Type != "media")
            errors.Add($"{path} requires a media property binding.");
        if (node.Type == "button" && node.Bind is not null && FindProperty(node.Bind, rootProperties, itemAliases)?.Type != "link")
            errors.Add($"{path} must bind to a link property.");

        var nestedAliases = new HashSet<string>(itemAliases, StringComparer.Ordinal);
        if (node.Type == "repeat" && node.ItemAlias is not null)
            nestedAliases.Add(node.ItemAlias);
        for (var index = 0; index < node.Children.Count; index++)
            ValidateLayout(node.Children[index], rootProperties, $"{path}.children[{index}]", errors, depth + 1, nestedAliases);
    }

    private static SiteKitComponentProperty? FindProperty(string? path, IReadOnlyList<SiteKitComponentProperty> properties, HashSet<string> itemAliases)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return null;

        var segmentIndex = 0;
        IReadOnlyList<SiteKitComponentProperty> currentProperties = properties;
        if (itemAliases.Contains(segments[0]))
            segmentIndex++;
        for (; segmentIndex < segments.Length; segmentIndex++)
        {
            var match = currentProperties?.FirstOrDefault(property => property is not null && property.Alias == segments[segmentIndex]);
            if (match is null)
                return null;
            if (segmentIndex == segments.Length - 1)
                return match;
            currentProperties = match.Children;
        }
        return null;
    }

    private static void ValidateObject(JsonElement value, IReadOnlyList<SiteKitComponentProperty> properties, string path, List<string> errors, int depth)
    {
        if (depth > 6)
        {
            errors.Add($"{path} exceeds the maximum data depth of 6.");
            return;
        }

        foreach (var unknown in value.EnumerateObject())
        {
            if (!properties.Any(property => property.Alias == unknown.Name))
                errors.Add($"{path}.{unknown.Name} is not defined in the component schema.");
        }

        foreach (var property in properties)
        {
            var propertyPath = $"{path}.{property.Alias}";
            if (!value.TryGetProperty(property.Alias, out var propertyValue))
            {
                if (property.Required && property.DefaultValue is null)
                    errors.Add($"{propertyPath} is required.");
                continue;
            }
            if (propertyValue.ValueKind == JsonValueKind.Null)
            {
                if (property.Required)
                    errors.Add($"{propertyPath} cannot be null.");
                continue;
            }

            switch (property.Type)
            {
                case "text":
                case "richText":
                    if (propertyValue.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"{propertyPath} must be text.");
                        break;
                    }
                    var text = propertyValue.GetString() ?? string.Empty;
                    if (property.MinLength.HasValue && text.Length < property.MinLength ||
                        property.MaxLength.HasValue && text.Length > property.MaxLength)
                        errors.Add($"{propertyPath} is outside its length limits.");
                    break;
                case "number":
                    if (propertyValue.ValueKind != JsonValueKind.Number || !propertyValue.TryGetDecimal(out var number))
                        errors.Add($"{propertyPath} must be a number.");
                    else if (property.Minimum.HasValue && number < property.Minimum || property.Maximum.HasValue && number > property.Maximum)
                        errors.Add($"{propertyPath} is outside its numeric limits.");
                    break;
                case "boolean":
                    if (propertyValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        errors.Add($"{propertyPath} must be true or false.");
                    break;
                case "color":
                    if (propertyValue.ValueKind != JsonValueKind.String || !IsSafeColor(propertyValue.GetString()))
                        errors.Add($"{propertyPath} must be a supported color value.");
                    break;
                case "media":
                    if (propertyValue.ValueKind != JsonValueKind.String || !IsSafeMediaUrl(propertyValue.GetString()))
                        errors.Add($"{propertyPath} must be a local media path or HTTPS URL.");
                    break;
                case "link":
                    if (propertyValue.ValueKind != JsonValueKind.Object ||
                        !propertyValue.TryGetProperty("url", out var linkUrl) ||
                        linkUrl.ValueKind != JsonValueKind.String || !IsSafeLink(linkUrl.GetString()))
                        errors.Add($"{propertyPath} must be a link object with a safe url.");
                    break;
                case "reference":
                    if (propertyValue.ValueKind != JsonValueKind.String || !Guid.TryParse(propertyValue.GetString(), out _))
                        errors.Add($"{propertyPath} must be a GUID content reference.");
                    break;
                case "object":
                    if (propertyValue.ValueKind != JsonValueKind.Object)
                        errors.Add($"{propertyPath} must be an object.");
                    else
                        ValidateObject(propertyValue, property.Children, propertyPath, errors, depth + 1);
                    break;
                case "collection":
                    if (propertyValue.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"{propertyPath} must be an array.");
                    }
                    else
                    {
                        var index = 0;
                        foreach (var item in propertyValue.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.Object)
                                errors.Add($"{propertyPath}[{index}] must be an object.");
                            else
                                ValidateObject(item, property.Children, $"{propertyPath}[{index}]", errors, depth + 1);
                            index++;
                            if (index > 200)
                            {
                                errors.Add($"{propertyPath} cannot contain more than 200 items.");
                                break;
                            }
                        }
                    }
                    break;
            }
        }
    }

    private static bool IsVersion(string value)
        => Version.TryParse(value, out var version) && version.Build >= 0 && version.Revision == -1;

    [GeneratedRegex("^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex AliasRegex();

    [GeneratedRegex("^[a-zA-Z0-9 _-]{0,120}$", RegexOptions.CultureInvariant)]
    private static partial Regex StyleClassRegex();

    [GeneratedRegex("^#(?:[a-fA-F0-9]{3}|[a-fA-F0-9]{4}|[a-fA-F0-9]{6}|[a-fA-F0-9]{8})$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}

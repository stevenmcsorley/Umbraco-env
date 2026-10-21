namespace SiteKit.Core.Components;

/// <summary>
/// Code-first authoring API for versioned declarative components. The builder
/// produces the same validated schema consumed by Component Studio and the
/// safe SiteKit renderer; it never compiles or executes renderer source.
/// </summary>
public sealed class SiteKitComponentDefinitionBuilder
{
    private readonly string _alias;
    private readonly string _name;
    private readonly string _category;
    private string _description = string.Empty;
    private string _version = "1.0.0";
    private readonly List<string> _tags = [];
    private readonly List<string> _dependencies = [];
    private readonly List<SiteKitComponentProperty> _properties = [];
    private readonly List<SiteKitComponentVariant> _variants = [];
    private SiteKitLayoutNode _layout = new() { Type = "section", Children = [] };

    private SiteKitComponentDefinitionBuilder(string alias, string name, string category)
    {
        _alias = alias;
        _name = name;
        _category = category;
    }

    public static SiteKitComponentDefinitionBuilder Create(string alias, string name, string category)
        => new(alias, name, category);

    public SiteKitComponentDefinitionBuilder Description(string description)
    { _description = description; return this; }

    public SiteKitComponentDefinitionBuilder Version(string version)
    { _version = version; return this; }

    public SiteKitComponentDefinitionBuilder Tag(string tag)
    { _tags.Add(tag); return this; }

    public SiteKitComponentDefinitionBuilder DependsOn(string componentAlias)
    { _dependencies.Add(componentAlias); return this; }

    public SiteKitComponentDefinitionBuilder Property(
        string alias,
        string label,
        string type,
        bool required = false,
        string? helpText = null,
        int? maxLength = null)
    {
        _properties.Add(new SiteKitComponentProperty
        {
            Alias = alias,
            Label = label,
            Type = type,
            Required = required,
            HelpText = helpText,
            MaxLength = maxLength
        });
        return this;
    }

    public SiteKitComponentDefinitionBuilder Property(SiteKitComponentProperty property)
    { _properties.Add(property); return this; }

    public SiteKitComponentDefinitionBuilder Layout(SiteKitLayoutNode layout)
    { _layout = layout; return this; }

    public SiteKitComponentDefinitionBuilder Variant(string alias, string label, string? styleClass = null)
    {
        _variants.Add(new SiteKitComponentVariant { Alias = alias, Label = label, StyleClass = styleClass });
        return this;
    }

    public SiteKitComponentDefinition Build()
    {
        var definition = new SiteKitComponentDefinition
        {
            Alias = _alias,
            Name = _name,
            Category = _category,
            Description = _description,
            Version = _version,
            Tags = _tags.ToArray(),
            Dependencies = _dependencies.ToArray(),
            Properties = _properties.ToArray(),
            Layout = _layout,
            Variants = _variants.ToArray()
        };
        var errors = SiteKitComponentValidator.ValidateDefinition(definition);
        if (errors.Count > 0) throw new ArgumentException("Invalid component definition: " + string.Join(" ", errors), nameof(definition));
        return definition;
    }
}

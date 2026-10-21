using System.Text.Json;

namespace SiteKit.Core.Components;

public sealed record SiteKitComponentDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public required string Alias { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string[] Tags { get; init; } = [];
    public string[] Dependencies { get; init; } = [];
    public required IReadOnlyList<SiteKitComponentProperty> Properties { get; init; }
    public required SiteKitLayoutNode Layout { get; init; }
    public IReadOnlyList<SiteKitComponentVariant> Variants { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record SiteKitComponentProperty
{
    public required string Alias { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? HelpText { get; init; }
    public string? DefaultValue { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public IReadOnlyList<SiteKitComponentProperty> Children { get; init; } = [];
}

public sealed record SiteKitLayoutNode
{
    public required string Type { get; init; }
    public string? Bind { get; init; }
    public string? Text { get; init; }
    public string? StyleClass { get; init; }
    public string? ItemAlias { get; init; }
    public int? Columns { get; init; }
    public int? HeadingLevel { get; init; }
    public IReadOnlyList<SiteKitLayoutNode> Children { get; init; } = [];
}

public sealed record SiteKitComponentVariant
{
    public required string Alias { get; init; }
    public required string Label { get; init; }
    public string? StyleClass { get; init; }
}

public sealed record SiteKitComponentPlacement
{
    public required string Id { get; init; }
    public required string ComponentAlias { get; init; }
    public string? Version { get; init; }
    public string? Variant { get; init; }
    /// <summary>Project-local CSS class overrides; values are validated and HTML encoded by the renderer.</summary>
    public string? StyleClass { get; init; }
    public SiteKitComponentAppearance? Appearance { get; init; }
    public JsonElement Data { get; init; }
}

public sealed record SiteKitComponentAppearance
{
    public string? Color { get; init; }
    public string? BackgroundColor { get; init; }
    public string? BorderColor { get; init; }
    public string? BorderRadius { get; init; }
    public string? Padding { get; init; }
}

public sealed record SiteKitProjectManifest
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Theme { get; init; } = "default";
    public string[] Modules { get; init; } = [];
    public Dictionary<string, string> ModuleVersions { get; init; } = new(StringComparer.Ordinal);
    public string[] Components { get; init; } = [];
    public string[] Entities { get; init; } = [];
    public string[] Screens { get; init; } = [];
    public SiteKitProjectTheme ThemeSettings { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record SiteKitProjectTheme
{
    public string PrimaryColor { get; init; } = "#087f8c";
    public string SurfaceColor { get; init; } = "#ffffff";
    public string TextColor { get; init; } = "#17212b";
    public string FontFamily { get; init; } = "system";
    public string Radius { get; init; } = "12px";
}

public sealed record SiteKitModuleManifest
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string Description { get; init; } = string.Empty;
    public required string[] CompatibleSiteKitVersions { get; init; }
    public string[] Dependencies { get; init; } = [];
    public Dictionary<string, string> DependencyVersions { get; init; } = new(StringComparer.Ordinal);
    public string[] Capabilities { get; init; } = [];
    public string[] ProjectCategories { get; init; } = ["website", "application", "combined", "api", "content"];
    public Dictionary<string, string> PackageFiles { get; init; } = new(StringComparer.Ordinal);
    public string IntegritySha256 { get; init; } = string.Empty;
}

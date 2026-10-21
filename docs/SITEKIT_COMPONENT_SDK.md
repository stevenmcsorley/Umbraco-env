# SiteKit component authoring SDK

The .NET authoring SDK lets developers create validated, reusable component
definitions in C# and publish them to the shared SiteKit component registry.
The output is the same versioned declarative schema used by Component Studio;
the production renderer continues to use the safe, fixed primitive set.

```csharp
using SiteKit.Core.Components;

var component = SiteKitComponentDefinitionBuilder
    .Create("help-disclosure", "Help disclosure", "content")
    .Description("An accessible answer disclosure.")
    .Version("1.0.0")
    .Tag("help")
    .Property("question", "Question", "text", required: true)
    .Property("answer", "Answer", "text", required: true)
    .Variant("compact", "Compact", "compact")
    .Layout(new SiteKitLayoutNode
    {
        Type = "accordion",
        Bind = "question",
        Children = [new() { Type = "text", Bind = "answer" }]
    })
    .Build();

var registry = new JsonSiteKitComponentRegistry(componentDirectory);
await registry.SaveAsync(component);
var package = await registry.ExportAsync(component.Alias, component.Version);
await File.WriteAllBytesAsync("help-disclosure-1.0.0.sitekit-component.json", package);
```

`Build()` runs the same schema and layout validation as the Studio API. A
registry export can be imported into another SiteKit installation with
`ISiteKitComponentRegistry.ImportAsync`; placements in projects keep referencing
the shared definition rather than copying its source. The Release contract test
builds a definition, exports and imports it into a separate registry, and renders
the imported version with project data.

Tags and dependency aliases are validated for shape and duplicates. The shared
registry checks that dependencies exist, rejects cycles and prevents deleting
the final version of a required component. Dependencies currently refer to the
component alias's latest version; version ranges and transactional installation
of dependency sets are not implemented.

The SDK currently authors declarative C# definitions. It does not load custom
Razor assemblies or execute React/TypeScript source. Those compiled renderer,
style-asset, client behavior and backend-service package contracts remain
unfinished. Custom source must not run in an untrusted production request.

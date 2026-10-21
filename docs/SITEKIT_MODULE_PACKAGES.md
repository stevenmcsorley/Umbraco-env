# SiteKit declarative modules

A module ZIP is an installable, versioned package that shares reusable component definitions, project-scoped entity schemas, screen/workflow JSON and documentation. Imported files are integrity checked and never executed as source code.

## Source directory

Create a source folder containing `sitekit.module.json` and one or more files beneath the supported folders:

- `components/*.json` — validated SiteKit component definitions.
- `entities/*.json` — validated entity schemas. Use `projectId: "module-package"`; installation binds them to the destination project.
- `screens/*.json` and `workflows/*.json` — inert JSON definitions retained with the installed module for compatible consumers.
- `docs/*.md` — package documentation.

Executable assemblies, C#, JavaScript, CSS, HTML, SVG, symlinks and arbitrary files are rejected. A package is limited to 256 files and 10 MiB compressed/decompressed, with each file no larger than 1 MiB.

The metadata manifest declares `schemaVersion`, a lowercase module `id`, semantic `version`, `name`, `description`, `compatibleSiteKitVersions`, `dependencies`, `capabilities`, and `projectCategories`. Leave `packageFiles` empty and `integritySha256` blank; the packer fills these hashes.

Example metadata:

```json
{
  "schemaVersion": 1,
  "id": "crm-starter",
  "name": "CRM Starter",
  "version": "1.0.0",
  "description": "Customer and opportunity schemas",
  "compatibleSiteKitVersions": ["1.x"],
  "dependencies": [],
  "capabilities": ["crm", "entities", "components"],
  "projectCategories": ["application", "combined"],
  "packageFiles": {},
  "integritySha256": ""
}
```

Build and import from the repository root:

```powershell
dotnet run --project SiteKit.AgentHost -- pack-module ./my-crm-module ./my-crm-module.sitekit-module.zip
dotnet run --project SiteKit.AgentHost -- import-module ./my-crm-module.sitekit-module.zip
```

Alternatively, upload the ZIP in `/studio/modules`. The UI lists verified packages, exports them, and installs them into a selected project. MCP agents can install a verified local module with `sitekit.install_module`.

The project manifest records installed module IDs plus the component/entity references. Component definitions are shared by reference; entity schemas are copied into the target project's schema registry. Existing component versions cannot be silently overwritten; entity schemas use SiteKit's additive version rules. Installation checks module/file hashes again before applying content. Dependencies must already be installed in the project and each module declares allowed project categories.

This package format is declarative. Screen files are validated and installed as project-bound SiteKit screen definitions, then can be previewed against operational records in Screen Studio. Generated projects currently receive the screen definitions as metadata; a generated screen runtime remains outstanding. Workflow files remain inert JSON. The Umbraco host installs the official Automate package; SiteKit-specific record event providers remain outstanding. See [SITEKIT_AUTOMATION.md](SITEKIT_AUTOMATION.md) and [SITEKIT_IMPLEMENTATION_PLAN.md](SITEKIT_IMPLEMENTATION_PLAN.md).

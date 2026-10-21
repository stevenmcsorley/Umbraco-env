# SiteKit implementation plan

This plan turns the existing Umbraco hotel and booking project into a reusable
website and business application platform while retaining the hotel system as
an installable starter module. Work proceeds incrementally against the existing
solution; each milestone records implementation and validation evidence here.

## Milestones

| Milestone | Scope | Status |
|---|---|---|
| M0 — Security and baseline | Audit existing code, remove committed secrets/build artifacts, secure administrative operations, establish reproducible build | Build verified; runtime smoke checks pending |
| M1 — Universal foundation | Project manifest, platform service boundaries, component registry and module package contracts | Partial: foundations and project/data contracts implemented; code SDK and full module lifecycle incomplete |
| M2 — Dynamic Component Studio | Schema based components, visual composition, preview, editing, import/export and developer SDK | Partial: runtime-authored schemas, multi-property visual editor, composed/reorderable layout builder, preview and safe renderer implemented; drag/drop canvas and compiled developer SDK incomplete |
| M3 — ChatGPT integration | Official SIWC flow and user controlled local Agent Host, typed tools and auditable orchestration | Implemented, live account verification pending |
| M4 — Project generation and theming | Independent website scaffolding, theme and layout configuration | Partial: independent .NET 10 generator and theme tokens; shared component projection and full production hosting configuration remain |
| M5 — Application Studio | Operational entities, versioned migrations, permissions, screens and persistent CRUD | Partial: visual typed-field/entity editor, explicit grants, SQL Server CRUD APIs and Studio record editor; generated apps now include model-driven screens and persistent token-protected CRUD, while generated storage is local JSON with one admin token; database-enforced relationships, RBAC and live SQL verification remain |
| M6 — Workflows and modules | Integrate supported Umbraco automation and installable modules | Partial: verified declarative module builder/import/export/install/explicit upgrade with project version pins and downgrade protection; official Automate 17.5.1 plus scoped record-change trigger and permission-checked update action compile; uninstall, version-ranged dependencies, executable workflow provisioning and live runtime verification remain |
| M7 — AI application generation | Build and modify persistent applications through guarded SiteKit tools | Partial: local ChatGPT orchestrator can build/list project, entity, screen, component and module state; screen definitions and metadata export work; deployable screen runtime, full CRM journey and live eligible ChatGPT verification remain |
| M8 — Production readiness | Deployable references and security, accessibility, reliability and performance evidence | Partial: Release build workflow, NuGet vulnerability gate, 12 focused core tests and screenshot set; accessibility, performance, restore/backup and live deployment verification remain |

## Existing feature ledger

| Capability | Status | Evidence / notes |
|---|---|---|
| Umbraco CMS and backoffice | Implemented | Migrated project in `MyDockerProject/MyDockerProject` targets Umbraco 17 LTS |
| Razor layouts and reusable partials | Implemented | Layouts, Block Grid/List templates and partial views under `Views` |
| Hotel pages, offers and events | Implemented | Hotel MVC routes, document type services and exported templates |
| Booking and inventory | Implemented | EF Core `BookingDbContext`, services, API and TypeScript engine |
| Payments | Partial | Mock and Stripe implementations; production configuration and end-to-end verification pending |
| Customer login | Partial | Session based login with ASP.NET Identity password hashing and allowlisted administration; native backoffice identity integration remains separate work |
| Data importer | Partial | Hotel importer preserved; universal project/entity mappings now support CSV/JSON/XML preview, idempotent upsert and value-free execution history; queueing, media and live host verification remain |
| Content type generation | Implemented | Seed API creates document types and element types; administrative endpoint security is critical |
| React/TypeScript booking UI | Implemented | Separate frontend and generated bundle |
| Docker deployment | Partial | Compose includes SQL Server, Umbraco and engine; credentials and development configuration need hardening |
| Component registry / Studio | Partial | Versioned declarative JSON registry, schema/data validation, import/export, safe primitive renderer, authenticated `/studio/components` editor, project registry and project-local placements/appearance in `/studio/projects`; reusable definitions are placed by reference with validated per-project overrides. Published page integration and developer package pipeline remain planned |
| Application data models / screen builder | Partial | SiteKit typed entity schemas, safe additive version rules, SQL Server record CRUD, audit columns, explicit grants and a basic data model/record editor; database-enforced relationships, screen composition and live SQL verification remain |
| Workflows / reusable module packages | Partial | Verified versioned module packages can ship/install component, entity and screen definitions; official Umbraco Automate and a scoped SiteKit record-change trigger are integrated; record actions are missing and module workflow JSON remains inert |
| ChatGPT plan sharing / Agent Host / MCP | Partial | Local Agent Host implements SIWC, Responses streaming, typed tools and local stdio MCP; live eligible account flow remains unverified |
| Generated business application runtime | Partial | Project generator exports project and screen manifests and deployable shells; application screen routing, persistent CRUD APIs and authorization are not yet projected into each independently generated runtime |

## Architecture decisions

1. Preserve Umbraco for editorial content, media, publishing and its native
   Block Grid/Block List editing. Use application database entities for
   transactional/business records instead of forcing them into the content tree.
2. Treat the hotel and booking implementation as a module/starter. Keep its
   data and workflows operational as platform boundaries are introduced.
3. Represent simple runtime authored components using versioned, validated
   JSON schemas and a constrained renderer. Developer components use a build
   pipeline and are never executed as arbitrary source in a request.
4. Keep SIWC credentials and inference in a user controlled SiteKit Agent Host.
   Do not introduce shared subscription proxying or promise eligibility that
   OpenAI does not guarantee.
5. Prefer official Umbraco extension and automation APIs after verifying their
   support and compatibility; isolate version specific adapters.

## M0 findings and constraints

- The repository started on Umbraco 16 / .NET 9. It now targets Umbraco 17.7.1
  / .NET 10 and pins the .NET SDK with `global.json`; Docker uses .NET 10 images.
- `MyDockerProject/.env`, two Umbraco export ZIPs, a database backup and 1,635
  generated dependency files were tracked. They are excluded from the
  current tree; historical objects remain in Git. The repository owner has
  confirmed the committed database password is a test credential.
- Admin, importer, migration, seed, Studio and SiteKit APIs require a signed-in account
  explicitly allowlisted in `SiteKit:AdminEmails`. Customer record endpoints require the matching session identity. Allowlisted addresses cannot be publicly claimed without the separate bootstrap code; remove that code after provisioning the first admin account. Passwords use ASP.NET Identity's
  salted password hasher and legacy SHA-256 hashes are upgraded on successful
  login. HTML login, registration and booking cancellation forms use antiforgery
  tokens. Forwarded headers accept only configured proxy IPs.
- The ImageSharp 4.1.2 security override removes a newly disclosed vulnerable
  transitive version. Updated to 4.1.3 to include current security fixes.
  ImageSharp 4 requires a Six Labors community/commercial key for direct
  dependencies. The open-source project must apply for a community key; never
  commit it. Release builds and GitHub CI consume `SIXLABORS_LICENSE_KEY` from
  the environment/repository secret. Debug builds remain available without it.
- No tests are present in the tracked solution. Validation will start with
  reproducible builds and focused smoke checks as features are implemented.
- The previously committed database password is a test credential according to
  the repository owner; its historical Git object remains in history. Runtime
  startup still requires database-backed smoke verification.

## M1 and M2 progress

- Added `SiteKit.Core` as a first-class solution project, with component,
  project and module manifest contracts. The JSON registries persist versioned
  component definitions and project manifests outside the Umbraco content
  tree; component imports are limited to 1 MiB and use the same validator as
  saves. Administrator endpoints manage project manifests independently.
- Added component schema/property/layout validation and data validation,
  including URL, media and color checks. The renderer uses a fixed primitive
  allowlist, HTML-encodes text and attributes, and does not execute component
  source or JavaScript.
- Added administrator-authorized list/get/save/delete/import/export/data
  validation/preview API endpoints and the initial Component Studio route at
  `/studio/components`. The editor has quick fields, a repeatable visual
  property builder and a composed/reorderable layout builder, plus advanced
  JSON schema editors for nested properties, variants and detailed layouts.
  Existing nested properties and layout metadata are preserved when visual
  controls are edited. Project
  manifest CRUD is exposed at `/api/sitekit/projects`. Project placements and project-local appearance overrides are implemented; a drag/drop canvas and developer package build pipeline remain.
- Build verification after these changes: `dotnet build UmbraccoEnv.sln
  --no-restore` succeeds with 0 errors and 154 warnings. No runtime UI or
  database smoke check has been performed yet. Static Playwright previews use fixture data; see `SITEKIT_SCREENSHOTS.md`.

## M3 progress

- Added the standalone `SiteKit.AgentHost` executable. It requests fresh OAuth
  state, nonce and PKCE values, uses the official dynamic registration and
  loopback callback, validates ID-token signature/issuer/audience/expiry/nonce,
  stores credentials per user under local OS protection, refreshes and revokes
  tokens, and checks the separately granted plan-usage scopes.
- Added per-account model discovery and a streaming Responses client using
  `store:false`, `stream:true`, full local input history, limited local function
  tools and explicit usage-limit handling. Chat history remains user controlled.
- Added the stdio MCP server and audited tool handlers for component/project
  CRUD, placement, validation, inspection and the fixed solution build. An MCP
  protocol smoke invocation exercised initialize, tools/list and
  `sitekit.list_components` successfully.
- `dotnet build UmbraccoEnv.sln --no-restore` succeeds. Live OAuth, refresh and
  Responses inference remain unverified because no ChatGPT account is connected
  in this environment. Docker Desktop's Linux engine and SQL Server are also
  unavailable, preventing live Umbraco Studio verification. Static fixture screenshots are documented in `SITEKIT_SCREENSHOTS.md`.

## M4 and M5 progress

- SiteKit projects now include validated theme tokens and a generator that writes
  an independent .NET 10 website, internal application, combined or API project
  with a project manifest, runnable ASP.NET Core host, Dockerfile and build/run
  documentation. The Studio and local AI/MCP tools can invoke generation.
- Added project-scoped typed entity definitions with explicit per-principal
  operation grants and schema version checks. Existing fields, types, constraints
  and relationships cannot be removed or changed; schema evolution currently
  permits optional additive fields one version at a time.
- Added a SiteKit entity editor and record screen to Projects Studio. Authenticated
  endpoints enforce explicit read/create/update/delete grants for every record
  operation. Allowlisted SiteKit admins grant access to account emails. The
  visual data model editor now creates and edits entity name, alias, schema
  version and typed fields with required/unique, max length and relationship
  target settings; advanced JSON remains available for fields not represented
  in the visual editor.
- SQL Server deployments persist operational records in a separate
  `SiteKitOperationalRecord` table with audit columns and soft deletion. The
  table is created idempotently by the SQL provider on first record access; the
  local JSON record store remains a development fallback. Schema definitions
  and permissions remain versioned local JSON.
- Validation: a project and `Customer` entity were created and listed through
  the local MCP protocol. The generated website built with .NET 10 and served a
  healthy `/health` response and HTTP 200 page. SQL CRUD, grant enforcement and
  schema behavior still require a running Umbraco/SQL environment.
- Additional validation: the JSON record-store implementation completed
  create/read/update/delete against disk and retained the original creator and
  creation timestamp across an update. This validates the local fallback only;
  it does not replace SQL Server or authenticated API verification.

## Validation record

| Date | Milestone | Validation |
|---|---|---|
| 2026-10-21 | M0 | `dotnet build UmbraccoEnv.sln` succeeded on .NET SDK 10.0.402; `dotnet list package --vulnerable --include-transitive` reported no vulnerable packages. 154 compiler warnings remain, primarily legacy Umbraco API usage and nullability. Runtime/DB smoke checks remain outstanding. |
| 2026-10-21 | M1/M2 | `dotnet build UmbraccoEnv.sln --no-restore` succeeds with 0 errors. An additional build with `RazorCompileOnBuild=true` exposes pre-existing Razor compile failures in the backup view folder and the generated `PublishedModels.Home` type; ordinary project builds intentionally disable build-time Razor compilation. Runtime UI smoke checks remain outstanding. |
| 2026-10-21 | M8 | `dotnet test SiteKit.Core.Tests/SiteKit.Core.Tests.csproj --no-restore --configuration Release` passed 3 tests covering encoded component rendering, screen/entity field validation and JSON record-store CRUD/audit retention. |


## Screenshot evidence

Studio visual previews and their validation limits are documented in [SITEKIT_SCREENSHOTS.md](SITEKIT_SCREENSHOTS.md). Module format and CLI documentation is in [SITEKIT_MODULE_PACKAGES.md](SITEKIT_MODULE_PACKAGES.md).







## M6 progress

- Added a bounded, SHA-256 checked `.sitekit-module.zip` format, pack/import/
  export CLI commands, authenticated module API and Studio module installer.
  Packages are declarative and reject executable code, web assets, path
  traversal, symlinks and oversized files.
- Module installation verifies hashes again, checks project-category and
  dependency compatibility, installs shared component references and
  project-scoped entity schemas, and updates the project manifest.
- Validation: built/imported a `CRM Starter` package with one component and one
  entity, then installed it into two projects through the local MCP tool. Both
  project manifests include the module and entity/component references.
- Installed official `Umbraco.Automate` 17.5.1 in the Umbraco host, preserving
  its native automation editor, persistence and built-in triggers/actions.
  Pinned transitive `OpenTelemetry.Api` to patched 1.15.3 after the package
  restore revealed vulnerable 1.1.0. The Umbraco project vulnerability scan is
  clean. See [SITEKIT_AUTOMATION.md](SITEKIT_AUTOMATION.md).
- A native record-changed trigger publishes project/entity/record IDs and
  operation without business field data, with per-user permission checks at the
  entity API and idempotency keys. Added a record-update action that resolves
  the Automate workspace service account, checks its SiteKit entity grant,
  validates and merges its JSON patch and emits the change event. The action
  output contains identifiers and changed-field count only. Automate discovery
  and run history still require live Umbraco/database verification; create and
  delete actions are not yet registered, and module workflow JSON remains inert.

## M7 progress

- Added a project-scoped application screen registry with validated definitions
  for tables, forms, details, dashboards, kanban boards, calendars, search and
  approval screens. Screens bind to declared entity fields and reusable
  component definitions; project manifests track screen aliases.
- Added the authenticated Screen Studio at `/studio/screens`, project/entity
  selection, screen editing, and previews backed by the operational record
  store. Table, form and kanban previews include a live record list/editor for
  create, edit and delete operations. Screen previews enforce the same
  per-principal `read` grant as record APIs, and mutations reuse the existing
  operation-specific grants. Screen metadata is included when generating an
  independent project.
- Added local agent/MCP `sitekit.create_screen` and `sitekit.list_screens` tools.
  Module packages now validate and install screen definitions with component
  and entity references. MCP and authenticated Studio APIs validate a
  project's complete screen/entity/component references. Project generation
  exports both entity and screen schema metadata.
- Validation: .NET 10 solution build succeeds (0 errors). MCP protocol smoke
  created an application project and Opportunity entity, created/listed a
  kanban screen, and returned `valid: true` from project validation. A static
  Playwright screenshot documents the Screen Studio layout. A second MCP smoke
  packed/imported a Contact Directory screen module and installed it into the
  application project with its entity; both screen definitions were listed
  afterwards. Project generation exported the saved screens to
  `sitekit.screens.json` and entity definitions to `sitekit.entities.json`.
- Limitations: generated applications currently export screen definitions as
  metadata and the Studio preview reads records, but deployable generated
  applications do not yet include screen rendering/runtime CRUD. CRM end-to-end
  generation and record-preserving AI modification remain incomplete. Screen
  Studio record editor controls call the permission-checked CRUD APIs, but the
  new controls have not been exercised against a live Umbraco/SQL installation.

## Universal import progress

- Preserved the existing hotel/content importer and added independent
  project-scoped import mapping contracts and JSON-backed mapping storage.
- Added CSV, JSON-array and XML-row parsing with UTF-8 validation, XML DTD and
  external entity protections, 10 MiB and 5,000-row bounds, typed field
  conversion, validation previews and upserts by declared unique fields.
  Existing entity fields omitted from an import row are preserved on update.
- Added authenticated import mapping APIs and `/studio/imports`. Preview
  requires entity read/manage access; execution requires import/manage and
  records the acting account. Mapping definitions are persisted for repeatable
  runs. Bounded history records run metadata and file hash without row values;
  Studio displays the latest 100 previews/imports. Added import navigation
  across Studio areas.
- Validation: all 11 Release core tests pass, including CSV/JSON/XML import and
  preview, unique-key repeat updates, retention of omitted data and invalid-row
  reporting. The Umbraco Debug host compiles with 0 errors (154 existing
  warnings). Added a static fixture screenshot; live authenticated endpoint
  and SQL-backed import verification remain outstanding.
- Current boundaries: no media transfer, background/resumable jobs, AI mapping,
  execution audit log or relationship-aware deduplication. See
  [SITEKIT_IMPORTS.md](SITEKIT_IMPORTS.md).

## M6 module versioning update

- Project manifests now pin the exact installed module version. A package with
  the same module ID cannot silently replace an installed version; Studio and
  the local Agent Host expose an explicit upgrade action. Upgrades move forward
  only, and downgrades are rejected before package assets are written.
- Validation: all 11 Release core contract tests pass, including first install,
  blocked implicit upgrade, explicit forward upgrade and blocked downgrade.
  The .NET 10 Umbraco solution builds with 0 errors (1 existing warning), and
  the module Studio JavaScript passes `node --check`.
- Module dependency validation currently checks installed module IDs; version
  ranges and transactional rollback across multiple registry files remain
  planned. Static Studio fixture evidence is in `SITEKIT_SCREENSHOTS.md`.

## M4/M5 generated application runtime update

- Generated application and combined projects now include a screen runtime for
  their exported screen definitions and JSON-backed persistent entity CRUD
  endpoints, including create, read, update and delete. Input rejects unknown
  fields and enforces required/type/length/unique constraints. API calls require
  a bearer token configured through `SITEKIT_ADMIN_TOKEN`; no token is embedded
  in generated source. The starter README explains deployment secrets and
  replacing the single-token/local-file defaults before production use.
- Validation: the generator contract test verifies project manifests, screen
  runtime assets and protected CRUD source, and invokes `dotnet build` on the
  emitted application. It starts the generated project and verifies unauthenticated
  denial, create/update/read/delete behavior, and record retention after an app
  restart. All 12 Release core tests pass; a live browser capture is documented
  in `SITEKIT_SCREENSHOTS.md`.
- Boundaries: generated app CRUD currently uses local JSON files and a single
  administrator token, with no record-level role grants, database transaction
  support or multi-process write coordination. The shared SiteKit host retains
  its separate SQL Server path and permission grants. These generated defaults
  are a deployable reference, not production-grade multi-user persistence.

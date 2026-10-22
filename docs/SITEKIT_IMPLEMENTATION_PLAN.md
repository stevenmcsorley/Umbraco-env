# SiteKit implementation plan

This plan turns the existing Umbraco hotel and booking project into a reusable
website and business application platform while retaining the hotel system as
an installable starter module. Work proceeds incrementally against the existing
solution; each milestone records implementation and validation evidence here.

## Milestones

| Milestone | Scope | Status |
|---|---|---|
| M0 — Security and baseline | Audit existing code, protect existing installations, secure administrative operations, establish reproducible build | Docker build verified; SQL healthy; local HTTPS overlay and HTTP-to-HTTPS redirect verified; SiteKit Studio admin APIs require the allowlisted session; data-protection key persists across container recreation; full Umbraco backoffice sign-in and production hardening remain pending |
| M1 — Universal foundation | Project manifest, platform service boundaries, component registry and module package contracts | Partial: foundations and project/data contracts implemented; code-first C# component authoring is available; compiled renderer SDK and full module lifecycle incomplete |
| M2 — Dynamic Component Studio | Schema based components, visual composition, preview, editing, import/export and developer SDK | Partial: runtime-authored schemas, C# definition builder, multi-property visual editor, composed/reorderable layout builder, accessible disclosure interactions, preview, safe renderer, and validated cross-registry export/import implemented; drag/drop canvas and compiled Razor/React SDK incomplete |
| M3 — ChatGPT integration | Official SIWC flow and user controlled local Agent Host, typed tools and auditable orchestration | Implemented, live account verification pending |
| M4 — Project generation and theming | Independent website scaffolding, theme and layout configuration | Partial: independent .NET 10 generator now projects shared validated component placements with per-project data and appearance; broader module projection and full production hosting configuration remain |
| M5 — Application Studio | Operational entities, versioned migrations, permissions, screens and persistent CRUD | Partial: visual typed-field/entity editor, explicit account and project-role grants, SQL Server CRUD APIs and Studio record editor; SQL persistence now verifies and locks relationship targets during writes and prevents deleting referenced records inside serializable transactions; generated apps include model-driven screens, accessible grouped count/sum charts, SQLite-backed CRUD, direct and role-based grants and relationship protection; identity federation, schema migrations for SQL entities and live multi-entity SQL verification remain |
| M6 — Workflows and modules | Integrate supported Umbraco automation and installable modules | Partial: verified declarative module builder/import/export/install/explicit upgrade, dependency-safe project removal preserving schemas and records, and registry-version deletion guarded by project pins; SemVer dependency ranges and dependent-upgrade checks; official Automate 17.5.1 plus scoped record-change trigger and permission-checked create/update/idempotent delete actions compile; executable workflow provisioning and live runtime verification remain |
| M7 — AI application generation | Build and modify persistent applications through guarded SiteKit tools | Partial: local ChatGPT orchestrator can build/list project, entity, screen, component and module state; generated apps now render tables, search, kanban, calendars, entity-count dashboards and grouped count/sum charts over SQLite CRUD; complete CRM journey and live eligible ChatGPT verification remain |
| M8 — Production readiness | Deployable references and security, accessibility, reliability and performance evidence | Partial: license-aware CI build, NuGet vulnerability gate and 29 focused core tests; accessibility, performance, restore/backup and live deployment verification remain |

## Existing feature ledger

| Capability | Status | Evidence / notes |
|---|---|---|
| Umbraco CMS and backoffice | Implemented | Migrated project in `MyDockerProject/MyDockerProject` targets Umbraco 17 LTS |
| Razor layouts and reusable partials | Implemented | Layouts, Block Grid/List templates and partial views under `Views` |
| Hotel pages, offers and events | Implemented | Hotel MVC routes, document type services and exported templates |
| Booking and inventory | Implemented | EF Core `BookingDbContext`, services, API and TypeScript engine |
| Payments | Partial | Mock and Stripe implementations; production configuration and end-to-end verification pending |
| Customer login | Partial | Session based login with ASP.NET Identity password hashing and allowlisted administration; native backoffice identity integration remains separate work |
| Data importer | Partial | Hotel importer preserved; universal project/entity mappings support CSV/JSON/XML files and allowlisted HTTPS JSON REST sources, preview, idempotent upsert and value-free execution history; queueing, media and live host verification remain |
| Content type generation | Implemented | Seed API creates document types and element types; administrative endpoint security is critical |
| React/TypeScript booking UI | Implemented | Separate frontend, Vite 8 / React Router 7, Tailwind 4, generated bundle; npm audit reports no known vulnerabilities |
| Docker deployment | Partial | Compose SQL Server, Umbraco and booking engine started and smoke checked. SQL health is green; HTTPS Umbraco backoffice and booking `/health` return HTTP 200. ASP.NET Core data-protection keys persist in the existing `umb_data` volume across app-container recreation. Automate completes its nine startup migrations using the shared CMS SQL connection. The optional HTTPS overlay has a locally trusted development certificate. Production TLS and data-protection key encryption remain required. |
| Component registry / Studio | Partial | Versioned declarative JSON registry, schema/data validation, import/export, safe primitive renderer, authenticated `/studio/components` editor with searchable persistent tags, project registry and project-local placements/appearance in `/studio/projects`; reusable definitions are projected by reference into generated websites with validated per-project overrides. Umbraco-published page integration and developer package pipeline remain planned |
| Application data models / screen builder | Partial | SiteKit typed entity schemas, safe additive version rules, SQL Server record CRUD, audit columns, direct and project-role grants and a basic data model/record editor; serializable SQL record writes and deletes enforce relationship existence/reference checks; screen composition, SQL entity schema migrations and live multi-entity verification remain |
| Workflows / reusable module packages | Partial | Verified versioned module packages can ship/install component, entity and screen definitions, upgrade with dependency checks, detach from projects without deleting data, and be deleted from the registry when no project pins the version; official Umbraco Automate provides a scoped SiteKit record-change trigger plus create/update/delete record actions with service-account grants; module workflow JSON remains inert |
| ChatGPT plan sharing / Agent Host / MCP | Partial | Local Agent Host implements SIWC, Responses streaming, typed tools and local stdio MCP; live eligible account flow remains unverified |
| Generated business application runtime | Partial | Project generator exports project and screen manifests with model-driven tables, forms, search, kanban, calendar, dashboard counts, accessible grouped count/sum SVG charts with tabular values, SQLite CRUD, token principals, project-role membership, unioned direct/role operation grants, relationship validation and delete protection; production identity federation and live external database integration remain |

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
- Umbraco resolves ImageSharp transitively. Pin to ImageSharp 3.2.0, the vendor's
  security-fixed v3 release. NuGet still reports five advisories for 3.2.0;
  Six Labors' [security support and license notice](https://sixlabors.com/posts/security-support-and-license-enforcement/)
  says the corresponding fixes were backported and advisory corrections are
  awaiting review. CI therefore exempts only those five advisory IDs for that
  exact package version. ImageSharp 3.2.0 requires a Six Labors key for
  Release builds. CI builds Release when `SIXLABORS_LICENSE_KEY` is configured
  and otherwise validates Debug, which does not require the key.
- The SiteKit core contract and storage test suite currently passes 29 tests.
  Broader application journey tests remain planned below.
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
  source or JavaScript. A native `<details>/<summary>` disclosure primitive
  supports keyboard-accessible FAQ/accordion interactions without custom
  source execution. Its summary binds only to validated text properties.
- Component Studio persists editable comma-separated tags as part of each
  versioned definition; the tags remain searchable after reload.
- Added administrator-authorized list/get/save/delete/import/export/data
  validation/preview API endpoints and the initial Component Studio route at
  `/studio/components`. The editor has quick fields, a repeatable visual
  property builder and a composed/reorderable layout builder, plus advanced
  JSON schema editors for nested properties, variants and detailed layouts.
  Existing nested properties and layout metadata are preserved when visual
  controls are edited. Project
  manifest CRUD is exposed at `/api/sitekit/projects`. Project placements and project-local appearance overrides are implemented; a drag/drop canvas and developer package build pipeline remain.
- Build verification: the solution builds with 0 errors and the core contract
  suite passes 23 Release tests. An authenticated live Studio session authored
  `northstar-editorial-hero` through the visual editor, saved its typed schema,
  and rendered it from the shared registry in two separate projects with
  independent data and appearance. Live browser captures and provenance are in
  `SITEKIT_SCREENSHOTS.md`; this validates the JSON registries and renderer, not
  Umbraco content-tree placement.

### C# component authoring SDK

- Added `SiteKitComponentDefinitionBuilder`, a fluent .NET API for declaring
  typed properties, layout trees, tags, variants and metadata. `Build()` runs
  the same component schema/layout validator used by Component Studio. The
  output saves to the shared registry and uses the existing validated export
  and import contract for reuse across installations. The component registry
  now resolves dependency aliases, rejects missing dependencies/cycles, and
  blocks removal of the last version required by another component.
- Validation: the SDK contract test builds an FAQ definition, exports it from
  one registry, imports it into another, validates it and renders the imported
  version with project data. It also verifies missing dependency, dependency
  cycle and required-dependency removal rejection. The full Release suite
  passes 27 tests.
- Boundaries: this is a C# code-first authoring path for declarative schemas,
  not a compiled custom renderer. Razor assemblies, React/TypeScript bundles,
  backend service hooks and controlled compilation/deployment remain planned.
  Component dependencies target the latest alias version; version ranges and
  transactional installation of dependency sets are not implemented.

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
- Changed plan-usage consent to use only scopes returned by the token endpoint;
  callback scopes are no longer treated as proof of consent. Added a regression
  test for missing, duplicate and whitespace-delimited scopes. The .NET 10
  Release suite now passes 23 tests.
- `dotnet build UmbraccoEnv.sln --no-restore` succeeds. Live OAuth, refresh and
  Responses inference remain unverified because no ChatGPT account is connected
  in this environment. Docker and SQL were unavailable when this entry was
  recorded; they have since been started and basic service smoke checks now
  pass. Live authenticated SiteKit Studio captures are now documented; a
  successful Umbraco backoffice identity-provider sign-in remains unverified.

## M4 and M5 progress

- SiteKit projects now include validated theme tokens and a generator that writes
  an independent .NET 10 website, internal application, combined or API project
  with a project manifest, runnable ASP.NET Core host, Dockerfile and build/run
  documentation. The Studio and local AI/MCP tools can invoke generation.
- Website and combined project generation resolve shared component definitions
  and placements from the registries, then render them through SiteKit's safe
  primitive renderer. A contract test generates two independent sites from one
  component definition and confirms that each uses separate data and appearance.
  Both generated projects were run locally and captured in `SITEKIT_SCREENSHOTS.md`.
- Generated website and application templates now use responsive theme styling;
  project manifest screen order is preserved when emitting the application.
  The CRM capture shows the overview, customers table and opportunity pipeline.
- Added project-scoped typed entity definitions with explicit per-principal
  operation grants and schema version checks. Existing fields, types, constraints
  and relationships cannot be removed or changed; schema evolution currently
  permits optional additive fields one version at a time.
- Generated application CRUD now validates single and multiple relationship
  values against persisted target records and prevents deletion of referenced
  records. The integration test verifies missing-target rejection, valid link
  persistence across restart and delete protection. Enforcement currently runs
  in the generated API rather than as database foreign keys.
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
| 2026-10-21 | M0 | .NET 10 Docker build succeeded with 0 errors and 164 existing Umbraco/nullability warnings. SQL is healthy; the Umbraco login form, generated booking CSS/JS assets and booking `/health` return HTTP 200. Data-protection key persistence survived app-container recreation. `npm audit` reports zero vulnerabilities in both Node projects. The HTTPS overlay serves OpenIddict metadata with HTTP 200; HTTP requests redirect to HTTPS on the overlay. Full Umbraco backoffice sign-in remains unverified. |
| 2026-10-21 | M1/M2 | `dotnet build UmbraccoEnv.sln --no-restore` succeeds with 0 errors. An additional build with `RazorCompileOnBuild=true` exposes pre-existing Razor compile failures in the backup view folder and the generated `PublishedModels.Home` type; ordinary project builds intentionally disable build-time Razor compilation. Live authenticated Studio smoke created and retrieved a new versioned component, rendered its preview, created a second website project, and rendered the same component in both projects with distinct data and appearance. All five Studio routes return HTTP 200 after correcting Razor escaping in the responsive styles; Screens and Imports loaded with zero browser console errors. |
| 2026-10-21 | M8 | `dotnet test SiteKit.Core.Tests/SiteKit.Core.Tests.csproj --configuration Release` passed all 23 tests with 0 failures and 0 skips in the .NET 10 SDK container. |
| 2026-10-22 | M5/M8 | SQL Server record writes verify/lock relationship targets and deletes check references under serializable locks; the JSON store rejects references and the API maps conflicts to HTTP 409. Project-role permission resolution is covered by an evaluator test. CI passed through `da01cf4`; its 29 core tests, vulnerability check and solution build are green. The local project-role editor renders for the authenticated demo account without browser console errors. |
| 2026-10-22 | M6/M8 | Added Automate delete-record action with service-account `delete`/`manage` authorization, safe replays for absent records, relationship-safe store deletion and change-event publication. The Umbraco host compiles with the action and SQL transaction checks (0 errors, 159 existing warnings); the 29 core Release tests pass. The protected Automate action catalogue was probed unauthenticated and correctly returned 401; live discovery/execution still needs a backoffice identity. |
| 2026-10-21 | M5/M7 | Generator integration suite passed 23/23 tests after adding table/kanban edit controls and single/multiple relationship selectors. The suite builds generated projects and exercises token-protected persistent CRUD, relationships and delete protection; the browser screen workflow itself remains unautomated. |
| 2026-10-21 | M3/M7 | Release suite passed 24/24 tests, including first-run generation and build through the Agent Host tool after restore; generated screen editing and relationship control contracts are asserted. Live ChatGPT orchestration and browser-driven generated-screen editing remain unverified. |
| 2026-10-22 | M5 | Release suite passed 25/25 tests. A schema migration regression proves existing record data remains intact through an optional field addition and that a required-field addition is rejected without changing the stored schema. |
| 2026-10-22 | M2/M8 | Release suite passed 26/26 tests, including accessible accordion rendering and HTML-encoding assertions. Rebuilt the Docker Umbraco host with 0 errors and 164 existing warnings; OpenIddict discovery returned HTTP 200 over local HTTPS. |
| 2026-10-22 | M1/M2 | Release suite passed 27/27 tests, including C# code-first component definition, export/import across registries, validation/rendering, and component dependency checks for missing aliases, cycles and last-version removal. |
| 2026-10-22 | M5/M8 | Generated standalone applications now map token principals to project roles and union role grants with direct grants; integration coverage verifies role-based read succeeds while ungranted create is denied. The generated-app end-to-end test passes, and direct VSTest execution passes all 29 tests. |
| 2026-10-22 | M6 | Module Studio and the authenticated API now support dependency-checked project module removal. The module pin is removed while project schemas, references and records remain intact; missing/unpinned dependency metadata blocks removal. Lifecycle regression coverage verifies dependent removal order, preserved component/entity references and a saved record, version-pin cleanup and repeated-removal rejection. The focused test and all 29 Release core tests pass. |
| 2026-10-22 | M0/M6 | Rebuilt the local HTTPS Umbraco container from `125feb3`: .NET 10 Debug build succeeded with 0 errors and 164 existing warnings; React/TypeScript production assets built. HTTPS `/umbraco` and authenticated `/studio/modules` return HTTP 200, the module screen has no console errors, and a no-op removal request for an uninstalled module returned the expected HTTP 409. Booking `/health` returns HTTP 200, HTTP port 44372 redirects to HTTPS 44373, SQL remains healthy, and the existing Northstar `Alder & Finch` customer is still present after app-container recreation. A full live install/remove cycle remains unverified because no package is installed in the local registry. |
| 2026-10-22 | M6/M8 | Added deletion of an unused package version from the local registry, guarded in the authenticated API against any project pin. The module lifecycle test deletes an unpinned package and verifies registry lookup returns null; the focused test and all 29 Release core tests pass. Live HTTPS Studio round trip: imported a temporary 703-byte package (200), exported it as a ZIP (200), installed it in a disposable project, verified pinned package deletion is rejected (409), removed the project module pin, verified the pin list is empty, deleted the package version, then deleted the scratch project (204). Only the seeded `modern-atelier` and `northstar-advisory` projects and no module packages remain. The Northstar `Alder & Finch` customer remains readable after app-container recreation; booking health is 200 and SQL is healthy. Browser errors during this probe were the deliberate 409 pin guard and 404 lookup after scratch-project deletion. |


## Screenshot evidence

Studio visual previews and their validation limits are documented in [SITEKIT_SCREENSHOTS.md](SITEKIT_SCREENSHOTS.md). Module format and CLI documentation is in [SITEKIT_MODULE_PACKAGES.md](SITEKIT_MODULE_PACKAGES.md).

## M0 live Docker verification update — 2026-10-21

- Started the Compose SQL Server, Umbraco host and booking engine without
  deleting or recreating named data volumes. The stale Compose network
  reference was cleared by recreating containers only.
- SQL Server reported `healthy`; `http://localhost:44372/umbraco/login` served
  the real Umbraco login form and returned HTTP 200; the booking engine
  `/health` endpoint returned HTTP 200. The local compose port is HTTP only, so
  OpenIddict rejects the sign-in authorization request until HTTPS is set up.
- Captured `output/playwright/sitekit-umbraco-login-live.png` from the live
  container. It shows the genuine Umbraco login form, not an authenticated
  SiteKit Studio screen.
- Configured ASP.NET Core data-protection keys to persist to the existing
  `umb_data` volume and confirmed the same key file survives app-container
  recreation. Session values still use in-memory storage and do not survive an
  app restart. Added the Umbraco v17 development-mode backoffice package needed
  for `InMemoryAuto`, and configured Automate to reuse the CMS SQL connection.
  Automate's nine startup migrations completed.
- Corrected `SiteKitDataImportService` to scoped lifetime because its SQL record
  store dependency is scoped. A Docker build caught the invalid singleton
  dependency during startup validation.
- Added `.dockerignore` and copied the `SiteKit.Core` project file before
  restore. Build context fell from about 587 MB to about 40 KB on the final
  build. Docker Debug build completed with 0 errors and 154 existing warnings.
- The host does not have the pinned .NET 10 SDK installed. Validation used the
  .NET 10 Docker SDK image. Authenticated Studio routes remain unverified.
- Added an optional Compose HTTPS overlay using a user-supplied trusted PFX
  mounted read-only, plus local setup instructions. Certificate files are
  excluded from Git and the Docker build context. `docker compose ... config
  --quiet` passed with a placeholder environment value; the overlay was not
  started because no local certificate was provisioned or trusted. The default
  Docker stack remains up on HTTP and its SQL health check is green.
- Updated the frontend's vulnerable Vite and React Router dependencies, moved
  to Tailwind 4 with its Vite plugin, and corrected the ESM path configuration.
  `npm audit` reports zero vulnerabilities and `npm run build` emits an 18.57
  KB CSS bundle with Tailwind utilities. Updated the booking API runtime/dev
  dependencies, removed vulnerable `ts-node-dev`, and moved Node images to 22;
  both npm projects audit cleanly. The Docker build now packages the JavaScript
  and CSS assets without Compose bind mounts shadowing them. Docker smoke checks
  returned HTTP 200 for the served 18.57 KB stylesheet, 213 KB bundle, login
  form, and booking API health route.







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
  and run history still require live Umbraco/database verification; module
  workflow JSON remains inert. Added native record-create, record-update and
  record-delete actions with
  explicit service-account grants, typed-record and relationship validation,
  deterministic IDs derived from a required workflow idempotency key, and the
  shared record-change notification. Replays return the existing record
  without overwriting it or emitting another event. Delete requires an
  explicit service-account grant and replay after deletion succeeds without
  emitting a duplicate event.
- Validation 2026-10-21: `dotnet build
  MyDockerProject/MyDockerProject/MyDockerProject.csproj --configuration Debug`
  succeeded in the .NET 10 SDK container with 0 errors and 164 existing
  warnings. The warning set includes Umbraco obsolete/nullability warnings and
  ImageSharp advisories already covered by the narrowly-scoped CI advisory
  allowlist. Automate discovery and action execution remain unverified in the
  live backoffice.

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
  kanban screen, and returned `valid: true` from project validation. A second MCP smoke
  packed/imported a Contact Directory screen module and installed it into the
  application project with its entity; both screen definitions were listed
  afterwards. Project generation exported the saved screens to
  `sitekit.screens.json` and entity definitions to `sitekit.entities.json`.
- Limitations: generated app screens currently cover tables, search, kanban,
  calendars and entity-count dashboards with token/grant-protected SQLite CRUD.
  Table and kanban screens now edit existing rows through configured fields;
  generated forms provide single and multiple relationship selectors populated
  only from records the principal may read. API integration coverage verifies
  protected CRUD and relationship integrity; it does not yet automate the
  browser editing flow. Charts, richer detail/approval interactions, and the
  end-to-end AI-generated CRM journey remain incomplete. Record-preserving AI
  modification remains incomplete. Screen Studio record editor controls call
  the permission-checked CRUD APIs, but the controls have not been exercised
  against a live Umbraco/SQL installation.

### M7 Agent Host application build flow

- Expanded SiteKit Architect instructions with an explicit create/modify
  sequence: inspect projects/components/modules, reuse compatible assets, save
  entity models before screens, validate, generate, build, and report required
  deployment configuration. Existing-project changes should preserve aliases
  and the generated app's SQLite database.
- Fixed `sitekit_build_project` to perform dependency restore for a generated
  project and run from that project's directory so its pinned .NET SDK selection
  applies. The checked-in solution continues to use `--no-restore`. The tool
  still accepts only a project ID and fixed targets; it does not expose shell
  command or path arguments.
- Validation: a new Agent Host integration test created an API project through
  the project registry, generated it, then invoked the same build tool. The
  clean first build passed with restore; the Release suite passed all 25 tests
  after adding the schema migration regression.
- Limits: no connected eligible ChatGPT account was available to validate the
  provider's live multi-step orchestration. The generated app still requires
  operators to configure identity tokens/grants before use.

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
- Validation at that checkpoint: all 11 Release core tests passed, including CSV/JSON/XML import and
  preview, unique-key repeat updates, retention of omitted data and invalid-row
  reporting. The Umbraco Debug host compiles with 0 errors (154 existing
  warnings). Live authenticated endpoint and SQL-backed import verification
  remained outstanding at that checkpoint; screenshots in the current guide
  are captured from a live runtime and are identified by their individual scope.
- Current boundaries: no media transfer, background/resumable jobs, AI mapping,
  execution audit log or relationship-aware deduplication. See
  [SITEKIT_IMPORTS.md](SITEKIT_IMPORTS.md).

## M6 module versioning update

- Project manifests now pin the exact installed module version. A package with
  the same module ID cannot silently replace an installed version; Studio and
  the local Agent Host expose an explicit upgrade action. Upgrades move forward
  only, and downgrades are rejected before package assets are written.
- Validation: all 18 Release core contract tests pass, including first install,
  blocked implicit upgrade, explicit forward upgrade and blocked downgrade.
  The .NET 10 Umbraco solution builds with 0 errors (1 existing warning), and
  the module Studio JavaScript passes `node --check`.
- Module dependency validation currently checks installed module IDs; version
  ranges and transactional rollback across multiple registry files remain
  planned. Live Studio project/component evidence is in `SITEKIT_SCREENSHOTS.md`.

## M4/M5 generated application runtime update

- Generated application and combined projects now include a screen runtime for
  their exported screen definitions and SQLite-backed persistent entity CRUD
  endpoints, including create, read, update and delete. Input rejects unknown
  fields and enforces required/type/length/unique constraints. API calls require
  a bearer token configured through `SITEKIT_ADMIN_TOKEN`; no token is embedded
  in generated source. The starter README explains deployment secrets and the
  principal-token hashes and entity-grant configuration. Schema migration 1
  creates an entity-scoped records table, migration ledger and update-time
  index; writes use transactions.
- Validation: the generator contract test verifies project manifests, screen
  runtime assets and protected CRUD source, and invokes `dotnet build` on the
  emitted application. It starts the generated project and verifies unauthenticated
  denial, read-only write denial, create/update/read/delete behavior, and record
  retention while a screen changes and the project is regenerated/restarted. All
  27 Release core tests pass at the current checkpoint; live browser captures are documented
  in `SITEKIT_SCREENSHOTS.md`.
- Boundaries: generated app CRUD currently uses local SQLite and deployment
  configured administrator/principal bearer tokens with entity-level role
  grants, but no soft-delete. The
  shared SiteKit host retains
  its separate SQL Server path and permission grants. These generated defaults
  are a deployable reference, not production-grade multi-user persistence.

## Live Umbraco SQL record and HTTPS checkpoint (2026-10-21)

- The local HTTPS overlay serves the backoffice at
  `https://localhost:44373/umbraco`. The HTTP listener at port 44372 redirects
  to HTTPS. OpenIddict returns `ID2083` when an authorization request itself
  reaches it over HTTP; use the HTTPS URL directly for backoffice sign-in.
- In Studio, created the Northstar Advisory `Customer` record `Alder & Finch`
  and an explicit local principal grant for read/create/update/delete. Confirmed
  the record and grant were still available after restarting only the web
  container; SQL data volumes were retained.
- Live host CRUD probe returned 201 create, 200 update/read, 204 soft delete,
  then 404 when reading the deleted record. With the read operation removed
  from the grant, list returned 403; restoring the grant returned 200. SQL
  inspection showed one active demo row and one soft-deleted verification row.
- Screenshot: `output/playwright/sitekit-studio-customer-records-live.png`;
  provenance and boundaries are recorded in `SITEKIT_SCREENSHOTS.md`.
- Scope: this verifies one entity and a local test principal against the
  Umbraco host SQL store. It does not validate multi-entity relational
  migrations, production role administration, or a complete CRM journey.

## OAuth HTTPS error probe (2026-10-22)

- Reproduced the local route behavior against the running Docker host. Requests
  to the Umbraco member authorization endpoint and the likely backoffice OAuth
  routes over `http://localhost:44372` returned 307 redirects to the matching
  `https://localhost:44373` URL. The HTTPS member authorization endpoint
  reached OpenIddict and returned its expected 400 validation response for the
  deliberately incomplete probe (no OAuth parameters).
- The recent container logs contained no `ID2083` entry. This verifies the
  current local HTTPS listener and redirect behavior, but does not identify
  which browser URL or external proxy request produced the reported error. For
  local sign-in, start at `https://localhost:44373/umbraco`; do not bookmark or
  open the HTTP listener directly.

## M5/M7 generated application charts (2026-10-22)

- Added the `chart` screen type. Charts group records by a validated entity
  field and either count records or sum a validated numeric field. Studio
  previews and generated applications render the chart; generated output
  includes an accessible SVG title and a semantic data table.
- Validation: the generated-application integration test saves a CRM chart,
  rejects a nonnumeric value field, verifies the exported screen definition,
  checks chart markup and builds the independent application. The Studio view
  JavaScript passes `node --check`.

## M0 ImageSharp advisory review (2026-10-22)

- Umbraco 17.7.1 depends on ImageSharp; this project pins its resolved version
  to 3.2.0. The signed ImageSharp 3.2.0 release notes include PR #3208,
  "Backport v3 security fixes and license enforcement". Six Labors' security
  support post says five NuGet advisory records are awaiting correction and
  names the advisories.
- CI suppresses only those five exact advisory IDs, only for
  `SixLabors.ImageSharp` 3.2.0. New advisories, other packages, or another
  version still fail the vulnerability gate. Regression tests cover each
  boundary.
- This does not remove the package's build-time license requirement. CI uses a
  Release build only when `SIXLABORS_LICENSE_KEY` is configured; otherwise it
  verifies the open-source Debug build. Production Release publishing needs a
  valid key for the project's use case.
- Sources: [ImageSharp 3.2.0 release notes](https://github.com/SixLabors/ImageSharp/releases/tag/v3.2.0), [PR #3208](https://github.com/SixLabors/ImageSharp/pull/3208), and [Six Labors security support notice](https://sixlabors.com/posts/security-support-and-license-enforcement/).

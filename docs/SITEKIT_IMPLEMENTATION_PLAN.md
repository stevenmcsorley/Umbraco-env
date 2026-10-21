# SiteKit implementation plan

This plan turns the existing Umbraco hotel and booking project into a reusable
website and business application platform while retaining the hotel system as
an installable starter module. Work proceeds incrementally against the existing
solution; each milestone records implementation and validation evidence here.

## Milestones

| Milestone | Scope | Status |
|---|---|---|
| M0 — Security and baseline | Audit existing code, remove committed secrets/build artifacts, secure administrative operations, establish reproducible build | Build verified; runtime smoke checks pending |
| M1 — Universal foundation | Project manifest, platform service boundaries, component registry and module package contracts | In progress |
| M2 — Dynamic Component Studio | Schema based components, visual composition, preview, editing, import/export and developer SDK | In progress |
| M3 — ChatGPT integration | Official SIWC flow and user controlled local Agent Host, typed tools and auditable orchestration | Implemented, live account verification pending |
| M4 — Project generation and theming | Independent website scaffolding, theme and layout configuration | Planned |
| M5 — Application Studio | Operational entities, versioned migrations, permissions, screens and persistent CRUD | Planned |
| M6 — Workflows and modules | Integrate supported Umbraco automation and installable modules | Planned |
| M7 — AI application generation | Build and modify persistent applications through guarded SiteKit tools | Planned |
| M8 — Production readiness | Deployable references and security, accessibility, reliability and performance evidence | Planned |

## Existing feature ledger

| Capability | Status | Evidence / notes |
|---|---|---|
| Umbraco CMS and backoffice | Implemented | Migrated project in `MyDockerProject/MyDockerProject` targets Umbraco 17 LTS |
| Razor layouts and reusable partials | Implemented | Layouts, Block Grid/List templates and partial views under `Views` |
| Hotel pages, offers and events | Implemented | Hotel MVC routes, document type services and exported templates |
| Booking and inventory | Implemented | EF Core `BookingDbContext`, services, API and TypeScript engine |
| Payments | Partial | Mock and Stripe implementations; production configuration and end-to-end verification pending |
| Customer login | Partial | Session based login with ASP.NET Identity password hashing and allowlisted administration; native backoffice identity integration remains separate work |
| Data importer | Partial | JSON/file importer is hotel specific; universal mappings, retries and audit trail planned |
| Content type generation | Implemented | Seed API creates document types and element types; administrative endpoint security is critical |
| React/TypeScript booking UI | Implemented | Separate frontend and generated bundle |
| Docker deployment | Partial | Compose includes SQL Server, Umbraco and engine; credentials and development configuration need hardening |
| Component registry / Studio | Partial | Versioned declarative JSON registry, schema/data validation, import/export, safe primitive renderer, authenticated `/studio/components` editor, project registry and project-local placements/appearance in `/studio/projects`; reusable definitions are placed by reference with validated per-project overrides. Published page integration and developer package pipeline remain planned |
| Application data models / screen builder | Planned | Operational entities currently limited to bookings, inventory and users |
| Workflows / reusable module packages | Planned | No persistent general purpose workflow or package system found |
| ChatGPT plan sharing / Agent Host / MCP | Planned | No implementation found; must follow current official SIWC contract and eligibility limits |

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
  generated `node_modules` files were tracked. They are excluded from the
  current tree; historical objects remain in Git. The repository owner has
  confirmed the committed database password is a test credential.
- Admin, importer, migration and seed APIs now require a signed-in account
  explicitly allowlisted in `SiteKit:AdminEmails`. Customer record endpoints
  require the matching session identity. Passwords use ASP.NET Identity's
  salted password hasher and legacy SHA-256 hashes are upgraded on successful
  login. HTML login, registration and booking cancellation forms use antiforgery
  tokens. Forwarded headers accept only configured proxy IPs.
- The ImageSharp 4.1.2 security override removes a newly disclosed vulnerable
  transitive version. ImageSharp 4 requires a Six Labors community/commercial
  license setup for direct dependencies; this package/licensing impact needs
  review before distribution to end users.
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
  `/studio/components`. The editor has quick fields plus advanced JSON schema
  editors for nested properties, variants and composed layouts. Project
  manifest CRUD is exposed at `/api/sitekit/projects`. Project placements,
  richer visual composition and developer package build pipeline remain.
- Build verification after these changes: `dotnet build UmbraccoEnv.sln
  --no-restore` succeeds with 0 errors and 154 warnings. No runtime UI or
  database smoke check has been performed yet.

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
  unavailable, preventing live Umbraco Studio verification. Static fixture screenshots\nare documented in `SITEKIT_SCREENSHOTS.md`.

## Validation record

| Date | Milestone | Validation |
|---|---|---|
| 2026-10-21 | M0 | `dotnet build UmbraccoEnv.sln` succeeded on .NET SDK 10.0.402; `dotnet list package --vulnerable --include-transitive` reported no vulnerable packages. 154 compiler warnings remain, primarily legacy Umbraco API usage and nullability. Runtime/DB smoke checks remain outstanding. |
| 2026-10-21 | M1/M2 | `dotnet build UmbraccoEnv.sln --no-restore` succeeds with 0 errors. An additional build with `RazorCompileOnBuild=true` exposes pre-existing Razor compile failures in the backup view folder and the generated `PublishedModels.Home` type; ordinary project builds intentionally disable build-time Razor compilation. Runtime UI smoke checks remain outstanding. |


## Screenshot evidence

Studio visual previews and their validation limits are documented in [SITEKIT_SCREENSHOTS.md](SITEKIT_SCREENSHOTS.md).

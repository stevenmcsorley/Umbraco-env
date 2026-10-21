# Repository implementation guide

## Product direction

This repository is SiteKit on Umbraco. Preserve the existing hotel, booking,
inventory, import and payment experience while extracting reusable platform
capabilities. Hotel-specific code is an optional starter/module, not the model
for every project. Keep CMS editorial content in Umbraco and operational
business records in SiteKit's separate entity store.

## Project map

- `MyDockerProject/MyDockerProject`: Umbraco 17 / .NET 10 host, existing hotel
  application, authenticated APIs and SiteKit Studio views.
- `SiteKit.Core`: validated component, project, entity, screen, record and
  module contracts and JSON/SQL storage abstractions.
- `SiteKit.AgentHost`: user-controlled Sign in with ChatGPT host, local
  conversation/tool orchestration and stdio MCP server.
- `frontend`: existing React/TypeScript booking engine. Preserve its build and
  runtime integration.
- `docs/SITEKIT_IMPLEMENTATION_PLAN.md`: implementation ledger and verified
  acceptance evidence. Update it when behavior or validation changes.
- `docs/SITEKIT_SCREENSHOTS.md`: screenshot provenance and limitations.
- `docs/SITEKIT_AUTOMATION.md`: Umbraco Automate integration boundaries.

## Security and data boundaries

- Keep administration endpoints behind `SiteKitAdminAuthorizationFilter` and
  the configured administrator allowlist. Preserve the separate bootstrap code
  requirement for first-account registration.
- Operational record reads/writes require explicit per-principal entity grants.
  Screen previews and record editors must use the same checks as direct CRUD.
- Keep SIWC credentials and inference inside the local/user-controlled Agent
  Host. Never collect ChatGPT passwords/cookies, proxy a subscription through a
  shared service, or make an OpenAI API key mandatory.
- Never run arbitrary uploaded module code in a production request. Current
  module packages are bounded, hash checked and declarative. Workflow JSON is
  inert unless installed through a verified Automate integration.
- Treat imported data, component text, external pages and AI tool arguments as
  untrusted. Validate schemas, encode rendered HTML, constrain paths/URLs and
  audit actions. Production, destructive and external side effects need
  appropriate authorization.
- Do not commit `.env`, passwords, OAuth tokens, license files or API keys.
  Repository history may contain old test artifacts; do not reintroduce them.

## Umbraco integration

Prefer supported Umbraco 17 APIs and native backoffice tools. Preserve Block
Grid/Block List, media, publishing and content editing. `Umbraco.Automate` is
installed for native workflows; SiteKit operational events are exposed through
its scoped record-change trigger. Verify package behavior against the matching
Umbraco major version before adding adapters. See the implementation ledger for
unverified runtime boundaries.

## Build and validation

Use a .NET 10 SDK. From the repository root:

```powershell
dotnet restore UmbraccoEnv.sln
dotnet build UmbraccoEnv.sln --no-restore
dotnet list MyDockerProject/MyDockerProject/MyDockerProject.csproj package --vulnerable --include-transitive
```

Release builds directly reference ImageSharp 4.1.3 and require the free
Six Labors community key for this open-source project. Set
`SIXLABORS_LICENSE_KEY` locally or in GitHub repository secrets; never commit
it. Docker and SQL-backed runtime checks require Docker Desktop and a reachable
SQL Server. Do not report static Playwright fixtures as live Umbraco evidence.

Use the fixed SiteKit MCP tools for project/component/entity/screen/module
operations. `sitekit.validate_project` should pass before project generation.
Record the commands and actual results in the ledger; do not claim unexecuted
acceptance tests pass.

## Current implementation limits

Consult `docs/SITEKIT_IMPLEMENTATION_PLAN.md` before starting a milestone. At
the current checkpoint, project/entity/screen/module authoring works through
local MCP, the JSON record store has CRUD smoke evidence, generated sites are
independent but do not yet render application screens or CRUD, and live SIWC,
Umbraco/SQL, Automate delivery and generated CRM acceptance checks remain
unverified or incomplete.

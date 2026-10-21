# SiteKit

[![Build](https://github.com/stevenmcsorley/Umbraco-env/actions/workflows/sitekit-build.yml/badge.svg)](https://github.com/stevenmcsorley/Umbraco-env/actions/workflows/sitekit-build.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Umbraco 17 LTS](https://img.shields.io/badge/Umbraco-17%20LTS-3544B1?logo=umbraco&logoColor=white)
![Project status](https://img.shields.io/badge/status-active%20development-0B8F87)

### Build websites and business applications on Umbraco. Keep the building blocks.

SiteKit is being developed as an open-source platform for creating **content-driven websites, internal business systems, and reusable software modules**. Compose validated components, model operational data, build application screens, install modules, and generate independently deployable .NET projects.

The platform grows with what you make: a component, entity, screen, or module can become a building block for the next project. Umbraco remains the CMS for editorial content; operational records live in application data stores suited to transactional work.

<p align="center">
  <img src="output/playwright/sitekit-generated-crm-runtime.png" alt="Generated SiteKit CRM with customer records and an opportunity pipeline" width="920">
</p>

<p align="center"><strong>From reusable component to a working application—with persistent records and permission-checked operations.</strong></p>

> **Project status:** SiteKit is under active development. The repository contains working platform capabilities and generated runtime evidence, alongside incomplete milestones. See the [implementation ledger](docs/SITEKIT_IMPLEMENTATION_PLAN.md) for exact status and verified results. The hotel and booking application remains in the repository as a working starter experience.

## What you can build

| Websites | Business applications | Reusable platform |
| --- | --- | --- |
| Marketing and corporate sites | CRMs and sales pipelines | Components and themes |
| Blogs, directories, event sites | Portals and operations tools | Typed entities and screens |
| Hotel and booking experiences | Data-backed forms and tables | Installable modules and workflows |
| Generated independent .NET sites | Dashboards, kanban, calendar views | Imports, APIs, and project starters |

SiteKit is not limited to a shipped component catalogue. Component definitions are versioned, validated data schemas rendered from safe primitives. Projects place shared definitions by reference and supply their own content and appearance values. Developer code belongs in a controlled package/build pipeline; it is not executed as untrusted request content.

## See it working

### Create once, use across projects

The same registered component definition is rendered by two independent generated websites. Each project supplies its own placement data and appearance.

| Northstar Advisory | Lumen House |
| --- | --- |
| ![Northstar Advisory: shared component, independent content and cool blue appearance](output/playwright/sitekit-generated-shared-component-one.png) | ![Lumen House: same shared component, warm colors and serif theme](output/playwright/sitekit-generated-shared-component-two.png) |

### Build and work with an application

![Generated CRM with a customer table and persisted opportunity pipeline](output/playwright/sitekit-generated-crm-runtime.png)

The generated CRM runtime uses SQLite-backed CRUD, token-based principals, and direct or role-based entity-operation grants. The current integration evidence covers record creation, update, persistence across restart, permission denial, relationship checks, and guarded deletion. See [runtime scope and setup](docs/SITEKIT_GENERATED_RUNTIME.md).

### Create a component in Studio and reuse it

The visual Component Studio created a new, editable hero definition with title,
supporting copy, and link fields. The definition is shared; each project stores
its own content and appearance.

<p align="center">
  <img src="output/playwright/sitekit-component-studio-live.png" alt="Authenticated SiteKit Component Studio editing a reusable hero component" width="920">
</p>

<p align="center">
  <img src="output/playwright/sitekit-component-composer-live.png" alt="Visual component schema builder with typed fields and composed layout primitives" width="920">
</p>

The same `northstar-editorial-hero` version is placed in two separate SiteKit
projects. Northstar Advisory uses a deep teal treatment and strategy copy;
Modern Atelier uses warm sand tones and its own message. Both previews were
rendered by the running Umbraco host from the shared component registry.

| Northstar Advisory | Modern Atelier |
| --- | --- |
| ![Northstar Advisory with the shared hero component in teal](output/playwright/sitekit-studio-projects-live.png) | ![Modern Atelier with the same component in warm sand](output/playwright/sitekit-studio-projects-atelier.png) |

### Explore SiteKit Studio

SiteKit Studio runs in the Umbraco host at `/studio/projects`,
`/studio/components`, `/studio/screens`, `/studio/imports`, and `/studio/modules`.
The captures above come from an authenticated local Docker session, not a hosted
demo. Follow the [HTTPS setup](#run-the-existing-umbraco-starter) to run the
backoffice locally. See the [screenshot guide](docs/SITEKIT_SCREENSHOTS.md) for
capture details and scope.

## How the pieces fit

```mermaid
flowchart LR
    person[Builder] --> studio[Umbraco backoffice<br/>SiteKit Studio]
    person --> agent[Local SiteKit Agent Host]
    chatgpt[ChatGPT<br/>official sign-in flow] <--> agent
    studio <--> platform[SiteKit platform services]
    agent -->|typed, permission-checked tools| platform
    platform --> components[Component registry<br/>schemas · versions · placements]
    platform --> projects[Project and module registry]
    platform --> appdata[Application data<br/>entities · permissions · records]
    platform --> automate[Umbraco Automate<br/>triggers and actions]
    platform --> cms[Umbraco CMS<br/>content · media · publishing]
    projects --> generator[Project generator]
    components --> generator
    appdata --> generator
    generator --> site[Independent .NET website]
    generator --> application[Independent .NET application]
```

The Agent Host runs on the user's machine or controlled environment and keeps credentials there. Sign in with ChatGPT plan sharing is subject to OpenAI eligibility and the official integration terms; it is not a promise of universal subscription access. SiteKit does not ask for ChatGPT passwords or cookies. The optional provider interface can support other providers; an OpenAI API key is not the mandatory path for an eligible, authorised plan-sharing user.

## Current capabilities

- **Dynamic components:** versioned schemas, visual property/layout editing, accessible interactive disclosure blocks, safe rendering, previews, variants, JSON import/export, registry search, shared project placements, and per-project appearance.
- **Project generation:** independent .NET 10 website, application, combined, and API projects with Dockerfiles, manifests, and generated screen/runtime assets.
- **Business data:** typed entities, additive schema evolution checks, SQLite persistence in generated apps, SQL Server record APIs in the Umbraco host, direct and project-role permission grants, audit data, and validated relationships.
- **Application screens:** tables and forms plus search, kanban, calendar, and entity-count dashboard views in generated applications.
- **Modules:** verified declarative packages with integrity checks, component/entity/screen assets, dependency version ranges, project pins, and explicit upgrades.
- **Universal imports:** CSV, JSON, XML, and allowlisted HTTPS REST JSON sources; field mappings, validation, repeatable upserts, preview, and import history.
- **AI and MCP:** local Agent Host, typed SiteKit tools, stdio MCP, OAuth/PKCE Sign in with ChatGPT flow, and streamed Responses API orchestration. Live account eligibility and end-to-end subscription verification remain outstanding.
- **Existing starter:** Umbraco hotel content, booking, pricing/inventory, payments, importing, and React/TypeScript booking UI are preserved.

The [milestone ledger](docs/SITEKIT_IMPLEMENTATION_PLAN.md) distinguishes implemented, partial, planned, and runtime-verified work. Not every acceptance scenario in the long-term product direction is complete.

## Quick start

### Prerequisites

- .NET 10 SDK (Umbraco 17 LTS)
- Docker Desktop with its Linux engine, for the full Umbraco/SQL Server stack
- Node.js 22+, for building the booking engine outside Docker
- A Six Labors key for Release builds of the Umbraco web project (Debug builds need no key)

### Build the platform

```powershell
dotnet restore UmbraccoEnv.sln
dotnet build UmbraccoEnv.sln --no-restore
dotnet test SiteKit.Core.Tests/SiteKit.Core.Tests.csproj --configuration Release
```

GitHub Actions uses a Release build when `SIXLABORS_LICENSE_KEY` is configured;
otherwise it verifies the Debug build so contributors can validate the public
source without a private license credential.

### Run the existing Umbraco starter

```powershell
Set-Location MyDockerProject
docker compose up -d --build
```

The compose stack exposes Umbraco's HTTP endpoint at `http://localhost:44372` and the booking engine API at `http://localhost:3001`. For local backoffice sign-in, start the HTTPS overlay and open **`https://localhost:44373/umbraco`**. The HTTP endpoint redirects to HTTPS while the overlay is running. Docker builds the React booking JavaScript and Tailwind CSS as part of the Umbraco image; ASP.NET Core data-protection keys persist in the existing Docker data volume, and Umbraco Automate shares the CMS SQL database. Session storage remains in memory, so active sessions reset when the app container restarts.

The default local stack serves HTTP only. OpenIddict requires HTTPS for its sign-in endpoints. Configure the trusted local HTTPS overlay before using the Umbraco backoffice. When the overlay is active, requests to the HTTP development port redirect to the HTTPS port. Production also needs key encryption at rest and TLS termination. Provision the first SiteKit administrator through the documented allowlist/bootstrap flow; keep `.env` private and remove the temporary registration code after setup.

For local authenticated backoffice work, an opt-in HTTPS Compose overlay is included. Export and trust a development certificate on your machine, then start the overlay:

```powershell
New-Item -ItemType Directory -Force MyDockerProject\https | Out-Null
$env:SITEKIT_HTTPS_CERT_PASSWORD = 'choose-a-local-password'
dotnet dev-certs https -ep MyDockerProject\https\sitekit-dev.pfx -p $env:SITEKIT_HTTPS_CERT_PASSWORD
dotnet dev-certs https --trust
Set-Location MyDockerProject
docker compose -f docker-compose.yml -f docker-compose.https.yml up -d --build
```

Open `https://localhost:44373/umbraco`. The certificate and its password stay local and are excluded from Git and Docker build contexts. The overlay does not disable Umbraco's HTTPS checks; the default HTTP stack remains available on port 44372. Keep the password in the same PowerShell session when using the overlay. To stop the HTTPS override, use the same two Compose files with `down` (this does not remove named data volumes).

### Generate a project

Use SiteKit Studio or the local Agent Host's project tools. Generated projects target .NET 10 and include their own project manifest, runnable ASP.NET Core host, and Dockerfile. The detailed generated runtime guide explains credentials, project files, and its current production boundaries: [Generated application runtime](docs/SITEKIT_GENERATED_RUNTIME.md).

## Repository map

```text
SiteKit.Core/                         Contracts, validation, registries, generator, modules
SiteKit.Core.Tests/                   Focused platform contract and generated-runtime tests
SiteKit.AgentHost/                    Local ChatGPT orchestration, credentials, MCP tools
MyDockerProject/MyDockerProject/      Umbraco 17 host, Studio, hotel app, APIs, Automate hooks
frontend/                             React + TypeScript booking engine
docs/                                 Implementation ledger, runtime guides, screenshots
output/playwright/                    Committed generated-runtime screenshots
```

## Documentation

- [Implementation plan and feature ledger](docs/SITEKIT_IMPLEMENTATION_PLAN.md)
- [Getting started guide](docs/GETTING_STARTED.md)
- [Generated application runtime](docs/SITEKIT_GENERATED_RUNTIME.md)
- [Screenshot evidence and provenance](docs/SITEKIT_SCREENSHOTS.md)
- [C# component authoring SDK](docs/SITEKIT_COMPONENT_SDK.md)
- [Umbraco Automate boundaries](docs/SITEKIT_AUTOMATION.md)
- [Module package format](docs/SITEKIT_MODULE_PACKAGES.md)
- [Hotel data import example](MyDockerProject/sample-import.json)

## Validation snapshot

At the current checkpoint, `SiteKit.Core.Tests` has **29 passing Release tests** and the solution builds with **0 errors** in CI. The latest CI run also passed the NuGet vulnerability gate. The Docker Umbraco host is running locally with a healthy SQL service. An authenticated Studio session created a component through the visual editor, saved its schema, and rendered the shared definition in two independent projects with different content and appearance. HTTPS metadata, the HTTP-to-HTTPS redirect, project placements, rendered previews, and a SQL-backed Customer record plus its permission grant were checked against the running host; both remained available after an application-container restart. Role-based permissions are exercised in both the SiteKit host evaluator and the generated-app CRUD integration flow. Eligible ChatGPT subscription use, full authenticated Umbraco backoffice sign-in, broader multi-entity and relational SQL journeys, accessibility, performance, backup/restore, and production deployment acceptance still need further verification; see the ledger before relying on any capability in production.

## Contributing

Keep Umbraco responsible for editorial content and use application entities for transactional records. Preserve the hotel/booking starter. Update the implementation ledger whenever behavior or validation changes, and describe whether evidence is unit, generated-runtime, or live Umbraco evidence. Read [AGENTS.md](AGENTS.md) before making changes.

## License

No top-level project license file is present yet. A license should be selected before redistributing SiteKit; third-party packages retain their own license terms.

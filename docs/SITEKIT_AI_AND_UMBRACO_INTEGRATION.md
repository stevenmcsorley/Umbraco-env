# SiteKit AI and Umbraco integration decisions

Reviewed 2026-10-21 against current OpenAI and Umbraco documentation and
repositories. Versions and preview limits can change; re-check the linked
primary sources when implementing each integration.

## ChatGPT Sign in and plan usage

OpenAI's Sign in with ChatGPT flow supports both identity and separately
consented ChatGPT plan usage for eligible open-source clients. The current
open-source flow uses OIDC authorization code with PKCE, dynamically issued
client IDs, a stable opaque host ID, a validated ID token and locally stored
tokens. SiteKit should start the system browser and use a loopback callback in
the local Agent Host. Identity scopes do not authorize plan usage; request and
record plan scopes separately. Never collect ChatGPT passwords or session
cookies.

The plan-sharing Responses contract currently requires `store: false`,
`stream: true`, a complete `input` array on each HTTP request and supported
function/custom tools. It does not support persistent server conversation
state, hosted MCP/connectors, image generation, file search, Code Interpreter,
native computer use, audio/video inputs, or several usual Responses parameters.
History is persisted locally by SiteKit and supplied again as input. The local
Agent Host must refresh credentials and must act only for the connected user;
background operations need express consent. Plan usage remains subject to
account eligibility and limits. No shared inference proxy or paid managed
hosting path is planned without OpenAI programme approval.

Primary sources:

- [Sign in with ChatGPT quickstart](https://developers.openai.com/siwc/quickstart)
- [Open-source plan usage, host IDs and first inference](https://developers.openai.com/siwc/token-sharing-open-source)
- [Current preview request and tool limitations](https://developers.openai.com/siwc/token-sharing-open-source/preview-limitations)
- [Sign in with ChatGPT terms](https://openai.com/policies/sign-in-with-chatgpt-terms/)

The local `SiteKit.AgentHost` implementation now contains the public-client
OAuth/PKCE loopback flow, separate plan-use consent, ID-token signature and
claim validation, token refresh and revocation, account eligibility checks,
model discovery, and the required streaming Responses API request contract.
The host stores ChatGPT credentials on the user's machine (DPAPI CurrentUser on
Windows; owner-only config files on Unix). It does not include an API-key
fallback as a requirement. Live account sign-in and real inference still need
verification with an eligible account and OpenAI's dynamic client flow.

The host also provides a local stdio MCP server for compatible agent clients.
For plan-sharing inference, SiteKit invokes approved function tools inside the
local process because the current SIWC preview does not support hosted MCP
connections. The tool set is allowlisted and limits operations to component and
project state, validation, and a fixed repository build; every call is audited
without logging model arguments. A local conversation history file preserves
state while each HTTP inference request sends its complete input with
`store:false` and `stream:true`.

## Umbraco capabilities and reuse strategy

| Capability | Current fit | SiteKit decision |
|---|---|---|
| Block Grid and Block List | Stable native Umbraco 17 editors based on Element Types; already present in the hotel templates | Use for page composition on the 17 LTS baseline and keep rendering on native partials where possible |
| Library Elements | Available in Umbraco 18, a standard-term release; not part of 17 LTS | Do not raise the default baseline. Add an optional 18+ adapter when supported projects need shared Element content; SiteKit definitions remain CMS independent |
| Umbraco.AI / Copilot | Provider/profile, prompt and editor-copilot packages are available for Umbraco 17 | Evaluate for editorial AI and provider governance. Do not assume its API-key provider is the SIWC subscription runtime; SiteKit Agent Host owns plan usage |
| Umbraco Developer MCP | Official `@umbraco-cms/mcp-dev@17` tools cover CMS content types, documents, and media via an Umbraco OAuth client | Reuse for CMS authoring after user-controlled local setup. Build SiteKit MCP tools only for projects, dynamic component packages, app entities, screens, modules, workflows and builds |
| Umbraco Automate | Visual event-trigger/action workflows with run history, connections and extension points | Integrate it for content/media/member automation. Add SiteKit business event triggers/actions where a documented gap exists; persist app-domain state in SiteKit |
| Umbraco Search | Provider-based search with Examine/Lucene and custom entity indexing; repository/docs describe the add-on as beta | Keep current Examine behavior until a search need exists; prototype optional custom entity indexing and do not make beta Search a required runtime dependency yet |
| Management API | Native REST management API used by backoffice, protected by Umbraco auth policies and OAuth | Use supported Management APIs for core CMS operations; custom SiteKit operations use authenticated backoffice API patterns |
| Delivery API | Opt-in read API for published content, with filters, paging, expansion and preview authorization | Use for headless website delivery when appropriate; never treat it as an operational business CRUD API |
| Backoffice extension APIs | Native extension manifests and Lit based APIs support custom sections/dashboards and secured controllers | Build Studio surfaces as native extensions instead of replacing content, media, users or publishing UX |

Primary sources:

- [Umbraco 17 Management API](https://docs.umbraco.com/umbraco-cms/17.latest/reference/management-api)
- [Umbraco 17 Content Delivery API](https://docs.umbraco.com/umbraco-cms/17.latest/reference/content-delivery-api)
- [Umbraco CMS MCP developer server](https://github.com/umbraco/Umbraco-CMS-MCP-Dev)
- [Umbraco MCP documentation in the CMS source repository](https://github.com/umbraco/Umbraco-CMS/blob/main/MCP.md)
- [Umbraco Automate 17 documentation](https://docs.umbraco.com/umbraco-automate/17.latest)
- [Umbraco Search custom entity indexing](https://docs.umbraco.com/umbraco-search/17.latest/extending/data-from-other-sources)
- [Umbraco AI source repository](https://github.com/umbraco/Umbraco.AI)
- [Umbraco 18 Library Elements release notes](https://umbraco.com/blog/umbraco-18-release/)

## Current implementation status

- .NET 10 SDK 10.0.402 and Umbraco 17.7.1 build successfully.
- The existing hotel partials, Block Grid/List views, booking service, inventory
  service, React booking UI, importer, and Docker composition remain in place.
- The dynamic component registry, validated renderer, Component Studio, project
  and placement registries, project-local appearance, entity schemas, explicit
  permission grants, record APIs and independent project generator are
  implemented in the current working tree. Developer component package builds
  and publishing generated placements into CMS pages remain unfinished.
- Declarative module packages can be packed, integrity verified, imported and
  installed into individual projects. Screen/workflow JSON currently remains
  inert package content; there is no SiteKit workflow runner or Automate adapter.
- The ChatGPT OAuth and local MCP Agent Host implementations exist, but have
  not been exercised against OpenAI because this environment has no connected
  test account. Eligibility and model availability cannot be promised for every
  account or workspace.

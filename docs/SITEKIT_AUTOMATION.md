# SiteKit workflow integration

SiteKit installs the official `Umbraco.Automate` package (`17.5.1`) into the
Umbraco 17 host. Automate supplies the visual backoffice editor, published
automation lifecycle, execution history, scheduled/manual/webhook and content
published triggers, and its built-in actions. It uses WorkflowCore and its
provider model; SiteKit does not maintain a second general-purpose workflow
engine.

The package is discovered through the existing Umbraco composer startup. A
running Umbraco installation and database are required to verify dashboard
discovery, initial persistence setup and execution history. The local Docker
host and SQL Server are available; these Automate-specific runtime checks have
not yet been performed.

## SiteKit record trigger

SiteKit registers a native Automate trigger named **SiteKit Operational Record
Changed**. It is published after an authorized operational record is created,
updated or deleted. Trigger settings scope it to one project, entity and
operation. The trigger output contains the project, entity and record IDs,
operation and timestamp; it deliberately does not copy business-record fields
into workflow payloads. The event has an idempotency key derived from the
record identity and update timestamp.

Automations are configured, published and reviewed in the native Automate
backoffice. The SiteKit endpoint still enforces the initiating user's explicit
entity permission before publishing an event. SiteKit now contributes an
**Update SiteKit Operational Record** Automate action. Its JSON patch preserves
fields that are not included, validates the entity schema and relationship
targets, and emits the same record-change trigger after saving. The action
resolves the workspace service account from Automate's execution context and
requires an explicit SiteKit `update` or `manage` entity grant for that
service-account email. Its step output includes IDs and an updated-field count,
not business data. The update is safe to retry against the same record.

Direct user grants and project-role grants use the same permission evaluator.
Automate service-account emails can be assigned to a project role, so the
workspace's service account receives the same scoped entity operations as an
interactive account.

SiteKit also contributes a **Create SiteKit Operational Record** Automate
action. It resolves the service account from the Automate execution context,
requires that account's explicit `create` or `manage` grant for the selected
entity, validates the typed fields and relationship targets, and creates a
record with a deterministic SiteKit ID derived from a required stable
idempotency key. Replays return the existing record ID without overwriting its
data or emitting a duplicate event. The step output contains only project,
entity and record IDs. The action publishes the same record-changed event as
the authenticated API on first creation.

SiteKit also contributes a **Delete SiteKit Operational Record** Automate
action. It resolves the executing service account, requires an explicit
`delete` or `manage` entity grant, and calls the same record store as the API.
SQL relationship protections apply to this path too. Deleting a record that is
already absent is an idempotent success; a first deletion publishes the shared
record-change event. The output contains project, entity and record IDs and an
`alreadyDeleted` flag, never business-record data.

Imported module `workflows/*.json` remain inert declarations; they are not
installed as executable automations.

Do not treat imported workflow JSON as executable instructions. The trigger
and actions compile against Automate's extension API, but discovery, execution
and run history have not been exercised in a live Umbraco/database runtime.
External HTTP actions must retain Automate's SSRF
protections and require workspace-scoped credentials.

## Version and security

The host remains on Umbraco 17.7.1 / .NET 10 and pins Automate 17.5.1. Automate
brings OpenTelemetry.Api transitively; SiteKit explicitly pins 1.15.3 to avoid
the vulnerable 1.1.0 version selected by that dependency graph. The package
vulnerability scan reports no vulnerable packages for the Umbraco project.

References checked 22 October 2026:

- [Umbraco Automate 17 documentation](https://docs.umbraco.com/umbraco-automate/17)
- [Umbraco Automate repository and provider overview](https://github.com/umbraco/Umbraco.Automate)
- [Automate package 17.5.1](https://www.nuget.org/packages/Umbraco.Automate/17.5.1)
- [Official custom action extension documentation](https://docs.umbraco.com/umbraco-automate/18/extending/custom-action)
- [Automate custom action discovery](https://docs.umbraco.com/umbraco-automate/18/extending/custom-action) states that attributed actions are discovered at startup without manual registration. Live discovery is still unverified locally because the action catalogue requires a backoffice bearer token.
- [Automate action execution identity and permissions](https://github.com/umbraco/Umbraco.Automate/blob/v18/dev/docs/identity-ownership-permissions.md)
- [OpenTelemetry.Api security advisory](https://github.com/advisories/GHSA-g94r-2vxg-569j)

# SiteKit workflow integration

SiteKit installs the official `Umbraco.Automate` package (`17.5.1`) into the
Umbraco 17 host. Automate supplies the visual backoffice editor, published
automation lifecycle, execution history, scheduled/manual/webhook and content
published triggers, and its built-in actions. It uses WorkflowCore and its
provider model; SiteKit does not maintain a second general-purpose workflow
engine.

The package is discovered through the existing Umbraco composer startup. A
running Umbraco installation and database are required to verify its dashboard,
initial persistence setup and execution history. Those runtime checks have not
been possible in this workspace because Docker's Linux engine and SQL Server
are unavailable.

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
entity permission before publishing an event. SiteKit has not yet added
Automate actions for reading or changing operational records, and imported
module `workflows/*.json` remain inert declarations; they are not installed as
executable automations.

Do not treat imported workflow JSON as executable instructions. The trigger
compiles against Automate's extension API, but its delivery and run history
have not been exercised in a live Umbraco/database runtime. The next
integration step must add narrowly scoped actions with service-account and
entity permission checks. External HTTP actions must retain Automate's SSRF
protections and require workspace-scoped credentials.

## Version and security

The host remains on Umbraco 17.7.1 / .NET 10 and pins Automate 17.5.1. Automate
brings OpenTelemetry.Api transitively; SiteKit explicitly pins 1.15.3 to avoid
the vulnerable 1.1.0 version selected by that dependency graph. The package
vulnerability scan reports no vulnerable packages for the Umbraco project.

References checked 21 October 2026:

- [Umbraco Automate 17 documentation](https://docs.umbraco.com/umbraco-automate/17)
- [Umbraco Automate repository and provider overview](https://github.com/umbraco/Umbraco.Automate)
- [Automate package 17.5.1](https://www.nuget.org/packages/Umbraco.Automate/17.5.1)
- [OpenTelemetry.Api security advisory](https://github.com/advisories/GHSA-g94r-2vxg-569j)

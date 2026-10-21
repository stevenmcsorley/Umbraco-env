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

## Current boundary

Automate's built-in content events and actions are available to the Umbraco
installation after startup. SiteKit operational record operations are still
served by SiteKit's separate entity APIs. A SiteKit-specific Automate trigger
and action provider that dispatches and consumes customer, booking, inventory
and other operational-record events has not yet been implemented. Existing
module `workflows/*.json` files remain inert package declarations; they are not
installed as executable automations.

Do not treat imported workflow JSON as executable instructions. The next
integration step must add typed, permission-checked providers using Automate's
extension API and its persistent run/history infrastructure. External HTTP
actions must retain the Automate package's SSRF protections and require
workspace-scoped credentials.

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

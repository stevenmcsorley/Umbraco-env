# Generated SiteKit application runtime

Application and combined project generation now writes:

- `sitekit.entities.json` and `sitekit.screens.json` as validated project
  definitions.
- A small .NET 10 host with a model-driven screen runtime.
- `GET /api/sitekit/screens` and `GET /api/sitekit/entities` metadata routes.
- Entity record `GET`, `POST`, `PUT` and `DELETE` routes under
  `/api/sitekit/records/{entity}`.
- Per-entity JSON files under `App_Data`, written through a temporary file and
  atomic replacement.

All `/api/sitekit/*` requests require `Authorization: Bearer <token>`. Configure
`SITEKIT_ADMIN_TOKEN` in the process or container secret store. With no token,
the API returns `503`; with an invalid token, it returns `401`. The generated UI
asks for the token and keeps it in the current browser tab's `sessionStorage`.
The token is never generated into source or project manifests.

## Scope and production boundary

This runtime is a deployable reference and development starter. It uses one
administrator token for every API operation and local JSON storage. It does not
implement user identities, per-record roles, SQL transactions, multi-process
file locking, soft-delete retention, relational constraints or a database
backup strategy. Configure a real identity provider and database adapter before
using generated applications with multiple users or consequential business
data. The shared SiteKit/Umbraco host has a separate permission-grant and SQL
Server record-store implementation.

The generated-app build is covered by the generator contract test. Live
multi-user authorization and database performance have not been verified.

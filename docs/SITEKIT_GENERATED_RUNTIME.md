# Generated SiteKit application runtime

Application and combined project generation now writes:

- `sitekit.entities.json` and `sitekit.screens.json` as validated project
  definitions.
- A small .NET 10 host with a model-driven screen runtime.
- Generated dashboard metrics, searchable tables, calendar lists and kanban
  lanes with permission-checked stage updates.
- `GET /api/sitekit/screens` and `GET /api/sitekit/entities` metadata routes.
- Entity record `GET`, `POST`, `PUT` and `DELETE` routes under
  `/api/sitekit/records/{entity}`.
- A SQLite database at `App_Data/sitekit.db`, with an initial migration ledger,
  an entity-scoped primary key, an update-time index and transactional writes.

All `/api/sitekit/*` requests require `Authorization: Bearer <token>`. For a
single administrator, configure `SITEKIT_ADMIN_TOKEN` in the process or
container secret store. For multiple principals, configure
`SITEKIT_PRINCIPAL_TOKENS` as a JSON object mapping SHA-256 token hashes to
principal names, and `SITEKIT_ENTITY_GRANTS` as a JSON object mapping each
principal or `role:<alias>` to entity aliases and allowed operations (`read`,
`create`, `update`, `delete`). Optionally configure `SITEKIT_PRINCIPAL_ROLES`
as a JSON object mapping each principal name to its role aliases. A principal
receives the union of its direct and assigned-role grants. For example:

```json
SITEKIT_PRINCIPAL_TOKENS={"<sha256-token-hash>":"sales-reader"}
SITEKIT_PRINCIPAL_ROLES={"sales-reader":["sales-team"]}
SITEKIT_ENTITY_GRANTS={"role:sales-team":{"customer":["read"]}}
```

With no token configuration, the API returns `503`; with an invalid token, it
returns `401`; with a valid token but missing entity operation grant, it returns
`403`. The generated UI asks for a token and keeps it in the current browser
tab's `sessionStorage`. Tokens are never generated into source or project
manifests. Use a deployment secret store for plaintext bearer tokens.

## Scope and production boundary

This runtime is a deployable reference and development starter. Its built-in
token grants operate at entity and operation level; it does not implement user
identity federation, per-record roles, soft-delete retention or relational
constraints. Configure a real identity provider before using generated
applications with end users or consequential business data. Back up the SQLite
database as part of deployment. The shared SiteKit/Umbraco host has a separate
permission registry and SQL Server record-store implementation.

The generated-app build is covered by the generator contract test. The test
starts the emitted app, exercises administrator, direct-principal, and
role-derived grants (including denied writes), CRUD over HTTP, renames a screen
and regenerates the project, then verifies the updated screen manifest and
existing records are still available after restart. Production identity
federation and database performance have not been verified.

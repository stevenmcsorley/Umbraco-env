# SiteKit operational data models

Use a SiteKit entity for operational/business records that need typed fields,
relationships, record permissions and persistent CRUD. Use Umbraco document types
for editorial content, publishing and media workflows. Operational records do
not have to be content-tree nodes.

## Studio workflow

Open **Projects and data** (`/studio/projects`), select an application project,
then create or select a data model. The visual editor configures the entity
name, URL-safe alias, schema version and fields. Fields support text, email,
number, boolean, date, datetime, JSON, single relationship and multiple
relationships. Required and unique validation, text maximum length and related
entity targets are available. The advanced JSON editor remains available for
properties not shown visually.

Use unique fields to define stable business keys (for example, customer email or
external import ID). Relationship values are record UUIDs; API validation
checks referenced entity schemas and records. In SQL deployments, records are
stored in the separate `SiteKitOperationalRecord` table with schema version,
creation/update actor and timestamps, and soft deletion.

## Schema evolution

Increment the entity version when making schema changes. The registry accepts
safe optional additive fields one version at a time. Removing a field, changing
an existing field's type or constraint, or changing relationships is rejected
by the current migration guard so existing records are not silently
reinterpreted. Plan a future explicit migration before making such a change.

## Permissions

Allowlisted SiteKit administrators grant access to account emails. Operations
are explicit: `read`, `create`, `update`, `delete`, `import` or `manage`.
`manage` implies each operation. Import uses the dedicated `import` grant in
addition to the authenticated SiteKit administrator session. Studio record
forms and APIs enforce grants per operation; a project administrator should
grant only the operations an account needs.

## Current limits

Entity definitions and permission grants are versioned JSON files in the
SiteKit data directory. SQL Server is the production record provider; JSON
record storage is a local development fallback. SQL schema migration, backup
and restore, live relationship/permission tests, and a production database
deployment check remain outstanding. Do not use the JSON fallback for
multi-instance production hosting.

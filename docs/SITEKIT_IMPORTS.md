# SiteKit universal imports

SiteKit Studio exposes `/studio/imports` for saved, project-scoped import
mappings. Mappings target operational entities and are persisted beneath
`umbraco/Data/SiteKit/imports/{projectId}`. The existing hotel/content importer
remains available for its current use cases.

## Supported source formats

- **JSON:** an array of objects.
- **CSV:** UTF-8 rows, optional header, one-character delimiter (comma in the
  current Studio), quoted values and doubled quote escapes.
- **XML:** repeated child elements under a document root. DTD processing and
  external entity resolution are disabled.

Field mapping is stored as `sourceName -> entityFieldAlias`. Match fields must
be unique entity fields, and all match values must be present in an incoming
row. Existing records with a match are updated; fields absent from an input row
are retained. Repeating an import with the same unique key updates the same
record instead of duplicating it.

## API

All routes require an authenticated SiteKit administrator session and CSRF
protection from the existing SiteKit admin authorization filter.

| Method and route | Purpose |
|---|---|
| `GET /api/sitekit/projects/{projectId}/imports` | List saved mappings |
| `PUT /api/sitekit/projects/{projectId}/imports/{alias}` | Validate and save a mapping |
| `POST /api/sitekit/projects/{projectId}/imports/{alias}/preview` | Validate an uploaded file without writing records |
| `POST /api/sitekit/projects/{projectId}/imports/{alias}/run` | Upsert valid rows and return per-row errors |

Preview requires the entity `read` or `manage` grant. Run requires `import` or
`manage`. The authenticated account is recorded as the actor for saved rows.
Uploads are limited to 10 MiB and 5,000 data rows. A preview returns row counts,
up to ten converted samples and row-level validation errors. Import continues
past invalid rows and returns created/updated totals with row errors.

## Current boundaries

The implementation does not yet provide media transfer, background/resumable
jobs, automatic AI field mapping, import execution history, relationship-aware
deduplication or configurable CSV delimiters in Studio. Imports execute during
the authenticated request and should therefore stay within the documented
size/row limits. SQL-backed entity validation and import endpoints need a live
Umbraco/SQL installation for end-to-end verification.

## Verification

`SiteKit.Core.Tests` covers CSV, JSON and XML preview/import, unique-key
upserts, retention of fields omitted by an input, invalid-row preview and the
local JSON record-store behavior. The Admin API and Razor Studio compile in the
Umbraco host. See [SITEKIT_SCREENSHOTS.md](SITEKIT_SCREENSHOTS.md) for the
fixture screenshot and its runtime limitations.

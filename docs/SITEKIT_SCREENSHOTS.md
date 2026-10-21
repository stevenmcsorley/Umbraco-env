# SiteKit runtime screenshots

These showcase captures come from independently generated .NET applications
running locally. They demonstrate working project output; they are **not** a
hosted SiteKit URL or a live Umbraco backoffice. The CRM uses its generated
SQLite store and a local test access token. Screenshots of the authenticated
Umbraco Studio views are pending a live Umbraco and SQL Server run; no fixture
mockups are included in this gallery.

## Generated CRM

![Generated CRM with sales overview counts, customer records and grouped opportunities](../output/playwright/sitekit-generated-crm-runtime.png)

The application was running locally with three persisted customers and three
opportunities. The screenshot shows its overview, customer table, and pipeline
board. Browser console check reported no errors. The integration test separately
verifies permission denial, CRUD, relationship validation, delete protection,
and data retention through restart.

## Shared component across two generated websites

![Northstar Advisory shared component example](../output/playwright/sitekit-generated-shared-component-one.png)

![Lumen House shared component example](../output/playwright/sitekit-generated-shared-component-two.png)

These two running .NET sites share one registered `shared-banner` component
definition. Northstar Advisory uses blue styling and strategy copy; Lumen House
uses a warm palette, serif typography, and hospitality copy. Both sites were
built and served locally from generated project output. Each screenshot shows
the shared component receiving independent project data and appearance.

# SiteKit Studio and generated project screenshots

The Component Studio and Projects Studio captures are static Playwright previews backed by local fixture data. They are **not** screenshots from the running Umbraco backoffice. This machine has no SQL Server service and its Docker Desktop Linux engine is unavailable. Their banner marks this limitation.

## Component Studio

![SiteKit Components Studio static visual preview](../output/playwright/sitekit-components-static-preview.png)

This shows the component catalogue, definition editor, schema/layout controls, variants and preview.

## Projects Studio

![SiteKit Projects Studio static visual preview](../output/playwright/sitekit-projects-static-preview.png)

This shows the project list, shared component placement, project-local appearance settings and rendered preview. It predates the data-model editor added in M5.

## Generated website runtime

![SiteKit generated website running locally](../output/playwright/sitekit-generated-website.png)

This screenshot is from a running independently generated .NET website. The generated site built with .NET 10; `/health` returned `{"status":"healthy"}` and `/` returned HTTP 200. This is runtime evidence for project generation, not live Umbraco, SQL CRUD or authorization coverage.

Reproducible Studio fixture HTML pages are included beside the screenshots.

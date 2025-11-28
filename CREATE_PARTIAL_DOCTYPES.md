# Creating Document Types for Partials

## Overview

All Razor partials now have corresponding Element Types in Umbraco, allowing content editors to manage component data through the backoffice.

## Created Element Types

The following Element Types have been created:

1. **Hero** (`hero`) - Heading, tagline, image, background/text colors
2. **Gallery** (`gallery`) - Title, images collection
3. **Features** (`features`) - Title, feature items (Block List)
4. **FAQ** (`faq`) - Title, questions (Block List)
5. **Cards** (`cards`) - Title, card items (Block List)
6. **CTA Panel** (`ctaPanel`) - Heading, description, button text/link
7. **Events** (`events`) - Title, event items (Block List)
8. **Offers** (`offers`) - Title, offer items (Block List)
9. **Rooms** (`rooms`) - Title, room items (Block List)
10. **Rich Text** (`richText`) - Content (Rich Text Editor)
11. **Testimonials** (`testimonials`) - Title, testimonial items (Block List)
12. **Accordion** (`accordion`) - Title, accordion items (Block List)
13. **Tabs** (`tabs`) - Title, tab items (Block List)
14. **Map** (`map`) - Title, latitude, longitude, address

## How to Create

### Via API (Recommended)

```bash
POST http://localhost:44372/api/seed/create-partial-element-types
```

This will create all Element Types at once.

### Verify Creation

1. Go to **Settings** → **Document Types**
2. Look for Element Types (they'll have an "Element" badge)
3. Each Element Type will have a "Content" property group with its properties

## Using Element Types

These Element Types can be used in:

- **Block List** - Add them as block items
- **Block Grid** - Add them as grid items
- **Nested Content** - Use them as nested content items

## Next Steps

1. **Rebuild Umbraco**:
   ```bash
   docker-compose up -d --build mydockerproject
   ```

2. **Create Element Types**:
   ```bash
   curl -X POST http://localhost:44372/api/seed/create-partial-element-types
   ```

3. **Verify in Backoffice**:
   - Go to Settings → Document Types
   - Check that all Element Types are visible
   - Open each one to see its properties

## Fixing Hotel Document Type

The Hotel document type properties exist but aren't visible in the UI because they're not properly assigned to a property group. 

**Current Status**: Properties exist in database but not in UI.

**Solution**: The properties need to be manually assigned to a property group in the Umbraco backoffice, OR we need to use the Umbraco Management API to properly assign them programmatically.

To fix manually:
1. Go to **Settings** → **Document Types** → **Hotel**
2. Click **Design** tab
3. Click **Add group** → Name it "Content"
4. For each property, drag it into the "Content" group
5. Click **Save**


# Content Structure Guide

## Two Approaches for Pages

### Approach 1: Umbraco Content Nodes (Traditional CMS)
Create document types and content nodes in Umbraco backoffice. Content editors manage everything through the CMS.

### Approach 2: API-Driven Pages (Current Setup)
Use custom routes that fetch data from APIs. Content can come from Umbraco APIs or external sources.

---

## Current Setup

### ✅ Homepage (Already Works)
- **Document Type**: `Home` (already exists in Umbraco)
- **Properties**: `Name`, `BodyText`
- **Template**: `home.cshtml`
- **How to Edit**: Go to Content → homepage → Edit

**You can edit the homepage content right now in Umbraco backoffice!**

### ❌ Hotel Pages (Need Setup)
Currently uses **custom routes** (`/hotels`) that fetch from APIs. You have two options:

#### Option A: Keep API-Driven (Current)
- No document types needed
- Content created via API: `POST /api/seed/create-demo-hotel`
- Pages work via custom routes

#### Option B: Use Umbraco Content Nodes
1. Create document types: Hotel, Room, Offer
2. Create templates in backoffice
3. Create content nodes in backoffice
4. Pages accessible via Umbraco URLs

---

## What You Need to Create

### For Homepage (Optional Enhancement)
If you want more fields on the homepage:

1. **Settings** → **Document Types** → **Home**
2. Add properties:
   - `heroHeading` (Textstring)
   - `heroTagline` (Textarea)
   - `heroImage` (Media Picker)
   - `features` (Block List) - for features section
3. Update `home.cshtml` to use these properties

### For Hotel Pages (If Using Option B)
1. Create **Hotel** document type (see `QUICK_START_CONTENT.md`)
2. Create **Room** document type
3. Create **Offer** document type
4. Create templates in backoffice
5. Assign templates to document types
6. Create content nodes

---

## Templates vs Document Types

- **Templates** = Razor views (.cshtml files) - define how pages look
- **Document Types** = Data structures - define what fields content has

**Current Status:**
- ✅ Templates exist: `home.cshtml`, `hotelList.cshtml`, `hotel.cshtml`, `room.cshtml`
- ❌ Document Types: Only `Home` exists, need `Hotel`/`Room`/`Offer` if using Option B

---

## Recommendation

**For now, keep it simple:**
1. **Homepage**: Edit existing `Home` document type in backoffice (add more properties if needed)
2. **Hotels**: Use API-driven approach (create content via API or manually in backoffice after creating document types)

**Later, you can:**
- Create Hotel/Room/Offer document types
- Create templates in backoffice
- Assign templates to document types
- Create content nodes in backoffice

---

## Quick Start: Edit Homepage

1. Go to: https://localhost:44372/umbraco
2. **Content** → **homepage**
3. Edit `Name` and `BodyText` fields
4. **Save and Publish**
5. Refresh homepage to see changes

The homepage will use whatever you put in `Name` and `BodyText`!


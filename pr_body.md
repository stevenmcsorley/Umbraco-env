## Umbraco Site Kit Implementation

This PR adds a complete Umbraco Site Kit system for programmatically creating Document Types and Element Types with their properties.

### Features Added

✅ **Programmatic Element Type Creation**
- Creates all 14 Element Types (Hero, Gallery, Features, FAQ, Cards, CTA Panel, Events, Offers, Rooms, Rich Text, Testimonials, Accordion, Tabs, Map)
- Two-phase approach: Create Element Types first, then add properties
- Handles duplicate uniqueId errors gracefully
- Properties automatically assigned to 'content' group for UI visibility

✅ **Programmatic Document Type Creation**
- Creates Hotel, Room, Offer Document Types
- Adds properties to Document Types programmatically

✅ **API Endpoints**
- `POST /api/seed/create-partial-element-types` - Create all Element Types
- `POST /api/seed/add-properties-to-element-types` - Add properties to Element Types
- `POST /api/seed/create-document-types` - Create Document Types
- `POST /api/seed/add-hotel-properties` - Add properties to Hotel
- Management endpoints for cleanup and troubleshooting

✅ **Documentation**
- Complete Site Kit guide in `docs/UMBRACO_SITE_KIT.md`
- Updated README.md with quick start guide
- API documentation with examples

### Key Files

- `Services/PartialElementTypeService.cs` - Element Type creation logic
- `Services/DocumentTypePropertyService.cs` - Property management
- `Controllers/Api/SeedController.cs` - API endpoints
- `docs/UMBRACO_SITE_KIT.md` - Complete documentation

### Testing

All 14 Element Types successfully created with 33 properties total. Properties are visible in Umbraco backoffice Design tab under 'content' group.

### Usage

```powershell
# Create all Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-partial-element-types" -Method POST

# Add properties
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-properties-to-element-types" -Method POST
```

See `docs/UMBRACO_SITE_KIT.md` for complete documentation.


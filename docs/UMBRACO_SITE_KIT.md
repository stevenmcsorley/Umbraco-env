# Umbraco Site Kit - Programmatic Document Type & Element Type Creation

## Overview

The Umbraco Site Kit is a powerful system for programmatically creating Document Types and Element Types with their properties, eliminating the need for manual setup in the Umbraco backoffice. This system allows you to:

- Create Document Types (like Hotel, Room, Offer) with properties
- Create Element Types (reusable components like Hero, Gallery, Features, etc.) with properties
- Automatically assign properties to "content" groups for UI visibility
- Handle duplicate key errors gracefully
- Reuse existing properties to avoid conflicts

## Architecture

### Key Components

1. **DocumentTypeService** (`MyDockerProject/Services/DocumentTypeService.cs`)
   - Creates Document Types (Hotel, Room, Offer)
   - Handles property creation and assignment

2. **DocumentTypePropertyService** (`MyDockerProject/Services/DocumentTypePropertyService.cs`)
   - Adds properties to existing Document Types
   - Ensures properties are assigned to property groups

3. **PartialElementTypeService** (`MyDockerProject/Services/PartialElementTypeService.cs`)
   - Creates Element Types (Hero, Gallery, Features, FAQ, etc.)
   - Adds properties to Element Types
   - Handles property reuse and duplicate key errors

4. **SeedController** (`MyDockerProject/Controllers/Api/SeedController.cs`)
   - API endpoints for creating Document Types and Element Types
   - Endpoints for cleanup and management

## API Endpoints

### Document Types

#### Create Document Types
```http
POST /api/seed/create-document-types
```
Creates Hotel, Room, and Offer Document Types.

#### Add Hotel Properties
```http
POST /api/seed/add-hotel-properties
```
Adds properties to the Hotel Document Type:
- hotelName (Textstring, mandatory)
- description (Textarea)
- address (Textstring)
- city (Textstring)
- country (Textstring)
- phone (Textstring)
- email (Textstring)

### Element Types

#### Create All Element Types
```http
POST /api/seed/create-partial-element-types
```
Creates all 14 Element Types:
- Hero
- Gallery
- Features
- FAQ
- Cards
- CTA Panel
- Events
- Offers
- Rooms
- Rich Text
- Testimonials
- Accordion
- Tabs
- Map

#### Add Properties to Element Types
```http
POST /api/seed/add-properties-to-element-types
```
Adds properties to all Element Types. Properties are automatically assigned to the "content" group.

**Response:**
```json
{
  "success": true,
  "message": "Properties added to 14 Element Types",
  "elementTypes": [
    {
      "name": "Hero",
      "alias": "hero",
      "propertyCount": 5,
      "hasContentGroup": true
    },
    ...
  ]
}
```

### Management Endpoints

#### List Element Types
```http
GET /api/seed/list-element-types
```
Returns all Element Types with their property counts and content group status.

#### List All Properties
```http
GET /api/seed/list-all-properties
```
Lists all properties matching Element Type property aliases.

#### Cleanup Orphaned Properties
```http
POST /api/seed/cleanup-orphaned-properties
```
Finds and deletes properties not associated with any ContentType.

#### Delete Properties by UniqueId
```http
POST /api/seed/delete-properties-by-uniqueid
Body: ["uniqueId1", "uniqueId2", ...]
```
Deletes specific properties by their uniqueIds (useful when resolving duplicate key errors).

#### Reset All Element Types
```http
POST /api/seed/reset-all-element-types
```
Deletes all Element Types and their properties (nuclear reset).

## How It Works

### Creating Element Types

The system uses a two-phase approach:

1. **Phase 1: Create Empty Element Types**
   - Creates Element Types without properties to avoid duplicate key errors
   - Ensures "content" property group exists
   - Saves Element Types immediately

2. **Phase 2: Add Properties**
   - Checks if properties already exist in any ContentType
   - Reuses existing properties when possible
   - Creates new properties only when they don't exist
   - Handles duplicate uniqueId errors gracefully

### Property Group Assignment

Properties **must** be assigned to a property group (like "content") to appear in the Umbraco backoffice Design tab. The system:

1. Ensures the "content" group exists
2. Adds properties to the group's `PropertyTypes` collection
3. Saves the ContentType to persist changes

### Handling Duplicate UniqueIds

Umbraco generates uniqueIds deterministically for properties with the same alias and DataType. The system handles this by:

1. Checking if a property exists before creating it
2. Reusing existing PropertyType instances when possible
3. Catching duplicate key errors and reloading the ContentType
4. Adding existing properties from the database to the group

## File Structure

```
MyDockerProject/
├── Services/
│   ├── DocumentTypeService.cs          # Creates Document Types
│   ├── DocumentTypePropertyService.cs  # Adds properties to Document Types
│   └── PartialElementTypeService.cs   # Creates Element Types and properties
├── Controllers/
│   └── Api/
│       └── SeedController.cs          # API endpoints
└── docs/
    └── UMBRACO_SITE_KIT.md            # This file
```

## Usage Examples

### PowerShell - Create All Element Types

```powershell
# Create Element Types
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/create-partial-element-types" -Method POST

# Add Properties
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/add-properties-to-element-types" -Method POST

# Check Status
$list = Invoke-RestMethod -Uri "http://localhost:44372/api/seed/list-element-types" -Method GET
$list.elementTypes | ForEach-Object { Write-Host "$($_.name): $($_.propertyCount) properties" }
```

### cURL - Create Document Types

```bash
# Create Document Types
curl -X POST http://localhost:44372/api/seed/create-document-types

# Add Hotel Properties
curl -X POST http://localhost:44372/api/seed/add-hotel-properties
```

## Element Types Created

### Hero
- **Properties:** heading (Textstring, mandatory), tagline (Textarea), image (MediaPicker3), backgroundColor (Textstring), textColor (Textstring)
- **Use Case:** Hero sections with heading, tagline, and background image

### Gallery
- **Properties:** title (Textstring), images (MediaPicker3)
- **Use Case:** Image galleries

### Features
- **Properties:** title (Textstring), items (BlockList)
- **Use Case:** Feature lists with icons/text

### FAQ
- **Properties:** title (Textstring), questions (BlockList)
- **Use Case:** Frequently asked questions

### Cards
- **Properties:** title (Textstring), cardItems (BlockList)
- **Use Case:** Card layouts

### CTA Panel
- **Properties:** title (Textstring), description (Textarea), buttonText (Textstring), buttonLink (Textstring)
- **Use Case:** Call-to-action panels

### Events
- **Properties:** title (Textstring), eventItems (BlockList)
- **Use Case:** Event listings

### Offers
- **Properties:** title (Textstring), offerItems (BlockList)
- **Use Case:** Special offers/promotions

### Rooms
- **Properties:** title (Textstring), roomItems (BlockList)
- **Use Case:** Room listings

### Rich Text
- **Properties:** content (Textarea)
- **Use Case:** Rich text content blocks

### Testimonials
- **Properties:** title (Textstring), testimonialItems (BlockList)
- **Use Case:** Customer testimonials

### Accordion
- **Properties:** title (Textstring), items (BlockList)
- **Use Case:** Accordion/collapsible content

### Tabs
- **Properties:** title (Textstring), tabItems (BlockList)
- **Use Case:** Tabbed content

### Map
- **Properties:** address (Textstring), latitude (Textstring), longitude (Textstring)
- **Use Case:** Location maps

## Key Implementation Details

### Property Group Creation

```csharp
private PropertyGroup EnsureContentGroup(ContentType contentType, bool saveImmediately = true)
{
    const string groupAlias = "content";
    const string groupName = "Content";
    
    var group = contentType.PropertyGroups.FirstOrDefault(g => 
        g.Alias == groupAlias || g.Name == groupName);
    
    if (group == null)
    {
        contentType.AddPropertyGroup(groupName, groupAlias);
        if (saveImmediately)
        {
            _contentTypeService.Save(contentType);
            // Reload to get persisted group
            var reloaded = _contentTypeService.Get(contentType.Alias);
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                group = contentType.PropertyGroups.FirstOrDefault(g => 
                    g.Alias == groupAlias || g.Name == groupName);
            }
        }
    }
    
    return group;
}
```

### Safe Property Addition

```csharp
private void AddPropertySafely(ContentType elementType, string alias, string name, 
    IDataType dataType, bool mandatory = false, int sortOrder = 0)
{
    // Check if property exists in Element Type
    // Check if property exists in ANY ContentType
    // Create or reuse property
    // Add to content group
    // Handle duplicate key errors
}
```

## Troubleshooting

### Duplicate Key Errors

If you see errors like:
```
Cannot insert duplicate key row in object 'dbo.cmsPropertyType' with unique index 'IX_cmsPropertyTypeUniqueID'
```

**Solution:**
1. Use the cleanup endpoint to remove orphaned properties
2. Or delete properties by uniqueId:
```powershell
$uniqueIds = @("uniqueId1", "uniqueId2")
$body = $uniqueIds | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:44372/api/seed/delete-properties-by-uniqueid" -Method POST -Body $body -ContentType "application/json"
```

### Properties Not Visible in Backoffice

If properties aren't visible in the Design tab:
1. Ensure properties are assigned to a property group (not just added to ContentType)
2. Check that the "content" group exists
3. Refresh the Umbraco backoffice (Ctrl+F5)

### Element Types Not Created

If Element Types aren't being created:
1. Check Umbraco logs for errors
2. Verify data types exist (Textstring, Textarea, MediaPicker3, BlockList)
3. Use `GET /api/seed/list-element-types` to check what exists

## Best Practices

1. **Always create Element Types first** (empty), then add properties
2. **Use the "content" property group** for UI visibility
3. **Handle errors gracefully** - continue even if some properties fail
4. **Check for existing properties** before creating new ones
5. **Save ContentTypes** after adding property groups and properties

## Future Enhancements

- [ ] Support for nested properties
- [ ] Template creation for Element Types
- [ ] Validation rules programmatic setup
- [ ] Compositions support
- [ ] Import/export functionality

## Related Files

- `MyDockerProject/Services/DocumentTypeService.cs` - Document Type creation
- `MyDockerProject/Services/DocumentTypePropertyService.cs` - Property management
- `MyDockerProject/Services/PartialElementTypeService.cs` - Element Type creation
- `MyDockerProject/Controllers/Api/SeedController.cs` - API endpoints

## License

Part of the Umbraco Site Kit system for programmatic content type management.


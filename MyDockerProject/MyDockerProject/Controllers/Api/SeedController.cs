using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Core.Models;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IScopeProvider _scopeProvider;
    private readonly IFileService _fileService;

    public SeedController(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        IScopeProvider scopeProvider,
        IFileService fileService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _scopeProvider = scopeProvider;
        _fileService = fileService;
    }

    [HttpPost("create-document-types")]
    public IActionResult CreateDocumentTypes()
    {
        try
        {
            var docTypeService = new Services.DocumentTypeService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            docTypeService.CreateAllDocumentTypes();

            return Ok(new
            {
                success = true,
                message = "Document types created successfully: Hotel, Room, Offer, Event"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-offer-document-type")]
    public IActionResult CreateOfferDocumentType()
    {
        try
        {
            var docTypeService = new Services.DocumentTypeService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            docTypeService.CreateOfferDocumentType();

            return Ok(new
            {
                success = true,
                message = "Offer document type created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-event-document-type")]
    public IActionResult CreateEventDocumentTypeEndpoint()
    {
        try
        {
            var docTypeService = new Services.DocumentTypeService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            docTypeService.CreateEventDocumentType();

            return Ok(new
            {
                success = true,
                message = "Event document type created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-addon-document-type")]
    public IActionResult CreateAddOnDocumentTypeEndpoint()
    {
        try
        {
            var docTypeService = new Services.DocumentTypeService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            docTypeService.CreateAddOnDocumentType();

            return Ok(new
            {
                success = true,
                message = "AddOn document type created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("seed-offers-events")]
    public IActionResult SeedOffersAndEvents()
    {
        try
        {
            var seedService = new Services.SeedDataService(_contentService, _contentTypeService);
            seedService.AddOffersAndEventsToExistingHotels();

            return Ok(new
            {
                success = true,
                message = "Offers and events added to all existing hotels successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-default-datatypes")]
    public IActionResult CreateDefaultDataTypes()
    {
        try
        {
            var dataTypeService = new Services.DataTypeService(_dataTypeService);
            var missing = dataTypeService.CheckMissingDataTypes();
            var required = dataTypeService.GetRequiredDataTypes();

            if (missing.Any())
            {
                return Ok(new
                {
                    success = false,
                    message = "Some required data types are missing. Please create them in Umbraco backoffice.",
                    missing = missing,
                    instructions = "Go to Settings > Data Types and create data types with these editors:",
                    required = required.Select(kvp => new { name = kvp.Key, editor = kvp.Value })
                });
            }

            return Ok(new
            {
                success = true,
                message = "All required data types exist!",
                existing = required.Keys.ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpGet("check-datatypes")]
    public IActionResult CheckDataTypes()
    {
        try
        {
            var dataTypeService = new Services.DataTypeService(_dataTypeService);
            var missing = dataTypeService.CheckMissingDataTypes();
            var required = dataTypeService.GetRequiredDataTypes();
            var allDataTypes = _dataTypeService.GetAll().ToList();

            return Ok(new
            {
                required = required.Select(kvp => new
                {
                    name = kvp.Key,
                    editor = kvp.Value,
                    exists = allDataTypes.Any(dt => dt.EditorAlias == kvp.Value)
                }),
                missing = missing,
                totalRequired = required.Count,
                totalExisting = allDataTypes.Count(dt => required.Values.Contains(dt.EditorAlias))
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("add-hotel-properties")]
    public IActionResult AddHotelProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            propertyService.AddHotelProperties();

            return Ok(new
            {
                success = true,
                message = "Hotel properties added successfully to 'content' group"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("add-room-properties")]
    public IActionResult AddRoomProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            propertyService.AddRoomProperties();

            return Ok(new
            {
                success = true,
                message = "Room properties added successfully to 'content' group"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("add-event-properties")]
    public IActionResult AddEventProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            propertyService.AddEventProperties();

            return Ok(new
            {
                success = true,
                message = "Event properties added successfully to 'content' group"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("add-offer-properties")]
    public IActionResult AddOfferProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            propertyService.AddOfferProperties();

            return Ok(new
            {
                success = true,
                message = "Offer properties added successfully to 'content' group"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("add-home-properties")]
    public IActionResult AddHomeProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            propertyService.AddHomeProperties();

            return Ok(new
            {
                success = true,
                message = "Home properties added successfully to 'content' group"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("check-room-document-type")]
    public IActionResult CheckRoomDocumentType()
    {
        try
        {
            var roomType = _contentTypeService.Get("room");
            
            if (roomType == null)
            {
                return Ok(new
                {
                    exists = false,
                    message = "Room document type does not exist. Create it via: POST /api/seed/create-document-types",
                    isElement = false,
                    alias = "room"
                });
            }

            return Ok(new
            {
                exists = true,
                alias = roomType.Alias,
                name = roomType.Name,
                isElement = roomType.IsElement,
                allowedAsRoot = roomType.AllowedAsRoot,
                parentId = roomType.ParentId,
                propertyCount = roomType.PropertyTypes.Count(),
                properties = roomType.PropertyTypes.Select(pt => new { alias = pt.Alias, name = pt.Name }).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpGet("check-hotel-properties")]
    public IActionResult CheckHotelProperties()
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            var missing = propertyService.GetMissingHotelProperties();
            var hotelType = _contentTypeService.Get("hotel");

            if (hotelType == null)
            {
                return Ok(new
                {
                    exists = false,
                    message = "Hotel document type does not exist"
                });
            }

            var existingProperties = hotelType.PropertyTypes.Select(pt => new
            {
                alias = pt.Alias,
                name = pt.Name,
                mandatory = pt.Mandatory
            }).ToList();

            // Check property groups and which properties are in them
            var propertyGroups = hotelType.PropertyGroups.Select(g => new
            {
                name = g.Name,
                alias = g.Alias,
                propertyCount = g.PropertyTypes.Count,
                properties = g.PropertyTypes.Select(pt => pt.Alias).ToList()
            }).ToList();

            // Check which properties are NOT in any group
            var propertiesInGroups = hotelType.PropertyGroups
                .SelectMany(g => g.PropertyTypes.Select(pt => pt.Alias))
                .Distinct()
                .ToList();
            
            var propertiesNotInGroups = hotelType.PropertyTypes
                .Where(pt => !propertiesInGroups.Contains(pt.Alias))
                .Select(pt => pt.Alias)
                .ToList();

            return Ok(new
            {
                exists = true,
                existingProperties = existingProperties,
                missingProperties = missing,
                allPropertiesExist = !missing.Any(),
                propertyGroups = propertyGroups,
                propertiesNotInGroups = propertiesNotInGroups,
                totalPropertyGroups = hotelType.PropertyGroups.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("add-layout-property")]
    public IActionResult AddLayoutProperty([FromQuery] string? documentTypeAlias = null)
    {
        try
        {
            var propertyService = new Services.DocumentTypePropertyService(_contentTypeService, _dataTypeService, _shortStringHelper);
            
            if (!string.IsNullOrEmpty(documentTypeAlias))
            {
                propertyService.AddLayoutPropertyToDocumentType(documentTypeAlias);
                return Ok(new
                {
                    success = true,
                    message = $"Layout property added to '{documentTypeAlias}' document type",
                    note = "If using Textstring, convert to Dropdown in Settings → Data Types and add values: Main, HolyGrail, Sidebar, Centered, FullWidth"
                });
            }
            else
            {
                propertyService.AddLayoutPropertyToAllDocumentTypes();
                return Ok(new
                {
                    success = true,
                    message = "Layout property added to all document types (hotel, room, offer, home)",
                    note = "IMPORTANT: To enable dropdown with options, go to Settings → Data Types → Find the data type used for 'Layout' property → Configure it as Dropdown and add these values: Main, HolyGrail, Sidebar, Centered, FullWidth"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("add-offers-events-to-hotels")]
    public IActionResult AddOffersAndEventsToHotels()
    {
        try
        {
            var seedService = new Services.SeedDataService(_contentService, _contentTypeService);
            seedService.AddOffersAndEventsToExistingHotels();

            return Ok(new
            {
                success = true,
                message = "Offers and events added to all existing hotels successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-demo-hotel")]
    public IActionResult CreateDemoHotel()
    {
        try
        {
            // Check if content types exist
            var hotelType = _contentTypeService.Get("hotel");
            if (hotelType == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Content type 'hotel' does not exist",
                    message = "Please create the 'hotel' content type in Umbraco backoffice first. " +
                              "Go to Settings > Document Types and create a document type with alias 'hotel'."
                });
            }

            // Create demo hotel using seed service
            var seedService = new Services.SeedDataService(_contentService, _contentTypeService);
            seedService.SeedExampleHotel();

            return Ok(new
            {
                success = true,
                message = "Demo hotel 'Grand Hotel Example' created successfully with rooms and offers"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("add-properties-to-element-types")]
    public IActionResult AddPropertiesToElementTypes()
    {
        try
        {
            var partialService = new Services.PartialElementTypeService(_contentTypeService, _dataTypeService, _shortStringHelper);
            partialService.AddPropertiesToAllElementTypes();
            
            // Check which Element Types have properties now
            var allTypes = _contentTypeService.GetAll();
            var elementTypes = allTypes.Where(ct => ct.IsElement).ToList();
            
            var results = elementTypes.Select(et => new
            {
                name = et.Name,
                alias = et.Alias,
                propertyCount = et.PropertyTypes.Count(),
                hasContentGroup = et.PropertyGroups.Any(g => g.Alias == "content" || g.Name == "Content")
            }).ToList();
            
            return Ok(new
            {
                success = true,
                message = $"Properties added to {elementTypes.Count} Element Types",
                elementTypes = results
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-partial-element-types")]
    public IActionResult CreatePartialElementTypes()
    {
        var created = new List<string>();
        var failed = new List<string>();
        
        var partialService = new Services.PartialElementTypeService(_contentTypeService, _dataTypeService, _shortStringHelper);
        
        // Use CreateAllPartialElementTypes which handles errors internally
        try
        {
            partialService.CreateAllPartialElementTypes();
        }
        catch (Exception ex)
        {
            // Log but continue - individual Element Types might still be created
            System.Diagnostics.Debug.WriteLine($"Error in CreateAllPartialElementTypes: {ex.Message}");
        }
        
        // Check which Element Types were actually created
        var allTypes = _contentTypeService.GetAll();
        var elementTypes = allTypes.Where(ct => ct.IsElement).ToList();
        
        var expectedAliases = new[] { "hero", "gallery", "features", "faq", "cards", "ctaPanel", "events", "offers", "rooms", "richText", "testimonials", "accordion", "tabs", "map" };
        
        foreach (var alias in expectedAliases)
        {
            var elementType = elementTypes.FirstOrDefault(et => et.Alias == alias);
            if (elementType != null)
            {
                created.Add(elementType.Name);
            }
            else
            {
                failed.Add(alias);
            }
        }

        return Ok(new
        {
            success = failed.Count == 0,
            message = $"Created: {created.Count}, Failed: {failed.Count}",
            created = created,
            failed = failed
        });
    }

    [HttpGet("list-all-properties")]
    public IActionResult ListAllProperties()
    {
        try
        {
            var elementTypePropertyAliases = new[]
            {
                "heading", "tagline", "image", "backgroundColor", "textColor",
                "title", "images",
                "items", "questions", "cardItems", "eventItems", "offerItems", "roomItems", 
                "testimonialItems", "tabItems",
                "description", "buttonText", "buttonLink",
                "content",
                "latitude", "longitude", "address"
            };

            var properties = new List<object>();

            using (var scope = _scopeProvider.CreateScope())
            {
                var database = scope.Database;
                
                // Query all properties, filter by alias in C#
                var sql = @"
                    SELECT pt.id, pt.alias, pt.uniqueId, pt.contentTypeId, ct.alias as contentTypeAlias
                    FROM cmsPropertyType pt
                    LEFT JOIN cmsContentType ct ON pt.contentTypeId = ct.nodeId";
                
                var allProperties = database.Query<dynamic>(sql)
                    .Where(p => 
                    {
                        string alias = p.alias?.ToString() ?? "";
                        return elementTypePropertyAliases.Contains(alias);
                    })
                    .ToList();
                
                foreach (var prop in allProperties)
                {
                    properties.Add(new
                    {
                        id = prop.id,
                        alias = prop.alias?.ToString(),
                        uniqueId = prop.uniqueId?.ToString(),
                        contentTypeId = prop.contentTypeId,
                        contentTypeAlias = prop.contentTypeAlias?.ToString()
                    });
                }
                
                scope.Complete();
            }

            return Ok(new
            {
                count = properties.Count,
                properties = properties
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpGet("list-element-types")]
    public IActionResult ListElementTypes()
    {
        var allTypes = _contentTypeService.GetAll();
        var elementTypes = allTypes.Where(ct => ct.IsElement).Select(ct => new
        {
            alias = ct.Alias,
            name = ct.Name,
            isElement = ct.IsElement,
            propertyCount = ct.PropertyTypes.Count(),
            hasContentGroup = ct.PropertyGroups.Any(g => g.Alias == "content" || g.Name == "Content")
        }).ToList();
        
        return Ok(new
        {
            count = elementTypes.Count,
            elementTypes = elementTypes
        });
    }

    [HttpPost("cleanup-orphaned-properties")]
    public IActionResult CleanupOrphanedProperties()
    {
        try
        {
            // Get all ContentTypes that exist
            var allContentTypes = _contentTypeService.GetAll();
            var existingContentTypeIds = allContentTypes.Select(ct => ct.Id).ToHashSet();
            var usedPropertyUniqueIds = allContentTypes
                .SelectMany(ct => ct.PropertyTypes)
                .Select(pt => pt.Key)
                .Distinct()
                .ToHashSet();

            // Get property aliases that we want to create for Element Types
            var elementTypePropertyAliases = new[]
            {
                "heading", "tagline", "image", "backgroundColor", "textColor", // Hero
                "title", "images", // Gallery
                "items", // Features, FAQ, Cards, Events, Offers, Rooms, Testimonials, Accordion, Tabs
                "questions", "cardItems", "eventItems", "offerItems", "roomItems", 
                "testimonialItems", "tabItems", // Specific item properties
                "description", "buttonText", "buttonLink", // CTA Panel
                "content", // Rich Text
                "latitude", "longitude", "address" // Map
            };

            var deleted = new List<string>();
            var failed = new List<string>();
            var found = new List<string>();

            using (var scope = _scopeProvider.CreateScope())
            {
                var database = scope.Database;
                
                // Get all property types from database
                var sql = @"
                    SELECT pt.id, pt.alias, pt.uniqueId, pt.contentTypeId
                    FROM cmsPropertyType pt";
                
                var allPropertyTypes = database.Query<dynamic>(sql).ToList();

                foreach (var prop in allPropertyTypes)
                {
                    string alias = prop.alias?.ToString() ?? "";
                    string uniqueIdStr = prop.uniqueId?.ToString() ?? "";
                    int? contentTypeId = prop.contentTypeId;
                    
                    if (string.IsNullOrEmpty(uniqueIdStr) || string.IsNullOrEmpty(alias))
                        continue;
                    
                    // Only process properties that match our Element Type property aliases
                    if (!elementTypePropertyAliases.Contains(alias))
                        continue;
                        
                    var uniqueId = Guid.Parse(uniqueIdStr);
                    
                    // Check if property is orphaned:
                    // 1. Not in any current ContentType's property list, OR
                    // 2. Associated with a ContentType that no longer exists
                    bool isOrphaned = false;
                    
                    if (!usedPropertyUniqueIds.Contains(uniqueId))
                    {
                        isOrphaned = true;
                    }
                    else if (contentTypeId.HasValue && !existingContentTypeIds.Contains(contentTypeId.Value))
                    {
                        // Property is associated with a deleted ContentType
                        isOrphaned = true;
                    }
                    
                    if (isOrphaned)
                    {
                        found.Add($"{alias} ({uniqueIdStr})");
                        
                        try
                        {
                            // Delete related data first, then the property type
                            database.Execute(@"
                                DELETE FROM cmsPropertyType2PropertyTypeGroup WHERE propertyTypeId = @id;
                                DELETE FROM cmsPropertyType WHERE id = @id",
                                new { id = prop.id });
                            
                            deleted.Add(alias);
                        }
                        catch (Exception ex)
                        {
                            failed.Add($"{alias}: {ex.Message}");
                        }
                    }
                }
                
                scope.Complete();
            }

            return Ok(new
            {
                success = failed.Count == 0,
                message = $"Found {found.Count} orphaned properties. Deleted: {deleted.Count}, Failed: {failed.Count}",
                found = found,
                deleted = deleted,
                failed = failed
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }
    
    [HttpPost("delete-all-properties-by-alias")]
    public IActionResult DeleteAllPropertiesByAlias()
    {
        try
        {
            var elementTypePropertyAliases = new[]
            {
                "heading", "tagline", "image", "backgroundColor", "textColor",
                "title", "images",
                "items", "questions", "cardItems", "eventItems", "offerItems", "roomItems", 
                "testimonialItems", "tabItems",
                "description", "buttonText", "buttonLink",
                "content",
                "latitude", "longitude", "address"
            };

            var deleted = new List<string>();
            var failed = new List<string>();

            using (var scope = _scopeProvider.CreateScope())
            {
                var database = scope.Database;
                
                // Get all ContentTypes to check which properties are in use
                var allContentTypes = _contentTypeService.GetAll();
                var usedPropertyIds = allContentTypes
                    .SelectMany(ct => ct.PropertyTypes)
                    .Select(pt => pt.Id)
                    .ToHashSet();
                
                foreach (var alias in elementTypePropertyAliases)
                {
                    try
                    {
                        // Get properties with this alias
                        var properties = database.Query<dynamic>(
                            "SELECT id, uniqueId FROM cmsPropertyType WHERE alias = @alias",
                            new { alias = alias }).ToList();
                        
                        foreach (var prop in properties)
                        {
                            int propId = prop.id;
                            
                            // Only delete if not in use by any ContentType
                            if (!usedPropertyIds.Contains(propId))
                            {
                                // Delete related data first
                                database.Execute(@"
                                    DELETE FROM cmsPropertyType2PropertyTypeGroup WHERE propertyTypeId = @id;
                                    DELETE FROM cmsPropertyType WHERE id = @id",
                                    new { id = propId });
                                
                                deleted.Add($"{alias} (id: {propId})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{alias}: {ex.Message}");
                    }
                }
                
                scope.Complete();
            }

            return Ok(new
            {
                success = failed.Count == 0,
                message = $"Deleted properties by alias. Deleted: {deleted.Count}, Failed: {failed.Count}",
                deleted = deleted,
                failed = failed
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }
    
    [HttpPost("delete-properties-by-uniqueid")]
    public IActionResult DeletePropertiesByUniqueId([FromBody] string[] uniqueIds)
    {
        try
        {
            var deleted = new List<string>();
            var failed = new List<string>();
            var notFound = new List<string>();

            using (var scope = _scopeProvider.CreateScope())
            {
                var database = scope.Database;
                
                // Get all ContentTypes to check which properties are in use
                var allContentTypes = _contentTypeService.GetAll();
                var usedPropertyIds = allContentTypes
                    .SelectMany(ct => ct.PropertyTypes)
                    .Select(pt => pt.Id)
                    .ToHashSet();
                
                foreach (var uniqueIdStr in uniqueIds)
                {
                    try
                    {
                        var uniqueId = Guid.Parse(uniqueIdStr);
                        
                        // Get property ID by uniqueId
                        var prop = database.Query<dynamic>(
                            "SELECT id, alias FROM cmsPropertyType WHERE uniqueId = @uniqueId",
                            new { uniqueId = uniqueId }).FirstOrDefault();
                        
                        if (prop == null)
                        {
                            notFound.Add(uniqueIdStr);
                            continue;
                        }
                        
                        int propId = prop.id;
                        string alias = prop.alias?.ToString() ?? "";
                        
                        // Only delete if not in use by any ContentType
                        if (!usedPropertyIds.Contains(propId))
                        {
                            // Delete related data first
                            database.Execute(@"
                                DELETE FROM cmsPropertyType2PropertyTypeGroup WHERE propertyTypeId = @id;
                                DELETE FROM cmsPropertyType WHERE id = @id",
                                new { id = propId });
                            
                            deleted.Add($"{alias} ({uniqueIdStr})");
                        }
                        else
                        {
                            failed.Add($"{alias} ({uniqueIdStr}): Property is in use by a ContentType");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{uniqueIdStr}: {ex.Message}");
                    }
                }
                
                scope.Complete();
            }

            return Ok(new
            {
                success = failed.Count == 0,
                message = $"Deleted: {deleted.Count}, Failed: {failed.Count}, Not Found: {notFound.Count}",
                deleted = deleted,
                failed = failed,
                notFound = notFound
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("reset-all-element-types")]
    public IActionResult ResetAllElementTypes()
    {
        try
        {
            var deleted = new List<string>();
            var failed = new List<string>();
            var propertiesDeleted = new List<string>();
            
            // Step 1: Delete all Element Types
            var allTypes = _contentTypeService.GetAll();
            var elementTypes = allTypes.Where(ct => ct.IsElement).ToList();
            
            foreach (var elementType in elementTypes)
            {
                try
                {
                    _contentTypeService.Delete(elementType);
                    deleted.Add($"{elementType.Alias} ({elementType.Name})");
                }
                catch (Exception ex)
                {
                    failed.Add($"{elementType.Alias}: {ex.Message}");
                }
            }
            
            // Step 2: Aggressively delete ALL properties with Element Type aliases from database
            var elementTypePropertyAliases = new[]
            {
                "heading", "tagline", "image", "backgroundColor", "textColor",
                "title", "images",
                "items", "questions", "cardItems", "eventItems", "offerItems", "roomItems", 
                "testimonialItems", "tabItems",
                "description", "buttonText", "buttonLink",
                "content",
                "latitude", "longitude", "address"
            };
            
            using (var scope = _scopeProvider.CreateScope())
            {
                var database = scope.Database;
                
                foreach (var alias in elementTypePropertyAliases)
                {
                    try
                    {
                        // Get all properties with this alias
                        var properties = database.Query<dynamic>(
                            "SELECT id, alias, uniqueId FROM cmsPropertyType WHERE alias = @alias",
                            new { alias = alias }).ToList();
                        
                        foreach (var prop in properties)
                        {
                            int propId = prop.id;
                            string propAlias = prop.alias?.ToString() ?? "";
                            
                            // Delete related data first
                            database.Execute(@"
                                DELETE FROM cmsPropertyType2PropertyTypeGroup WHERE propertyTypeId = @id;
                                DELETE FROM cmsPropertyType WHERE id = @id",
                                new { id = propId });
                            
                            propertiesDeleted.Add($"{propAlias} (id: {propId})");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Continue even if some fail
                    }
                }
                
                scope.Complete();
            }
            
            return Ok(new
            {
                success = failed.Count == 0,
                message = $"Reset complete. Deleted Element Types: {deleted.Count}, Deleted Properties: {propertiesDeleted.Count}, Failed: {failed.Count}",
                deletedElementTypes = deleted,
                deletedProperties = propertiesDeleted,
                failed = failed
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("delete-all-element-types")]
    public IActionResult DeleteAllElementTypes()
    {
        var deleted = new List<string>();
        var failed = new List<string>();
        
        // Get all Element Types (not just by alias - they might have different aliases)
        var allTypes = _contentTypeService.GetAll();
        var elementTypes = allTypes.Where(ct => ct.IsElement).ToList();
        
        foreach (var elementType in elementTypes)
        {
            try
            {
                _contentTypeService.Delete(elementType);
                deleted.Add($"{elementType.Alias} ({elementType.Name})");
            }
            catch (Exception ex)
            {
                failed.Add($"{elementType.Alias}: {ex.Message}");
            }
        }
        
        return Ok(new
        {
            success = failed.Count == 0,
            message = $"Deleted: {deleted.Count}, Failed: {failed.Count}",
            deleted = deleted,
            failed = failed
        });
    }

    [HttpPost("recreate-partial-element-types")]
    public IActionResult RecreatePartialElementTypes()
    {
        try
        {
            // Step 1: Delete all existing Element Types
            var elementTypeAliases = new[] { "hero", "gallery", "features", "faq", "cards", "ctaPanel", "events", "offers", "rooms", "richText", "testimonials", "accordion", "tabs", "map" };
            var deleted = new List<string>();
            var failed = new List<string>();
            
            foreach (var alias in elementTypeAliases)
            {
                try
                {
                    var existing = _contentTypeService.Get(alias);
                    if (existing != null)
                    {
                        _contentTypeService.Delete(existing);
                        deleted.Add(alias);
                    }
                }
                catch (Exception ex)
                {
                    failed.Add($"{alias}: {ex.Message}");
                }
            }

            // Wait for deletion to complete
            System.Threading.Thread.Sleep(3000);

            // Step 2: Create them all fresh
            var partialService = new Services.PartialElementTypeService(
                _contentTypeService,
                _dataTypeService,
                _shortStringHelper);
            
            var created = new List<string>();
            var createFailed = new List<string>();
            
            var elementTypes = new (string name, Action createAction)[]
            {
                ("Hero", () => partialService.CreateHeroElementType()),
                ("Gallery", () => partialService.CreateGalleryElementType()),
                ("Features", () => partialService.CreateFeaturesElementType()),
                ("FAQ", () => partialService.CreateFAQElementType()),
                ("Cards", () => partialService.CreateCardsElementType()),
                ("CTA Panel", () => partialService.CreateCTAPanelElementType()),
                ("Events", () => partialService.CreateEventsElementType()),
                ("Offers", () => partialService.CreateOffersElementType()),
                ("Rooms", () => partialService.CreateRoomsElementType()),
                ("Rich Text", () => partialService.CreateRichTextElementType()),
                ("Testimonials", () => partialService.CreateTestimonialsElementType()),
                ("Accordion", () => partialService.CreateAccordionElementType()),
                ("Tabs", () => partialService.CreateTabsElementType()),
                ("Map", () => partialService.CreateMapElementType())
            };

            foreach (var item in elementTypes)
            {
                try
                {
                    item.createAction();
                    created.Add(item.name);
                }
                catch (Exception ex)
                {
                    createFailed.Add($"{item.name}: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = createFailed.Count == 0,
                message = $"Recreated Element Types. Deleted: {deleted.Count}, Created: {created.Count}, Failed: {createFailed.Count}",
                deleted = deleted,
                created = created,
                createFailed = createFailed,
                deleteFailed = failed
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }

    [HttpPost("create-home-template")]
    public IActionResult CreateHomeTemplate()
    {
        try
        {
            // Check if template already exists - if it does, delete it first to recreate
            var existingTemplate = _fileService.GetTemplate("home");
            if (existingTemplate != null)
            {
                try
                {
                    _fileService.DeleteTemplate("home");
                }
                catch
                {
                    // Ignore errors - will try to create anyway
                }
            }

            // Read the template file content from Views/home.cshtml
            // Try multiple possible paths
            var possiblePaths = new[]
            {
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Views", "home.cshtml"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "Views", "home.cshtml"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Views", "home.cshtml"),
                "/app/Views/home.cshtml",
                "Views/home.cshtml"
            };

            string templateContent = null;
            foreach (var templatePath in possiblePaths)
            {
                if (System.IO.File.Exists(templatePath))
                {
                    templateContent = System.IO.File.ReadAllText(templatePath);
                    break;
                }
            }

            // If still not found, use the full template content from Windows version
            if (string.IsNullOrEmpty(templateContent))
            {
                templateContent = @"@{
	Layout = ""undefined.cshtml"";
}
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage<Umbraco.Cms.Web.Common.PublishedModels.Home>

@{
    // Get layout from content property or default to Main
    var layoutFromContent = Model?.Value<string>(""layout"");
    var layoutName = !string.IsNullOrEmpty(layoutFromContent) ? layoutFromContent : ""Main"";
    
    // Map layout name to layout path
    var layoutPath = layoutName switch
    {
        ""HolyGrail"" => ""Layouts/HolyGrail.cshtml"",
        ""Sidebar"" => ""Layouts/Sidebar.cshtml"",
        ""Centered"" => ""Layouts/Centered.cshtml"",
        ""FullWidth"" => ""Layouts/FullWidth.cshtml"",
        _ => ""Layouts/Main.cshtml""
    };
    
    Layout = layoutPath;
    ViewData[""Title""] = Model.Name ?? ""Home"";
}

@{
    // Hero section - use Umbraco properties if they exist, otherwise defaults
    ViewData[""HeroHeading""] = Model.Value<string>(""heroHeading"") ?? ""Discover Luxury Hotels Worldwide"";
    ViewData[""HeroTagline""] = Model.Value<string>(""heroTagline"") ?? ""Experience world-class accommodations in the most beautiful destinations. Your perfect stay awaits."";
    ViewData[""HeroImage""] = Model.Value<Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent>(""heroImage"")?.Url() ?? """";
    ViewData[""HeroBackgroundColor""] = ""bg-gradient-to-br from-blue-600 via-purple-600 to-indigo-800"";
    ViewData[""HeroTextColor""] = ""text-white"";
    ViewData[""HeroButtonText""] = ""Explore Hotels"";
    ViewData[""HeroButtonLink""] = ""/hotels"";
}
@await Html.PartialAsync(""Partials/Hero"", (object)null)

@{
    // Features section - enhanced with more items
    var featuresTitle = Model.Value<string>(""featuresTitle"") ?? ""Why Choose Us"";
    ViewData[""FeaturesTitle""] = featuresTitle;
    
    ViewData[""FeaturesItems""] = new[]
    {
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6\""></path></svg>"", 
            title = ""Luxury Accommodations"", 
            description = ""World-class hotels with premium amenities and exceptional comfort"" 
        },
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z\""></path></svg>"", 
            title = ""Global Destinations"", 
            description = ""Beautiful hotels in prime locations across six continents"" 
        },
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z\""></path></svg>"", 
            title = ""Exceptional Service"", 
            description = ""Dedicated staff committed to making your stay unforgettable"" 
        },
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4\""></path></svg>"", 
            title = ""Gourmet Dining"", 
            description = ""Award-winning restaurants and world-class culinary experiences"" 
        },
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z\""></path></svg>"", 
            title = ""Spa & Wellness"", 
            description = ""Relax and rejuvenate with our premium spa facilities"" 
        },
        new { 
            icon = @""<svg class=\""w-12 h-12\"" fill=\""none\"" stroke=\""currentColor\"" viewBox=\""0 0 24 24\""><path stroke-linecap=\""round\"" stroke-linejoin=\""round\"" stroke-width=\""1.5\"" d=\""M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z\""></path></svg>"", 
            title = ""Best Price Guarantee"", 
            description = ""We match any lower price you find elsewhere"" 
        }
    };
}
@await Html.PartialAsync(""Partials/Features"", (object)null)

<!-- Our Beautiful Hotels Section - Dynamically loaded -->
<section class=""py-20 lg:py-28 bg-gray-50"">
    <div class=""max-w-7xl mx-auto px-6 lg:px-12"">
        <div class=""mb-16"">
            <h2 class=""text-4xl md:text-5xl font-serif text-gray-900 mb-4 tracking-tight"">Our Beautiful Hotels</h2>
            <p class=""text-gray-600 text-lg font-light"">Explore our beautiful properties</p>
        </div>
        <div class=""grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"" id=""hotels-gallery-container"">
            <div class=""col-span-full text-center py-12"">
                <div class=""inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-gray-400""></div>
                <p class=""text-gray-600 mt-4 text-lg font-light"">Loading hotels...</p>
            </div>
        </div>
    </div>
</section>

<script>
    // Fetch hotels and display in gallery format
    fetch('/api/hotels')
        .then(response => response.json())
        .then(hotels => {
            const container = document.getElementById('hotels-gallery-container');
            
            if (!hotels || hotels.length === 0) {
                container.innerHTML = `
                    <div class=""col-span-full text-center py-12"">
                        <p class=""text-gray-600 text-lg font-light"">No hotels available at the moment.</p>
                    </div>
                `;
                return;
            }
            
            // Collect all hotel images (heroImage and galleryImages)
            const allImages = [];
            hotels.forEach(hotel => {
                // Add hero image if available
                if (hotel.heroImage) {
                    allImages.push({
                        url: hotel.heroImage,
                        hotelName: hotel.name,
                        hotelSlug: hotel.slug || hotel.id
                    });
                }
                // Add gallery images if available
                if (hotel.galleryImages && Array.isArray(hotel.galleryImages)) {
                    hotel.galleryImages.forEach(imgUrl => {
                        allImages.push({
                            url: imgUrl,
                            hotelName: hotel.name,
                            hotelSlug: hotel.slug || hotel.id
                        });
                    });
                }
            });
            
            // Limit to 6 images for the gallery
            const displayImages = allImages.slice(0, 6);
            
            if (displayImages.length === 0) {
                container.innerHTML = `
                    <div class=""col-span-full text-center py-12"">
                        <p class=""text-gray-600 text-lg font-light"">Hotel images coming soon.</p>
                    </div>
                `;
                return;
            }
            
            container.innerHTML = displayImages.map(img => `
                <div class=""relative overflow-hidden aspect-[4/3] cursor-pointer group rounded-xl"" onclick=""if(typeof window.openLightbox === 'function') window.openLightbox('${img.url}');"">
                    <img src=""${img.url}"" alt=""${img.hotelName || 'Hotel'}"" class=""w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"" />
                    <div class=""absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-colors duration-300""></div>
                    <div class=""absolute bottom-0 left-0 right-0 p-4 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity"">
                        <p class=""text-white text-sm font-light"">${img.hotelName || 'Hotel'}</p>
                    </div>
                </div>
            `).join('');
        })
        .catch(error => {
            console.error('Error loading hotels for gallery:', error);
            document.getElementById('hotels-gallery-container').innerHTML = `
                <div class=""col-span-full text-center py-12"">
                    <p class=""text-gray-600 text-lg font-light"">Unable to load hotel images.</p>
                </div>
            `;
        });
</script>

<!-- Featured Destinations Section - Dynamically linked to hotels -->
<section class=""py-20 lg:py-28 bg-white"">
    <div class=""max-w-7xl mx-auto px-6 lg:px-12"">
        <div class=""mb-16"">
            <h2 class=""text-4xl md:text-5xl font-serif text-gray-900 mb-4 tracking-tight"">Featured Destinations</h2>
            <p class=""text-gray-600 text-lg font-light"">Explore our most sought-after locations</p>
        </div>
        <div class=""grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8"" id=""destinations-container"">
            <div class=""col-span-full text-center py-12"">
                <div class=""inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-gray-400""></div>
                <p class=""text-gray-600 mt-4 text-lg font-light"">Loading destinations...</p>
            </div>
        </div>
    </div>
</section>

<script>
    // Featured destinations with city-to-hotel mapping
    const featuredDestinations = [
        { city: ""London"", description: ""Experience British elegance in the heart of the capital"", image: ""https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=600"" },
        { city: ""Paris"", description: ""French sophistication meets modern luxury"", image: ""https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=600"" },
        { city: ""New York"", description: ""Urban luxury with panoramic city views"", image: ""https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=600"" },
        { city: ""Tokyo"", description: ""Traditional elegance meets cutting-edge design"", image: ""https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=600"" },
        { city: ""Dubai"", description: ""Ultra-modern beachfront luxury"", image: ""https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=600"" },
        { city: ""Santorini"", description: ""Breathtaking sunsets and Aegean views"", image: ""https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=600"" }
    ];
    
    // Fetch hotels and match them to destinations
    fetch('/api/hotels')
        .then(response => response.json())
        .then(hotels => {
            const container = document.getElementById('destinations-container');
            
            // Create a map of city to hotel (case-insensitive)
            const cityToHotelMap = new Map();
            hotels.forEach(hotel => {
                if (hotel.city) {
                    const cityKey = hotel.city.toLowerCase();
                    // Use hotel image if available, otherwise keep the destination image
                    if (!cityToHotelMap.has(cityKey)) {
                        cityToHotelMap.set(cityKey, {
                            hotel: hotel,
                            image: hotel.heroImage || hotel.galleryImages?.[0] || null
                        });
                    }
                }
            });
            
            // Render destinations with hotel links
            container.innerHTML = featuredDestinations.map(dest => {
                const cityKey = dest.city.toLowerCase();
                const hotelMatch = cityToHotelMap.get(cityKey);
                
                // Use hotel image if available, otherwise use destination image
                const displayImage = hotelMatch?.image || dest.image;
                const hotelLink = hotelMatch ? `/hotels/${hotelMatch.hotel.slug || hotelMatch.hotel.id}` : '/hotels';
                
                return `
                    <div class=""group"">
                        <div class=""relative overflow-hidden aspect-[4/3] mb-6 rounded-xl"">
                            <img src=""${displayImage}"" alt=""${dest.city}"" class=""w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"" />
                        </div>
                        <h3 class=""text-xl font-serif text-gray-900 mb-3 tracking-tight"">${dest.city}</h3>
                        <p class=""text-gray-600 mb-6 leading-relaxed font-light text-sm"">${dest.description}</p>
                        <a href=""${hotelLink}"" class=""text-gray-900 text-xs font-light tracking-widest uppercase hover:opacity-70 transition-opacity inline-flex items-center"">
                            Learn More
                            <svg class=""w-4 h-4 ml-2"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.5"" d=""M9 5l7 7-7 7""></path>
                            </svg>
                        </a>
                    </div>
                `;
            }).join('');
        })
        .catch(error => {
            console.error('Error loading destinations:', error);
            document.getElementById('destinations-container').innerHTML = `
                <div class=""col-span-full text-center py-12"">
                    <p class=""text-gray-600 text-lg font-light"">Unable to load destinations.</p>
                </div>
            `;
        });
</script>

@{
    // Testimonials section
    ViewData[""TestimonialsTitle""] = ""What Our Guests Say"";
    ViewData[""TestimonialsItems""] = new[]
    {
        new { quote = ""Absolutely stunning hotel with impeccable service. The staff went above and beyond to make our stay memorable."", author = ""Sarah Johnson"", role = ""Travel Blogger"" },
        new { quote = ""The best hotel experience we've ever had. The rooms were luxurious, the food was exceptional, and the location was perfect."", author = ""Michael Chen"", role = ""Business Executive"" },
        new { quote = ""A true paradise! Every detail was perfect, from the welcome drink to the breathtaking views. We'll definitely be back."", author = ""Emma Williams"", role = ""Honeymooner"" }
    };
}
<section class=""py-20 lg:py-28 bg-gray-50"">
    <div class=""max-w-7xl mx-auto px-6 lg:px-12"">
        <div class=""mb-16"">
            <h2 class=""text-4xl md:text-5xl font-serif text-gray-900 mb-4 tracking-tight"">What Our Guests Say</h2>
        </div>
        <div class=""grid grid-cols-1 md:grid-cols-3 gap-10"">
            @foreach (var testimonial in (ViewData[""TestimonialsItems""] as System.Collections.IEnumerable))
            {
                <div class=""bg-white p-8"">
                    <div class=""text-gray-400 text-sm mb-6 tracking-wider"">★★★★★</div>
                    <p class=""text-gray-700 text-base mb-8 leading-relaxed font-light italic"">""@(testimonial.GetType().GetProperty(""quote"")?.GetValue(testimonial)?.ToString() ?? """")""</p>
                    <div class=""border-t border-gray-100 pt-6"">
                        <p class=""font-serif text-gray-900 text-sm mb-1"">@(testimonial.GetType().GetProperty(""author"")?.GetValue(testimonial)?.ToString() ?? """")</p>
                        <p class=""text-gray-500 text-xs font-light uppercase tracking-wide"">@(testimonial.GetType().GetProperty(""role"")?.GetValue(testimonial)?.ToString() ?? """")</p>
                    </div>
                </div>
            }
        </div>
    </div>
</section>

@{
    // CTA Panel section
    ViewData[""CTATitle""] = ""Ready to Book Your Perfect Stay?"";
    ViewData[""CTADescription""] = ""Join thousands of satisfied guests who have experienced luxury at its finest. Book now and save up to 30% on your next stay."";
    ViewData[""CTAButtonText""] = ""Book Now"";
    ViewData[""CTAButtonLink""] = ""/hotels"";
}
<section class=""py-20 lg:py-28 bg-gray-900 text-white"">
    <div class=""max-w-4xl mx-auto px-6 lg:px-12 text-center"">
        <h2 class=""text-4xl md:text-5xl font-serif mb-6 tracking-tight"">@ViewData[""CTATitle""]</h2>
        <p class=""text-gray-300 text-lg mb-10 max-w-2xl mx-auto font-light leading-relaxed"">@ViewData[""CTADescription""]</p>
        <a href=""@ViewData[""CTAButtonLink""]"" class=""bg-white text-gray-900 px-12 py-4 text-sm font-light tracking-widest uppercase hover:bg-gray-100 transition-colors inline-block"">
            @ViewData[""CTAButtonText""]
        </a>
    </div>
</section>";
            }

            // Create the template
            var template = new Template(_shortStringHelper, "Home", "home")
            {
                Content = templateContent
            };

            // Save the template
            _fileService.SaveTemplate(template);

            // Assign template to Home document type
            try
            {
                var homeDocType = _contentTypeService.Get("home");
                if (homeDocType != null)
                {
                    // Reload template to get the actual ID after save
                    var savedTemplate = _fileService.GetTemplate("home");
                    if (savedTemplate != null)
                    {
                        // Set as default template for Home document type
                        var contentType = (Umbraco.Cms.Core.Models.ContentType)homeDocType;
                        contentType.SetDefaultTemplate(savedTemplate);
                        _contentTypeService.Save(contentType);
                    }
                }
            }
            catch (Exception assignEx)
            {
                // Log but don't fail - template is created even if assignment fails
                System.Diagnostics.Debug.WriteLine($"Could not assign template to Home document type: {assignEx.Message}");
            }

            return Ok(new
            {
                success = true,
                message = "Home template created successfully and assigned to Home document type",
                templateId = template.Id,
                alias = template.Alias
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            });
        }
    }
}

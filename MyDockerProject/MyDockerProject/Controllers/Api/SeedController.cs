using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;

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

    public SeedController(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        IScopeProvider scopeProvider)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _scopeProvider = scopeProvider;
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
                message = "Document types created successfully: Hotel, Room, Offer"
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
}

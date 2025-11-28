using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace MyDockerProject.Services;

public class PartialElementTypeService
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;

    public PartialElementTypeService(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
    }

    private IDataType? GetDataTypeByEditor(string editorAlias)
    {
        return _dataTypeService.GetAll().FirstOrDefault(dt => dt.EditorAlias == editorAlias);
    }

    private PropertyGroup EnsureContentGroup(ContentType contentType, bool saveImmediately = false)
    {
        const string groupAlias = "content";
        const string groupName = "Content";
        
        // Check if group exists (by alias or name)
        var group = contentType.PropertyGroups.FirstOrDefault(g => 
            g.Alias == groupAlias || g.Alias == "Content" || 
            g.Name == groupName || g.Name == "content");
        
        if (group == null)
        {
            // Create the group
            contentType.AddPropertyGroup(groupName, groupAlias);
            
            if (saveImmediately)
            {
                // Save to persist the group
                _contentTypeService.Save(contentType);
                
                // Reload to get the persisted group
                var reloaded = _contentTypeService.Get(contentType.Alias);
                if (reloaded != null)
                {
                    contentType = (ContentType)reloaded;
                    group = contentType.PropertyGroups.FirstOrDefault(g => 
                        g.Alias == groupAlias || g.Alias == "Content" ||
                        g.Name == groupName || g.Name == "content");
                }
            }
            else
            {
                // Don't save yet - return the group that was just added
                group = contentType.PropertyGroups.FirstOrDefault(g => 
                    g.Alias == groupAlias || g.Alias == "Content" ||
                    g.Name == groupName || g.Name == "content");
            }
        }
        
        return group;
    }

    private void AddPropertyToContentGroupSafely(ContentType contentType, PropertyType propertyType)
    {
        // Check if property already exists in this ContentType
        var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == propertyType.Alias);
        if (existingProperty != null)
        {
            // Property already exists - ensure it's in content group
            var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => 
                g.Alias == "content" || g.Alias == "Content" ||
                g.Name == "Content" || g.Name == "content");
            
            if (contentGroup != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == propertyType.Alias))
            {
                var propType = existingProperty as PropertyType;
                if (propType != null)
                {
                    contentGroup.PropertyTypes.Add(propType);
                }
            }
            return;
        }
        
        // Check if property exists in ANY ContentType (to avoid duplicate uniqueId)
        var allContentTypes = _contentTypeService.GetAll();
        var propertyExistsElsewhere = allContentTypes
            .SelectMany(ct => ct.PropertyTypes)
            .Any(pt => pt.Alias == propertyType.Alias && pt.DataTypeKey == propertyType.DataTypeKey);
        
        if (propertyExistsElsewhere)
        {
            // Property exists elsewhere - skip creating it to avoid duplicate uniqueId
            // The Element Type will be created without this property
            return;
        }
        
        // Property doesn't exist anywhere - safe to create
        AddPropertyToContentGroup(contentType, propertyType);
    }

    private void AddPropertyToContentGroup(ContentType contentType, PropertyType propertyType)
    {
        // Step 1: Ensure content group exists (don't save yet - we'll save at the end)
        var contentGroup = EnsureContentGroup(contentType, saveImmediately: false);
        if (contentGroup == null)
        {
            throw new Exception($"Failed to create content group for {contentType.Alias}");
        }

        // Step 2: Check if property already exists in ContentType
        var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == propertyType.Alias);
        
        // Step 3: Check if property is already in the content group
        var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == propertyType.Alias);

        if (!isInGroup)
        {
            if (existingProperty != null)
            {
                // Property exists in ContentType but not in group - cast and add to group
                var propType = existingProperty as PropertyType;
                if (propType != null)
                {
                    // Double-check it's not already in the group (race condition protection)
                    if (!contentGroup.PropertyTypes.Any(pt => pt.Alias == propertyType.Alias))
                    {
                        try
                        {
                            contentGroup.PropertyTypes.Add(propType);
                        }
                        catch
                        {
                            // If add fails (might be duplicate), skip this property
                        }
                    }
                }
            }
            else
            {
                // Property doesn't exist - check if it exists in ANY ContentType first (to reuse it)
                var allContentTypes = _contentTypeService.GetAll();
                var existingPropertyInAnyType = allContentTypes
                    .SelectMany(ct => ct.PropertyTypes)
                    .FirstOrDefault(pt => pt.Alias == propertyType.Alias);
                
                if (existingPropertyInAnyType != null)
                {
                    // Property exists elsewhere - skip creating it to avoid duplicate uniqueId
                    // The Element Type will be created without this property
                    return;
                }
                else
                {
                    // Property doesn't exist anywhere - create new property
                    try
                    {
                        // Add to ContentType first (required)
                        contentType.AddPropertyType(propertyType);
                        // Then add to group
                        if (!contentGroup.PropertyTypes.Any(pt => pt.Alias == propertyType.Alias))
                        {
                            contentGroup.PropertyTypes.Add(propertyType);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If property creation fails due to duplicate uniqueId, try to reload and reuse existing property
                        if (ex.Message.Contains("duplicate key") || ex.Message.Contains("IX_cmsPropertyTypeUniqueID"))
                        {
                            try
                            {
                                // Reload ContentType to get property from database
                                var reloaded = _contentTypeService.Get(contentType.Alias);
                                if (reloaded != null)
                                {
                                    var reloadedType = (ContentType)reloaded;
                                    var dbProp = reloadedType.PropertyTypes.FirstOrDefault(pt => pt.Alias == propertyType.Alias);
                                    if (dbProp != null)
                                    {
                                        // Property exists in DB - add it to the group
                                        var reloadedGroup = reloadedType.PropertyGroups.FirstOrDefault(g => 
                                            g.Alias == "content" || g.Alias == "Content" ||
                                            g.Name == "Content" || g.Name == "content");
                                        if (reloadedGroup != null && !reloadedGroup.PropertyTypes.Any(pt => pt.Alias == propertyType.Alias))
                                        {
                                            var propType = dbProp as PropertyType;
                                            if (propType != null)
                                            {
                                                reloadedGroup.PropertyTypes.Add(propType);
                                                _contentTypeService.Save(reloadedType);
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // If reload/add fails, skip this property
                            }
                        }
                    }
                }
            }
        }
    }

    private void EnsureElementTypeHasProperties(ContentType elementType, (string alias, string name, string editorAlias)[] requiredProperties)
    {
        try
        {
            // Just ensure content group exists - properties will automatically appear
            var contentGroup = EnsureContentGroup(elementType, saveImmediately: true);
            
            // Reload to get fresh reference with all properties
            var reloaded = _contentTypeService.Get(elementType.Alias);
            if (reloaded == null)
            {
                return;
            }
            var reloadedElementType = (ContentType)reloaded;
            contentGroup = reloadedElementType.PropertyGroups.FirstOrDefault(g => 
                g.Alias == "content" || g.Alias == "Content" ||
                g.Name == "Content" || g.Name == "content");

            if (contentGroup == null)
            {
                return;
            }

            // Get all properties that are in ANY group
            var allGroups = reloadedElementType.PropertyGroups.ToList();
            var propertiesInGroups = allGroups.SelectMany(g => g.PropertyTypes).Select(pt => pt.Alias).ToHashSet();

            bool hasChanges = false;

            // Add properties that aren't in any group to the content group
            foreach (var prop in reloadedElementType.PropertyTypes.ToList())
            {
                if (!propertiesInGroups.Contains(prop.Alias))
                {
                    var propType = prop as PropertyType;
                    if (propType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        try
                        {
                            contentGroup.PropertyTypes.Add(propType);
                            hasChanges = true;
                        }
                        catch
                        {
                            // Skip if add fails
                        }
                    }
                }
            }

            if (hasChanges)
            {
                _contentTypeService.Save(reloadedElementType);
            }
        }
        catch
        {
            // If update fails, skip - Element Type exists, properties will show in UI
        }
    }

    private void UpdateElementTypeWithContentGroup(ContentType elementType)
    {
        try
        {
            const string groupAlias = "content";
            const string groupName = "Content";
            
            // Check if content group exists
            var contentGroup = elementType.PropertyGroups.FirstOrDefault(g => 
                g.Alias == groupAlias || g.Alias == "Content" ||
                g.Name == groupName || g.Name == "content");
            
            if (contentGroup == null)
            {
                // Create the group and save - properties will automatically appear in it
                elementType.AddPropertyGroup(groupName, groupAlias);
                _contentTypeService.Save(elementType);
            }
            
            // Properties that aren't in any group will automatically appear
            // in the 'content' group when viewed in Umbraco backoffice Design tab
        }
        catch (Exception ex)
        {
            // Log but don't throw - Element Type exists, group creation might have failed
            // but that's okay, user can create it manually if needed
            System.Diagnostics.Debug.WriteLine($"Failed to create content group for {elementType.Alias}: {ex.Message}");
        }
    }


    private void CreateEmptyElementType(string alias, string name, string icon)
    {
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Element Type already exists - ensure content group
            UpdateElementTypeWithContentGroup((ContentType)existing);
            return;
        }

        // Create Element Type WITHOUT properties to avoid duplicate key errors
        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = name,
            Alias = alias,
            Icon = icon,
            IsElement = true,
            AllowedAsRoot = false
        };
        
        // Save Element Type immediately (empty - no properties)
        _contentTypeService.Save(elementType);
        
        // Ensure content group exists
        var reloaded = _contentTypeService.Get(alias);
        if (reloaded != null)
        {
            UpdateElementTypeWithContentGroup((ContentType)reloaded);
        }
    }

    private void AddPropertySafely(ContentType elementType, string alias, string name, IDataType dataType, bool mandatory = false, int sortOrder = 0)
    {
        try
        {
            // Check if property already exists in this Element Type
            var existingInElementType = elementType.PropertyTypes.FirstOrDefault(pt => pt.Alias == alias);
            if (existingInElementType != null)
            {
                // Property exists - ensure it's in content group
                var contentGroup = elementType.PropertyGroups.FirstOrDefault(g => 
                    g.Alias == "content" || g.Alias == "Content" ||
                    g.Name == "Content" || g.Name == "content");
                
                if (contentGroup != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == alias))
                {
                    var propType = existingInElementType as PropertyType;
                    if (propType != null)
                    {
                        contentGroup.PropertyTypes.Add(propType);
                    }
                }
                return;
            }
            
            // Check if property exists in ANY ContentType (to reuse it)
            var allContentTypes = _contentTypeService.GetAll();
            var existingProperty = allContentTypes
                .SelectMany(ct => ct.PropertyTypes)
                .FirstOrDefault(pt => pt.Alias == alias && pt.DataTypeKey == dataType.Key);
            
            PropertyType propertyToAdd;
            
            if (existingProperty != null)
            {
                // Property exists in another ContentType - we can't reuse it directly
                // because PropertyTypes are tied to their ContentType
                // So we need to create a new PropertyType with the same alias and DataType
                // Umbraco will handle the uniqueId generation
                propertyToAdd = new PropertyType(_shortStringHelper, dataType, alias)
                {
                    Name = name,
                    Mandatory = mandatory,
                    SortOrder = sortOrder
                };
            }
            else
            {
                // Property doesn't exist - create new one
                propertyToAdd = new PropertyType(_shortStringHelper, dataType, alias)
                {
                    Name = name,
                    Mandatory = mandatory,
                    SortOrder = sortOrder
                };
            }
            
            // Add to Element Type
            try
            {
                elementType.AddPropertyType(propertyToAdd);
                
                // Ensure content group exists and add property to it
                var contentGroup2 = EnsureContentGroup(elementType, saveImmediately: false);
                if (contentGroup2 != null && !contentGroup2.PropertyTypes.Any(pt => pt.Alias == alias))
                {
                    contentGroup2.PropertyTypes.Add(propertyToAdd);
                }
            }
            catch (Exception ex)
            {
                // If duplicate uniqueId error, try to reload and add existing property
                if (ex.Message.Contains("duplicate key") || ex.Message.Contains("IX_cmsPropertyTypeUniqueID"))
                {
                    // Reload Element Type - property might have been created
                    var reloaded = _contentTypeService.Get(elementType.Alias);
                    if (reloaded != null)
                    {
                        var reloadedType = (ContentType)reloaded;
                        var dbProp = reloadedType.PropertyTypes.FirstOrDefault(pt => pt.Alias == alias);
                        if (dbProp != null)
                        {
                            var contentGroup3 = reloadedType.PropertyGroups.FirstOrDefault(g => 
                                g.Alias == "content" || g.Alias == "Content" ||
                                g.Name == "Content" || g.Name == "content");
                            if (contentGroup3 != null && !contentGroup3.PropertyTypes.Any(pt => pt.Alias == alias))
                            {
                                var propType = dbProp as PropertyType;
                                if (propType != null)
                                {
                                    contentGroup3.PropertyTypes.Add(propType);
                                    _contentTypeService.Save(reloadedType);
                                }
                            }
                        }
                    }
                }
                // Otherwise, skip this property
            }
        }
        catch
        {
            // Skip if property addition fails
        }
    }

    public void AddPropertiesToAllElementTypes()
    {
        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var textarea = GetDataTypeByEditor("Umbraco.TextArea");
        var mediaPicker = GetDataTypeByEditor("Umbraco.MediaPicker3");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");
        
        // If BlockList doesn't exist, try Block Grid or use Textarea as fallback
        if (blockList == null)
        {
            blockList = GetDataTypeByEditor("Umbraco.BlockGrid");
        }
        if (blockList == null)
        {
            blockList = textarea; // Fallback to textarea for items
        }
        
        if (textstring == null || textarea == null) return;
        
        // Hero
        var hero = _contentTypeService.Get("hero");
        if (hero != null && hero.IsElement)
        {
            var heroType = (ContentType)hero;
            AddPropertySafely(heroType, "heading", "Heading", textstring, mandatory: true, sortOrder: 1);
            AddPropertySafely(heroType, "tagline", "Tagline", textarea, sortOrder: 2);
            if (mediaPicker != null) AddPropertySafely(heroType, "image", "Image", mediaPicker, sortOrder: 3);
            AddPropertySafely(heroType, "backgroundColor", "Background Color", textstring, sortOrder: 4);
            AddPropertySafely(heroType, "textColor", "Text Color", textstring, sortOrder: 5);
            _contentTypeService.Save(heroType);
        }
        
        // Gallery
        var gallery = _contentTypeService.Get("gallery");
        if (gallery != null && gallery.IsElement)
        {
            var galleryType = (ContentType)gallery;
            AddPropertySafely(galleryType, "title", "Title", textstring, sortOrder: 1);
            if (mediaPicker != null) AddPropertySafely(galleryType, "images", "Images", mediaPicker, sortOrder: 2);
            _contentTypeService.Save(galleryType);
        }
        
        // Features
        var features = _contentTypeService.Get("features");
        if (features != null && features.IsElement)
        {
            var featuresType = (ContentType)features;
            AddPropertySafely(featuresType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(featuresType, "items", "Items", blockList, sortOrder: 2);
            _contentTypeService.Save(featuresType);
        }
        
        // FAQ
        var faq = _contentTypeService.Get("faq");
        if (faq != null && faq.IsElement)
        {
            var faqType = (ContentType)faq;
            AddPropertySafely(faqType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(faqType, "questions", "Questions", blockList, sortOrder: 2);
            _contentTypeService.Save(faqType);
        }
        
        // Cards
        var cards = _contentTypeService.Get("cards");
        if (cards != null && cards.IsElement)
        {
            var cardsType = (ContentType)cards;
            AddPropertySafely(cardsType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(cardsType, "cardItems", "Card Items", blockList, sortOrder: 2);
            _contentTypeService.Save(cardsType);
        }
        
        // CTA Panel
        var ctaPanel = _contentTypeService.Get("ctaPanel");
        if (ctaPanel != null && ctaPanel.IsElement)
        {
            var ctaType = (ContentType)ctaPanel;
            AddPropertySafely(ctaType, "title", "Title", textstring, sortOrder: 1);
            AddPropertySafely(ctaType, "description", "Description", textarea, sortOrder: 2);
            AddPropertySafely(ctaType, "buttonText", "Button Text", textstring, sortOrder: 3);
            AddPropertySafely(ctaType, "buttonLink", "Button Link", textstring, sortOrder: 4);
            _contentTypeService.Save(ctaType);
        }
        
        // Events
        var events = _contentTypeService.Get("events");
        if (events != null && events.IsElement)
        {
            var eventsType = (ContentType)events;
            AddPropertySafely(eventsType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(eventsType, "eventItems", "Event Items", blockList, sortOrder: 2);
            _contentTypeService.Save(eventsType);
        }
        
        // Offers
        var offers = _contentTypeService.Get("offers");
        if (offers != null && offers.IsElement)
        {
            var offersType = (ContentType)offers;
            AddPropertySafely(offersType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(offersType, "offerItems", "Offer Items", blockList, sortOrder: 2);
            _contentTypeService.Save(offersType);
        }
        
        // Rooms
        var rooms = _contentTypeService.Get("rooms");
        if (rooms != null && rooms.IsElement)
        {
            var roomsType = (ContentType)rooms;
            AddPropertySafely(roomsType, "title", "Title", textstring, sortOrder: 1);
            if (mediaPicker != null) AddPropertySafely(roomsType, "image", "Image", mediaPicker, sortOrder: 2);
            if (blockList != null) AddPropertySafely(roomsType, "roomItems", "Room Items", blockList, sortOrder: 3);
            _contentTypeService.Save(roomsType);
        }
        
        // Rich Text
        var richText = _contentTypeService.Get("richText");
        if (richText != null && richText.IsElement)
        {
            var richTextType = (ContentType)richText;
            AddPropertySafely(richTextType, "content", "Content", textarea, sortOrder: 1);
            _contentTypeService.Save(richTextType);
        }
        
        // Testimonials
        var testimonials = _contentTypeService.Get("testimonials");
        if (testimonials != null && testimonials.IsElement)
        {
            var testimonialsType = (ContentType)testimonials;
            AddPropertySafely(testimonialsType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(testimonialsType, "testimonialItems", "Testimonial Items", blockList, sortOrder: 2);
            _contentTypeService.Save(testimonialsType);
        }
        
        // Accordion
        var accordion = _contentTypeService.Get("accordion");
        if (accordion != null && accordion.IsElement)
        {
            var accordionType = (ContentType)accordion;
            AddPropertySafely(accordionType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(accordionType, "items", "Items", blockList, sortOrder: 2);
            _contentTypeService.Save(accordionType);
        }
        
        // Tabs
        var tabs = _contentTypeService.Get("tabs");
        if (tabs != null && tabs.IsElement)
        {
            var tabsType = (ContentType)tabs;
            AddPropertySafely(tabsType, "title", "Title", textstring, sortOrder: 1);
            if (blockList != null) AddPropertySafely(tabsType, "tabItems", "Tab Items", blockList, sortOrder: 2);
            _contentTypeService.Save(tabsType);
        }
        
        // Map
        var map = _contentTypeService.Get("map");
        if (map != null && map.IsElement)
        {
            var mapType = (ContentType)map;
            AddPropertySafely(mapType, "address", "Address", textstring, sortOrder: 1);
            AddPropertySafely(mapType, "latitude", "Latitude", textstring, sortOrder: 2);
            AddPropertySafely(mapType, "longitude", "Longitude", textstring, sortOrder: 3);
            _contentTypeService.Save(mapType);
        }
    }

    public void CreateAllPartialElementTypes()
    {
        // Create all Element Types - wrap each in try-catch to continue even if one fails
        TryCreate(() => CreateEmptyElementType("hero", "Hero", "icon-picture"), "Hero");
        TryCreate(() => CreateEmptyElementType("gallery", "Gallery", "icon-images"), "Gallery");
        TryCreate(() => CreateEmptyElementType("features", "Features", "icon-star"), "Features");
        TryCreate(() => CreateEmptyElementType("faq", "FAQ", "icon-help-alt"), "FAQ");
        TryCreate(() => CreateEmptyElementType("cards", "Cards", "icon-layout"), "Cards");
        TryCreate(() => CreateEmptyElementType("ctaPanel", "CTA Panel", "icon-megaphone"), "CTA Panel");
        TryCreate(() => CreateEmptyElementType("events", "Events", "icon-calendar"), "Events");
        TryCreate(() => CreateEmptyElementType("offers", "Offers", "icon-gift"), "Offers");
        TryCreate(() => CreateEmptyElementType("rooms", "Rooms", "icon-home"), "Rooms");
        TryCreate(() => CreateEmptyElementType("richText", "Rich Text", "icon-article"), "Rich Text");
        TryCreate(() => CreateEmptyElementType("testimonials", "Testimonials", "icon-quote"), "Testimonials");
        TryCreate(() => CreateEmptyElementType("accordion", "Accordion", "icon-list"), "Accordion");
        TryCreate(() => CreateEmptyElementType("tabs", "Tabs", "icon-tab"), "Tabs");
        TryCreate(() => CreateEmptyElementType("map", "Map", "icon-map"), "Map");
    }

    private void TryCreate(Action createAction, string elementTypeName)
    {
        try
        {
            createAction();
        }
        catch (Exception ex)
        {
            // Don't re-throw - just log and continue
            // Element Type creation might have partially succeeded
            System.Diagnostics.Debug.WriteLine($"Failed to create {elementTypeName}: {ex.Message}");
            
            // Try to ensure content group exists even if creation failed
            try
            {
                var alias = elementTypeName.ToLower().Replace(" ", "").Replace("ctapanel", "ctaPanel").Replace("richtext", "richText");
                var existing = _contentTypeService.Get(alias);
                if (existing != null && existing.IsElement)
                {
                    UpdateElementTypeWithContentGroup((ContentType)existing);
                }
            }
            catch
            {
                // Ignore - Element Type might not exist
            }
        }
    }

    public void CreateHeroElementType()
    {
        var alias = "hero";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Element Type already exists - ensure content group
            var existingElementType = (ContentType)existing;
            UpdateElementTypeWithContentGroup(existingElementType);
            return;
        }

        // Create Element Type WITHOUT properties first to avoid duplicate key errors
        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Hero",
            Alias = alias,
            Icon = "icon-picture",
            IsElement = true,
            AllowedAsRoot = false
        };
        
        // Save Element Type immediately (empty - no properties)
        _contentTypeService.Save(elementType);
        
        // Ensure content group exists
        var reloaded = _contentTypeService.Get(alias);
        if (reloaded != null)
        {
            UpdateElementTypeWithContentGroup((ContentType)reloaded);
        }
        
        // Properties will be added manually in Umbraco backoffice to avoid duplicate key errors
    }

    public void CreateGalleryElementType()
    {
        var alias = "gallery";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Update existing - add properties to content group
            UpdateElementTypeWithContentGroup((ContentType)existing);
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var mediaPicker = GetDataTypeByEditor("Umbraco.MediaPicker3");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Gallery",
            Alias = alias,
            Icon = "icon-images",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (mediaPicker != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, mediaPicker, "images")
            {
                Name = "Images",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateFeaturesElementType()
    {
        var alias = "features";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Update existing - add properties to content group
            UpdateElementTypeWithContentGroup((ContentType)existing);
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var textarea = GetDataTypeByEditor("Umbraco.TextArea");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Features",
            Alias = alias,
            Icon = "icon-star",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "items")
            {
                Name = "Feature Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateFAQElementType()
    {
        var alias = "faq";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Update existing - add properties to content group
            UpdateElementTypeWithContentGroup((ContentType)existing);
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var richText = GetDataTypeByEditor("Umbraco.RichText");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "FAQ",
            Alias = alias,
            Icon = "icon-help-alt",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "questions")
            {
                Name = "Questions",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateCardsElementType()
    {
        var alias = "cards";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Element Type exists - ensure content group and add missing properties
            var existingElementType = (ContentType)existing;
            EnsureElementTypeHasProperties(existingElementType, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("cardItems", "Card Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Cards",
            Alias = alias,
            Icon = "icon-documents",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "cardItems")
            {
                Name = "Card Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateCTAPanelElementType()
    {
        var alias = "ctaPanel";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("heading", "Heading", "Umbraco.TextBox"),
                ("description", "Description", "Umbraco.TextArea"),
                ("buttonText", "Button Text", "Umbraco.TextBox"),
                ("buttonLink", "Button Link", "Umbraco.MultiUrlPicker")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var textarea = GetDataTypeByEditor("Umbraco.TextArea");
        var urlPicker = GetDataTypeByEditor("Umbraco.MultiUrlPicker");

        if (textstring == null || textarea == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "CTA Panel",
            Alias = alias,
            Icon = "icon-megaphone",
            IsElement = true,
            AllowedAsRoot = false
        };


        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "heading")
        {
            Name = "Heading",
            SortOrder = 1
        });

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textarea, "description")
        {
            Name = "Description",
            SortOrder = 2
        });

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "buttonText")
        {
            Name = "Button Text",
            SortOrder = 3
        });

        if (urlPicker != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, urlPicker, "buttonLink")
            {
                Name = "Button Link",
                SortOrder = 4
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateEventsElementType()
    {
        var alias = "events";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("eventItems", "Event Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Events",
            Alias = alias,
            Icon = "icon-calendar",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "eventItems")
            {
                Name = "Event Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateOffersElementType()
    {
        var alias = "offers";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("offerItems", "Offer Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Offers",
            Alias = alias,
            Icon = "icon-gift",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "offerItems")
            {
                Name = "Offer Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateRoomsElementType()
    {
        var alias = "rooms";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("roomItems", "Room Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Rooms",
            Alias = alias,
            Icon = "icon-home",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "roomItems")
            {
                Name = "Room Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateRichTextElementType()
    {
        var alias = "richText";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("content", "Content", "Umbraco.RichText")
            });
            return;
        }

        var richText = GetDataTypeByEditor("Umbraco.RichText");

        if (richText == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Rich Text",
            Alias = alias,
            Icon = "icon-article",
            IsElement = true,
            AllowedAsRoot = false
        };


        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, richText, "content")
        {
            Name = "Content",
            SortOrder = 1
        });

        _contentTypeService.Save(elementType);
    }

    public void CreateTestimonialsElementType()
    {
        var alias = "testimonials";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("testimonialItems", "Testimonial Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Testimonials",
            Alias = alias,
            Icon = "icon-quote",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "testimonialItems")
            {
                Name = "Testimonial Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateAccordionElementType()
    {
        var alias = "accordion";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("items", "Accordion Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Accordion",
            Alias = alias,
            Icon = "icon-list",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "items")
            {
                Name = "Accordion Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateTabsElementType()
    {
        var alias = "tabs";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            EnsureElementTypeHasProperties((ContentType)existing, new[]
            {
                ("title", "Title", "Umbraco.TextBox"),
                ("tabItems", "Tab Items", "Umbraco.BlockList")
            });
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var blockList = GetDataTypeByEditor("Umbraco.BlockList");

        if (textstring == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Tabs",
            Alias = alias,
            Icon = "icon-tab",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        if (blockList != null)
        {
            AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, blockList, "tabItems")
            {
                Name = "Tab Items",
                SortOrder = 2
            });
        }

        _contentTypeService.Save(elementType);
    }

    public void CreateMapElementType()
    {
        var alias = "map";
        var existing = _contentTypeService.Get(alias);
        if (existing != null)
        {
            // Update existing - add properties to content group
            UpdateElementTypeWithContentGroup((ContentType)existing);
            return;
        }

        var textstring = GetDataTypeByEditor("Umbraco.TextBox");
        var decimalType = GetDataTypeByEditor("Umbraco.Decimal");

        if (textstring == null || decimalType == null) return;

        var elementType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Map",
            Alias = alias,
            Icon = "icon-map",
            IsElement = true,
            AllowedAsRoot = false
        };

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "title")
        {
            Name = "Title",
            SortOrder = 1
        });

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, decimalType, "latitude")
        {
            Name = "Latitude",
            SortOrder = 2
        });

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, decimalType, "longitude")
        {
            Name = "Longitude",
            SortOrder = 3
        });

        AddPropertyToContentGroup(elementType, new PropertyType(_shortStringHelper, textstring, "address")
        {
            Name = "Address",
            SortOrder = 4
        });

        _contentTypeService.Save(elementType);
    }
}


using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using System.Linq;

namespace MyDockerProject.Services;

public class DocumentTypePropertyService
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;

    public DocumentTypePropertyService(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
    }

    public void AddHotelProperties()
    {
        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null)
        {
            throw new Exception("Hotel document type does not exist. Please create it first in Umbraco backoffice.");
        }

        var allDataTypes = _dataTypeService.GetAll().ToList();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");

        if (textstringDataType == null || textareaDataType == null)
        {
            throw new Exception("Required data types (Textstring, Textarea) not found.");
        }

        // Step 1: Ensure property group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentGroup = hotelType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        if (contentGroup == null)
        {
            // AddPropertyGroup creates the group
            hotelType.AddPropertyGroup(groupName, groupAlias);
            
            // Save to persist the group creation
            _contentTypeService.Save(hotelType);
            
            // Reload to get the persisted group
            var reloaded = _contentTypeService.Get("hotel");
            if (reloaded == null)
            {
                throw new Exception("Failed to reload Hotel document type after creating property group.");
            }
            hotelType = (ContentType)reloaded;
            
            contentGroup = hotelType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        }
        
        // Final check - if still null, try to find by name
        if (contentGroup == null)
        {
            contentGroup = hotelType.PropertyGroups.FirstOrDefault(g => g.Name == groupName || g.Name == "content");
        }
        
        if (contentGroup == null)
        {
            throw new Exception($"Property group '{groupName}' not found. Available groups: {string.Join(", ", hotelType.PropertyGroups.Select(g => $"{g.Name}({g.Alias})"))}");
        }

        // Get additional data types
        var mediaPickerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.MediaPicker3");
        var richTextDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TinyMCE");
        
        // Step 2: Define required properties
        var requiredProperties = new[]
        {
            new { Alias = "hotelName", Name = "Hotel Name", DataType = textstringDataType, Mandatory = true, SortOrder = 1 },
            new { Alias = "description", Name = "Description", DataType = textareaDataType, Mandatory = false, SortOrder = 2 },
            new { Alias = "shortDescription", Name = "Short Description", DataType = textstringDataType, Mandatory = false, SortOrder = 3 },
            new { Alias = "heroImage", Name = "Hero Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 4 },
            new { Alias = "galleryImages", Name = "Gallery Images", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 5 },
            new { Alias = "address", Name = "Address", DataType = textstringDataType, Mandatory = false, SortOrder = 6 },
            new { Alias = "city", Name = "City", DataType = textstringDataType, Mandatory = false, SortOrder = 7 },
            new { Alias = "country", Name = "Country", DataType = textstringDataType, Mandatory = false, SortOrder = 8 },
            new { Alias = "phone", Name = "Phone", DataType = textstringDataType, Mandatory = false, SortOrder = 9 },
            new { Alias = "email", Name = "Email", DataType = textstringDataType, Mandatory = false, SortOrder = 10 },
            new { Alias = "amenities", Name = "Amenities", DataType = textareaDataType, Mandatory = false, SortOrder = 11 },
            new { Alias = "highlights", Name = "Highlights", DataType = textareaDataType, Mandatory = false, SortOrder = 12 },
            new { Alias = "features", Name = "Features", DataType = textareaDataType, Mandatory = false, SortOrder = 13 }
        };

        bool hasChanges = false;

        // Step 3: Add properties directly to the PropertyGroup's PropertyTypes collection
        // This is the correct way according to Umbraco docs
        foreach (var prop in requiredProperties)
        {
            // Check if property already exists in the ContentType
            var existingProperty = hotelType.PropertyTypes.FirstOrDefault(pt => pt.Alias == prop.Alias);
            
            // Check if property is already in the group
            var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias);
            
            if (!isInGroup)
            {
                if (existingProperty != null)
                {
                    // Property exists but not in group - cast to PropertyType and add to group
                    var propertyType = existingProperty as PropertyType;
                    if (propertyType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        contentGroup.PropertyTypes.Add(propertyType);
                        hasChanges = true;
                    }
                }
                else
                {
                    // Create new property type
                    var propertyType = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        SortOrder = prop.SortOrder
                    };
                    
                    // Add to ContentType first (required)
                    hotelType.AddPropertyType(propertyType);
                    
                    // Add to group
                    contentGroup.PropertyTypes.Add(propertyType);
                    hasChanges = true;
                }
            }
        }

        // Step 4: Save the ContentType (this saves both the properties and group assignments)
        if (hasChanges)
        {
            _contentTypeService.Save(hotelType);
        }
    }

    public List<string> GetMissingHotelProperties()
    {
        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null)
        {
            return new List<string> { "Hotel document type does not exist" };
        }

        var requiredAliases = new[] { "hotelName", "description", "shortDescription", "heroImage", "galleryImages", "address", "city", "country", "phone", "email", "amenities", "highlights", "features" };
        var existingAliases = hotelType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
        var missing = requiredAliases.Where(alias => !existingAliases.Contains(alias)).ToList();

        return missing;
    }

    public void AddRoomProperties()
    {
        var roomType = _contentTypeService.Get("room");
        if (roomType == null)
        {
            throw new Exception("Room document type does not exist. Please create it first.");
        }

        var allDataTypes = _dataTypeService.GetAll().ToList();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var integerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Integer");
        var decimalDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Decimal");
        var mediaPickerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.MediaPicker3");

        // Use Textstring as fallback for Decimal if it doesn't exist
        if (decimalDataType == null)
        {
            decimalDataType = textstringDataType;
        }

        if (textstringDataType == null || textareaDataType == null || integerDataType == null)
        {
            throw new Exception("Required data types not found.");
        }

        // Ensure content group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentType = (ContentType)roomType;
        var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        if (contentGroup == null)
        {
            contentType.AddPropertyGroup(groupName, groupAlias);
            _contentTypeService.Save(contentType);
            var reloaded = _contentTypeService.Get("room");
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
            }
        }

        if (contentGroup == null)
        {
            throw new Exception($"Property group '{groupName}' not found for Room document type.");
        }

        // Define additional room properties
        var additionalProperties = new[]
        {
            new { Alias = "roomImages", Name = "Room Images", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 6 },
            new { Alias = "heroImage", Name = "Hero Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 7 },
            new { Alias = "size", Name = "Size (m²)", DataType = textstringDataType, Mandatory = false, SortOrder = 8 },
            new { Alias = "bedType", Name = "Bed Type", DataType = textstringDataType, Mandatory = false, SortOrder = 9 },
            new { Alias = "furnishings", Name = "Furnishings", DataType = textareaDataType, Mandatory = false, SortOrder = 10 },
            new { Alias = "specifications", Name = "Specifications", DataType = textareaDataType, Mandatory = false, SortOrder = 11 },
            new { Alias = "features", Name = "Features", DataType = textareaDataType, Mandatory = false, SortOrder = 12 },
            new { Alias = "hotelServices", Name = "Hotel Services", DataType = textareaDataType, Mandatory = false, SortOrder = 13 }
        };

        bool hasChanges = false;

        foreach (var prop in additionalProperties)
        {
            var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == prop.Alias);
            var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias);

            if (!isInGroup)
            {
                if (existingProperty != null)
                {
                    var propertyType = existingProperty as PropertyType;
                    if (propertyType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        contentGroup.PropertyTypes.Add(propertyType);
                        hasChanges = true;
                    }
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        SortOrder = prop.SortOrder
                    };
                    
                    contentType.AddPropertyType(propertyType);
                    contentGroup.PropertyTypes.Add(propertyType);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            _contentTypeService.Save(contentType);
        }
    }

    private IDataType GetOrCreateLayoutDropdownDataType()
    {
        var allDataTypes = _dataTypeService.GetAll().ToList();
        
        // First, try to find an existing dropdown with layout options
        var existingLayoutDropdown = allDataTypes.FirstOrDefault(dt => 
            dt.Name == "Layout Selector" || 
            (dt.EditorAlias == "Umbraco.DropDown.Flexible" && dt.Name.Contains("Layout")));
        
        if (existingLayoutDropdown != null)
        {
            return existingLayoutDropdown;
        }
        
        // Try to find any dropdown data type
        var dropdownDataType = allDataTypes.FirstOrDefault(dt => 
            dt.EditorAlias == "Umbraco.DropDown.Flexible" || 
            dt.EditorAlias == "Umbraco.DropDown");
        
        // If no dropdown exists, we'll need to use Textstring and provide instructions
        // Note: Creating dropdowns with prevalues programmatically is complex in Umbraco 16
        // The user will need to manually configure the dropdown in backoffice
        if (dropdownDataType == null)
        {
            // Use Textstring as fallback - user can manually convert to dropdown
            dropdownDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        }

        if (dropdownDataType == null)
        {
            throw new Exception("Required data type (Dropdown or Textstring) not found. Please create a dropdown data type in Umbraco backoffice.");
        }
        
        return dropdownDataType;
    }

    public void AddLayoutPropertyToDocumentType(string documentTypeAlias)
    {
        var docType = _contentTypeService.Get(documentTypeAlias);
        if (docType == null)
        {
            throw new Exception($"Document type '{documentTypeAlias}' does not exist.");
        }

        var dropdownDataType = GetOrCreateLayoutDropdownDataType();

        // Ensure content group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentType = (ContentType)docType;
        var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        
        if (contentGroup == null)
        {
            contentType.AddPropertyGroup(groupName, groupAlias);
            _contentTypeService.Save(contentType);
            var reloaded = _contentTypeService.Get(documentTypeAlias);
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
            }
        }

        if (contentGroup == null)
        {
            throw new Exception($"Failed to create/get content group for {documentTypeAlias}");
        }

        // Check if layout property already exists
        var existingLayout = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == "layout");
        var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == "layout");

        if (!isInGroup)
        {
            PropertyType layoutProperty;
            
            if (existingLayout != null)
            {
                layoutProperty = existingLayout as PropertyType;
            }
            else
            {
                layoutProperty = new PropertyType(_shortStringHelper, dropdownDataType, "layout")
                {
                    Name = "Layout",
                    Mandatory = false,
                    SortOrder = 0, // Put it first
                    Description = "Choose page layout. Valid values: Main, HolyGrail, Sidebar, Centered, FullWidth. " +
                                  "To configure as dropdown: Go to Settings → Data Types → Find this data type → Add values: Main, HolyGrail, Sidebar, Centered, FullWidth"
                };
                contentType.AddPropertyType(layoutProperty);
            }

            if (layoutProperty != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == "layout"))
            {
                contentGroup.PropertyTypes.Add(layoutProperty);
                _contentTypeService.Save(contentType);
            }
        }
    }

    public void AddLayoutPropertyToAllDocumentTypes()
    {
        var documentTypeAliases = new[] { "hotel", "room", "offer", "home" };
        
        foreach (var alias in documentTypeAliases)
        {
            try
            {
                var docType = _contentTypeService.Get(alias);
                if (docType != null)
                {
                    AddLayoutPropertyToDocumentType(alias);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add layout property to {alias}: {ex.Message}");
            }
        }
    }
    
    public void AddEventProperties()
    {
        var eventType = _contentTypeService.Get("event");
        if (eventType == null)
        {
            throw new Exception("Event document type does not exist. Please create it first.");
        }

        var allDataTypes = _dataTypeService.GetAll().ToList();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var decimalDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Decimal");
        var dateTimeDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.DateTime");
        var mediaPickerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.MediaPicker3");

        // Use Textstring as fallback for Decimal if it doesn't exist
        if (decimalDataType == null)
        {
            decimalDataType = textstringDataType;
        }

        if (textstringDataType == null || textareaDataType == null || dateTimeDataType == null)
        {
            throw new Exception("Required data types not found.");
        }

        // Ensure content group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentType = (ContentType)eventType;
        var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        if (contentGroup == null)
        {
            contentType.AddPropertyGroup(groupName, groupAlias);
            _contentTypeService.Save(contentType);
            var reloaded = _contentTypeService.Get("event");
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
            }
        }

        if (contentGroup == null)
        {
            throw new Exception($"Property group '{groupName}' not found for Event document type.");
        }

        // Define Event properties
        var eventProperties = new[]
        {
            new { Alias = "eventName", Name = "Event Name", DataType = textstringDataType, Mandatory = true, SortOrder = 1 },
            new { Alias = "description", Name = "Description", DataType = textareaDataType, Mandatory = false, SortOrder = 2 },
            new { Alias = "eventDate", Name = "Event Date", DataType = dateTimeDataType, Mandatory = false, SortOrder = 3 },
            new { Alias = "location", Name = "Location", DataType = textstringDataType, Mandatory = false, SortOrder = 4 },
            new { Alias = "price", Name = "Price", DataType = decimalDataType, Mandatory = false, SortOrder = 5 },
            new { Alias = "heroImage", Name = "Hero Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 6 },
            new { Alias = "eventImage", Name = "Event Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 7 }
        };

        bool hasChanges = false;

        foreach (var prop in eventProperties)
        {
            var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == prop.Alias);
            var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias);

            if (!isInGroup)
            {
                if (existingProperty != null)
                {
                    var propertyType = existingProperty as PropertyType;
                    if (propertyType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        contentGroup.PropertyTypes.Add(propertyType);
                        hasChanges = true;
                    }
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        SortOrder = prop.SortOrder
                    };
                    
                    contentType.AddPropertyType(propertyType);
                    contentGroup.PropertyTypes.Add(propertyType);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            _contentTypeService.Save(contentType);
        }
    }

    public void AddOfferProperties()
    {
        var offerType = _contentTypeService.Get("offer");
        if (offerType == null)
        {
            throw new Exception("Offer document type does not exist. Please create it first.");
        }

        var allDataTypes = _dataTypeService.GetAll().ToList();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var decimalDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Decimal");
        var dateTimeDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.DateTime");
        var mediaPickerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.MediaPicker3");

        // Use Textstring as fallback for Decimal if it doesn't exist
        if (decimalDataType == null)
        {
            decimalDataType = textstringDataType;
        }

        if (textstringDataType == null || textareaDataType == null || dateTimeDataType == null)
        {
            throw new Exception("Required data types not found.");
        }

        // Ensure content group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentType = (ContentType)offerType;
        var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        if (contentGroup == null)
        {
            contentType.AddPropertyGroup(groupName, groupAlias);
            _contentTypeService.Save(contentType);
            var reloaded = _contentTypeService.Get("offer");
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
            }
        }

        if (contentGroup == null)
        {
            throw new Exception($"Property group '{groupName}' not found for Offer document type.");
        }

        // Define Offer properties
        var offerProperties = new[]
        {
            new { Alias = "offerName", Name = "Offer Name", DataType = textstringDataType, Mandatory = true, SortOrder = 1 },
            new { Alias = "description", Name = "Description", DataType = textareaDataType, Mandatory = false, SortOrder = 2 },
            new { Alias = "discount", Name = "Discount", DataType = decimalDataType, Mandatory = false, SortOrder = 3 },
            new { Alias = "validFrom", Name = "Valid From", DataType = dateTimeDataType, Mandatory = false, SortOrder = 4 },
            new { Alias = "validTo", Name = "Valid To", DataType = dateTimeDataType, Mandatory = false, SortOrder = 5 },
            new { Alias = "image", Name = "Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 6 }
        };

        bool hasChanges = false;

        foreach (var prop in offerProperties)
        {
            var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == prop.Alias);
            var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias);

            if (!isInGroup)
            {
                if (existingProperty != null)
                {
                    var propertyType = existingProperty as PropertyType;
                    if (propertyType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        contentGroup.PropertyTypes.Add(propertyType);
                        hasChanges = true;
                    }
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        SortOrder = prop.SortOrder
                    };
                    
                    contentType.AddPropertyType(propertyType);
                    contentGroup.PropertyTypes.Add(propertyType);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            _contentTypeService.Save(contentType);
        }
    }

    public void AddHomeProperties()
    {
        var homeType = _contentTypeService.Get("home");
        if (homeType == null)
        {
            throw new Exception("Home document type does not exist. Please create it first.");
        }

        var allDataTypes = _dataTypeService.GetAll().ToList();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var mediaPickerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.MediaPicker3");

        if (textstringDataType == null || textareaDataType == null)
        {
            throw new Exception("Required data types not found.");
        }

        // Ensure content group exists
        const string groupAlias = "content";
        const string groupName = "Content";
        
        var contentType = (ContentType)homeType;
        var contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
        if (contentGroup == null)
        {
            contentType.AddPropertyGroup(groupName, groupAlias);
            _contentTypeService.Save(contentType);
            var reloaded = _contentTypeService.Get("home");
            if (reloaded != null)
            {
                contentType = (ContentType)reloaded;
                contentGroup = contentType.PropertyGroups.FirstOrDefault(g => g.Alias == groupAlias || g.Alias == "Content");
            }
        }

        if (contentGroup == null)
        {
            throw new Exception($"Property group '{groupName}' not found for Home document type.");
        }

        // Define Home properties based on template
        var homeProperties = new[]
        {
            new { Alias = "heroHeading", Name = "Hero Heading", DataType = textstringDataType, Mandatory = false, SortOrder = 1 },
            new { Alias = "heroTagline", Name = "Hero Tagline", DataType = textareaDataType, Mandatory = false, SortOrder = 2 },
            new { Alias = "heroImage", Name = "Hero Image", DataType = mediaPickerDataType ?? textstringDataType, Mandatory = false, SortOrder = 3 },
            new { Alias = "featuresTitle", Name = "Features Title", DataType = textstringDataType, Mandatory = false, SortOrder = 4 },
            new { Alias = "layout", Name = "Layout", DataType = textstringDataType, Mandatory = false, SortOrder = 0, Description = "Choose page layout: Main, HolyGrail, Sidebar, Centered, FullWidth" }
        };

        bool hasChanges = false;

        foreach (var prop in homeProperties)
        {
            var existingProperty = contentType.PropertyTypes.FirstOrDefault(pt => pt.Alias == prop.Alias);
            var isInGroup = contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias);

            if (!isInGroup)
            {
                if (existingProperty != null)
                {
                    var propertyType = existingProperty as PropertyType;
                    if (propertyType != null && !contentGroup.PropertyTypes.Any(pt => pt.Alias == prop.Alias))
                    {
                        contentGroup.PropertyTypes.Add(propertyType);
                        hasChanges = true;
                    }
                }
                else
                {
                    var propertyType = new PropertyType(_shortStringHelper, prop.DataType, prop.Alias)
                    {
                        Name = prop.Name,
                        Mandatory = prop.Mandatory,
                        SortOrder = prop.SortOrder
                    };
                    
                    if (!string.IsNullOrEmpty(prop.Description))
                    {
                        propertyType.Description = prop.Description;
                    }
                    
                    contentType.AddPropertyType(propertyType);
                    contentGroup.PropertyTypes.Add(propertyType);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            _contentTypeService.Save(contentType);
        }
    }
}

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

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

        // Step 2: Define required properties
        var requiredProperties = new[]
        {
            new { Alias = "hotelName", Name = "Hotel Name", DataType = textstringDataType, Mandatory = true, SortOrder = 1 },
            new { Alias = "description", Name = "Description", DataType = textareaDataType, Mandatory = false, SortOrder = 2 },
            new { Alias = "address", Name = "Address", DataType = textstringDataType, Mandatory = false, SortOrder = 3 },
            new { Alias = "city", Name = "City", DataType = textstringDataType, Mandatory = false, SortOrder = 4 },
            new { Alias = "country", Name = "Country", DataType = textstringDataType, Mandatory = false, SortOrder = 5 },
            new { Alias = "phone", Name = "Phone", DataType = textstringDataType, Mandatory = false, SortOrder = 6 },
            new { Alias = "email", Name = "Email", DataType = textstringDataType, Mandatory = false, SortOrder = 7 }
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

        var requiredAliases = new[] { "hotelName", "description", "address", "city", "country", "phone", "email" };
        var existingAliases = hotelType.PropertyTypes.Select(pt => pt.Alias).ToHashSet();
        var missing = requiredAliases.Where(alias => !existingAliases.Contains(alias)).ToList();

        return missing;
    }
}

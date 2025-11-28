using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace MyDockerProject.Services;

public class DocumentTypeService
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;

    public DocumentTypeService(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
    }

    private IDataType? GetOrCreateDataType(string editorAlias, string name)
    {
        var allDataTypes = _dataTypeService.GetAll();
        var existing = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == editorAlias);
        return existing; // Just return existing - data types should already exist in Umbraco
    }

    public void CreateHotelDocumentType()
    {
        // Check if already exists
        var existing = _contentTypeService.Get("hotel");
        if (existing != null)
        {
            return; // Already exists
        }

        // Get existing data types - they should already exist in Umbraco
        var allDataTypes = _dataTypeService.GetAll();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        
        if (textstringDataType == null || textareaDataType == null)
        {
            throw new Exception($"Required data types not found. Please create data types in Umbraco backoffice first:\n" +
                              $"1. Go to Settings > Data Types\n" +
                              $"2. Create 'Textstring' with editor 'Textbox'\n" +
                              $"3. Create 'Textarea' with editor 'Textarea'\n" +
                              $"Available editors: {string.Join(", ", allDataTypes.Select(dt => dt.EditorAlias).Distinct())}");
        }

        // Create Hotel document type
        var hotelType = new ContentType(_shortStringHelper, -1)
        {
            Name = "Hotel",
            Alias = "hotel",
            Icon = "icon-building",
            AllowedAsRoot = true,
            IsElement = false
        };

        // Add properties
        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "hotelName")
        {
            Name = "Hotel Name",
            Mandatory = true,
            SortOrder = 1
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textareaDataType, "description")
        {
            Name = "Description",
            SortOrder = 2
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "address")
        {
            Name = "Address",
            SortOrder = 3
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "city")
        {
            Name = "City",
            SortOrder = 4
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "country")
        {
            Name = "Country",
            SortOrder = 5
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "phone")
        {
            Name = "Phone",
            SortOrder = 6
        });

        hotelType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "email")
        {
            Name = "Email",
            SortOrder = 7
        });

        _contentTypeService.Save(hotelType);
    }

    public void CreateRoomDocumentType()
    {
        var existing = _contentTypeService.Get("room");
        if (existing != null)
        {
            return;
        }

        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null)
        {
            throw new Exception("Hotel document type must be created first");
        }

        var allDataTypes = _dataTypeService.GetAll();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var integerDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Integer");
        var decimalDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Decimal");
        
        if (textstringDataType == null || textareaDataType == null || integerDataType == null || decimalDataType == null)
        {
            throw new Exception("Required data types not found. Please create data types in Umbraco backoffice first.");
        }

        var roomType = new ContentType(_shortStringHelper, hotelType.Id)
        {
            Name = "Room",
            Alias = "room",
            Icon = "icon-home",
            AllowedAsRoot = false,
            IsElement = false
        };

        roomType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "roomName")
        {
            Name = "Room Name",
            Mandatory = true,
            SortOrder = 1
        });

        roomType.AddPropertyType(new PropertyType(_shortStringHelper, textareaDataType, "description")
        {
            Name = "Description",
            SortOrder = 2
        });

        roomType.AddPropertyType(new PropertyType(_shortStringHelper, integerDataType, "maxOccupancy")
        {
            Name = "Max Occupancy",
            SortOrder = 3
        });

        roomType.AddPropertyType(new PropertyType(_shortStringHelper, decimalDataType, "priceFrom")
        {
            Name = "Price From",
            SortOrder = 4
        });

        roomType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "roomType")
        {
            Name = "Room Type",
            SortOrder = 5
        });

        _contentTypeService.Save(roomType);
    }

    public void CreateOfferDocumentType()
    {
        var existing = _contentTypeService.Get("offer");
        if (existing != null)
        {
            return;
        }

        var hotelType = _contentTypeService.Get("hotel");
        if (hotelType == null)
        {
            throw new Exception("Hotel document type must be created first");
        }

        var allDataTypes = _dataTypeService.GetAll();
        var textstringDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextBox");
        var textareaDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.TextArea");
        var decimalDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.Decimal");
        var dateTimeDataType = allDataTypes.FirstOrDefault(dt => dt.EditorAlias == "Umbraco.DateTime");
        
        if (textstringDataType == null || textareaDataType == null || decimalDataType == null || dateTimeDataType == null)
        {
            throw new Exception("Required data types not found. Please create data types in Umbraco backoffice first.");
        }

        var offerType = new ContentType(_shortStringHelper, hotelType.Id)
        {
            Name = "Offer",
            Alias = "offer",
            Icon = "icon-gift",
            AllowedAsRoot = false,
            IsElement = false
        };

        offerType.AddPropertyType(new PropertyType(_shortStringHelper, textstringDataType, "offerName")
        {
            Name = "Offer Name",
            Mandatory = true,
            SortOrder = 1
        });

        offerType.AddPropertyType(new PropertyType(_shortStringHelper, textareaDataType, "description")
        {
            Name = "Description",
            SortOrder = 2
        });

        offerType.AddPropertyType(new PropertyType(_shortStringHelper, decimalDataType, "discount")
        {
            Name = "Discount",
            SortOrder = 3
        });

        offerType.AddPropertyType(new PropertyType(_shortStringHelper, dateTimeDataType, "validFrom")
        {
            Name = "Valid From",
            SortOrder = 4
        });

        offerType.AddPropertyType(new PropertyType(_shortStringHelper, dateTimeDataType, "validTo")
        {
            Name = "Valid To",
            SortOrder = 5
        });

        _contentTypeService.Save(offerType);
    }

    public void CreateAllDocumentTypes()
    {
        CreateHotelDocumentType();
        CreateRoomDocumentType();
        CreateOfferDocumentType();
    }

}


using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace MyDockerProject.Services;

public class DataTypeService
{
    private readonly IDataTypeService _dataTypeService;

    public DataTypeService(IDataTypeService dataTypeService)
    {
        _dataTypeService = dataTypeService;
    }

    public void CreateDefaultDataTypes()
    {
        var allDataTypes = _dataTypeService.GetAll().ToList();
        var existingAliases = allDataTypes.Select(dt => dt.EditorAlias).Distinct().ToHashSet();

        // List of required data types with their editor aliases
        var requiredDataTypes = new Dictionary<string, string>
        {
            { "Textstring", "Umbraco.TextBox" },
            { "Textarea", "Umbraco.TextArea" },
            { "Rich Text Editor", "Umbraco.RichText" },
            { "Media Picker", "Umbraco.MediaPicker3" },
            { "Integer", "Umbraco.Integer" },
            { "Decimal", "Umbraco.Decimal" },
            { "DateTime", "Umbraco.DateTime" },
            { "URL Picker", "Umbraco.MultiUrlPicker" },
            { "True/False", "Umbraco.TrueFalse" },
            { "Color Picker", "Umbraco.ColorPicker" }
        };

        var created = new List<string>();
        var skipped = new List<string>();

        foreach (var kvp in requiredDataTypes)
        {
            var name = kvp.Key;
            var editorAlias = kvp.Value;

            // Check if a data type with this editor already exists
            if (existingAliases.Contains(editorAlias))
            {
                skipped.Add($"{name} (editor: {editorAlias})");
                continue;
            }

            // Check if a data type with this name already exists
            var existingByName = allDataTypes.FirstOrDefault(dt => dt.Name == name);
            if (existingByName != null)
            {
                skipped.Add($"{name} (already exists)");
                continue;
            }

            // Note: In Umbraco 16, we can't easily create data types programmatically
            // without the proper editor instances. Instead, we'll return a list of what needs to be created.
            // The actual creation should be done via uSync or manually.
            created.Add($"{name} ({editorAlias})");
        }

        // Return information about what needs to be created
        if (created.Any())
        {
            throw new Exception($"The following data types need to be created manually in Umbraco backoffice:\n" +
                              $"{string.Join("\n", created.Select(c => $"  - {c}"))}\n\n" +
                              $"Go to Settings > Data Types and create them with the specified editors.");
        }
    }

    public Dictionary<string, string> GetRequiredDataTypes()
    {
        return new Dictionary<string, string>
        {
            { "Textstring", "Umbraco.TextBox" },
            { "Textarea", "Umbraco.TextArea" },
            { "Rich Text Editor", "Umbraco.RichText" },
            { "Media Picker", "Umbraco.MediaPicker3" },
            { "Integer", "Umbraco.Integer" },
            { "Decimal", "Umbraco.Decimal" },
            { "DateTime", "Umbraco.DateTime" },
            { "URL Picker", "Umbraco.MultiUrlPicker" },
            { "True/False", "Umbraco.TrueFalse" },
            { "Color Picker", "Umbraco.ColorPicker" }
        };
    }

    public List<string> CheckMissingDataTypes()
    {
        var allDataTypes = _dataTypeService.GetAll().ToList();
        var existingAliases = allDataTypes.Select(dt => dt.EditorAlias).Distinct().ToHashSet();
        var required = GetRequiredDataTypes();
        var missing = new List<string>();

        foreach (var kvp in required)
        {
            var name = kvp.Key;
            var editorAlias = kvp.Value;

            // Check if any data type uses this editor
            if (!existingAliases.Contains(editorAlias))
            {
                missing.Add($"{name} ({editorAlias})");
            }
        }

        return missing;
    }
}


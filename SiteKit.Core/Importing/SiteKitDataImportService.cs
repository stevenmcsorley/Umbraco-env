using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using SiteKit.Core.Data;

namespace SiteKit.Core.Importing;

/// <summary>Previewable, field-mapped imports into SiteKit operational entities.</summary>
public sealed class SiteKitDataImportService
{
    public const int MaximumBytes = 10 * 1024 * 1024;
    public const int MaximumRows = 5000;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitRecordStore _records;

    public SiteKitDataImportService(ISiteKitEntityRegistry entities, ISiteKitRecordStore records)
    { _entities = entities; _records = records; }

    public async Task<SiteKitImportPreview> PreviewAsync(SiteKitImportMapping mapping, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var (entity, rows) = await ParseAndValidateAsync(mapping, content, cancellationToken);
        var errors = new List<SiteKitImportRowError>();
        var valid = 0;
        var samples = new List<JsonElement>();
        for (var index = 0; index < rows.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var converted = ConvertRow(mapping, entity, rows[index], index + 1);
                var rowErrors = SiteKitEntityValidator.ValidateRecord(entity, converted);
                if (rowErrors.Count == 0) valid++;
                else errors.AddRange(rowErrors.Select(error => new SiteKitImportRowError(index + 1, error)));
                if (samples.Count < 10) samples.Add(converted.Clone());
            }
            catch (ArgumentException exception)
            {
                errors.Add(new SiteKitImportRowError(index + 1, exception.Message));
            }
        }
        return new SiteKitImportPreview(rows.Count, valid, errors, samples);
    }

    public async Task<SiteKitImportResult> ImportAsync(SiteKitImportMapping mapping, ReadOnlyMemory<byte> content, string actor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 320) throw new ArgumentException("A valid authenticated import actor is required.", nameof(actor));
        var (entity, rows) = await ParseAndValidateAsync(mapping, content, cancellationToken);
        var existingRecords = (await _records.ListAsync(mapping.ProjectId, mapping.EntityAlias, cancellationToken)).ToList();
        var errors = new List<SiteKitImportRowError>();
        var created = 0;
        var updated = 0;
        for (var index = 0; index < rows.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var data = ConvertRow(mapping, entity, rows[index], index + 1);
                var match = FindMatch(mapping.MatchFields, data, existingRecords);
                var merged = match is null ? data : Merge(match.Data, data);
                var rowErrors = SiteKitEntityValidator.ValidateRecord(entity, merged);
                if (rowErrors.Count > 0) throw new ArgumentException(string.Join(" ", rowErrors));
                var relationshipErrors = await ValidateRelationshipsAsync(entity, merged, cancellationToken);
                if (relationshipErrors.Count > 0) throw new ArgumentException(string.Join(" ", relationshipErrors));
                var saved = await _records.SaveAsync(entity, merged, actor, match?.Id, cancellationToken);
                if (match is null) { created++; existingRecords.Add(saved); }
                else { updated++; existingRecords[existingRecords.FindIndex(record => record.Id == match.Id)] = saved; }
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException)
            {
                errors.Add(new SiteKitImportRowError(index + 1, exception.Message));
            }
        }
        return new SiteKitImportResult(rows.Count, created, updated, errors);
    }

    private async Task<(SiteKitEntityDefinition Entity, List<Dictionary<string, JsonElement>> Rows)> ParseAndValidateAsync(SiteKitImportMapping mapping, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var mappingErrors = JsonSiteKitImportMappingRegistry.Validate(mapping);
        if (mappingErrors.Count > 0) throw new ArgumentException(string.Join(" ", mappingErrors));
        if (content.Length == 0 || content.Length > MaximumBytes) throw new ArgumentException("Import files must be between 1 byte and 10 MiB.", nameof(content));
        var entity = await _entities.GetAsync(mapping.ProjectId, mapping.EntityAlias, ct) ?? throw new InvalidOperationException("Import entity is not defined in this project.");
        var fieldAliases = entity.Fields.Select(field => field.Alias).ToHashSet(StringComparer.Ordinal);
        var invalidTargets = mapping.Fields.Values.Where(alias => !fieldAliases.Contains(alias) || alias == "id").ToArray();
        if (invalidTargets.Length > 0) throw new ArgumentException($"Mapping targets undefined or generated id fields: {string.Join(", ", invalidTargets)}.");
        if (mapping.MatchFields.Any(alias => !entity.Fields.Any(field => field.Alias == alias && field.Unique)))
            throw new ArgumentException("Every matchField must be a unique field on the target entity.");
        var rows = ParseRows(mapping, content);
        if (rows.Count > MaximumRows) throw new ArgumentException($"Import exceeds the {MaximumRows} row limit.");
        return (entity, rows);
    }

    private static List<Dictionary<string, JsonElement>> ParseRows(SiteKitImportMapping mapping, ReadOnlyMemory<byte> content)
    {
        var text = new UTF8Encoding(false, true).GetString(content.Span);
        if (mapping.Format == "json")
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new ArgumentException("JSON imports must contain an array of objects.");
            var rows = new List<Dictionary<string, JsonElement>>();
            foreach (var item in document.RootElement.EnumerateArray())
                rows.Add(ToDictionary(item) ?? throw new ArgumentException("Every JSON import row must be an object."));
            return rows;
        }
        if (mapping.Format == "csv")
        {
            var records = ParseCsv(text, mapping.Delimiter[0]);
            if (records.Count == 0) return [];
            var headers = mapping.HasHeader ? records[0] : mapping.Fields.Keys.ToList();
            if (headers.Count == 0 || headers.Count > 100 || headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.Ordinal).Count() != headers.Count)
                throw new ArgumentException("CSV headers must be non-empty, unique and limited to 100 columns.");
            var offset = mapping.HasHeader ? 1 : 0;
            var rows = new List<Dictionary<string, JsonElement>>();
            for (var rowIndex = offset; rowIndex < records.Count; rowIndex++)
            {
                if (records[rowIndex].Count != headers.Count) throw new ArgumentException($"CSV row {rowIndex + 1} has {records[rowIndex].Count} values; expected {headers.Count}.");
                rows.Add(headers.Select((header, column) => KeyValuePair.Create(header, JsonSerializer.SerializeToElement(records[rowIndex][column])))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            }
            return rows;
        }
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumBytes, MaxCharactersFromEntities = 0 };
        using var stringReader = new StringReader(text);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        var root = XDocument.Load(xmlReader, LoadOptions.None).Root ?? throw new ArgumentException("XML import has no root element.");
        return root.Elements().Select(item => item.Elements().GroupBy(child => child.Name.LocalName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => JsonSerializer.SerializeToElement(group.Last().Value), StringComparer.Ordinal)).ToList();
    }

    private static Dictionary<string, JsonElement>? ToDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        return element.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
    }

    private static JsonElement ConvertRow(SiteKitImportMapping mapping, SiteKitEntityDefinition entity, Dictionary<string, JsonElement> source, int rowNumber)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (sourceField, targetAlias) in mapping.Fields)
        {
            if (!source.TryGetValue(sourceField, out var sourceValue)) continue;
            var target = entity.Fields.First(field => field.Alias == targetAlias);
            try { result[targetAlias] = ConvertValue(sourceValue, target.Type); }
            catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
            { throw new ArgumentException($"Field '{sourceField}' on row {rowNumber} cannot be converted to {target.Type}.", exception); }
        }
        return JsonSerializer.SerializeToElement(result);
    }

    private static JsonElement ConvertValue(JsonElement value, string type)
    {
        if (value.ValueKind == JsonValueKind.Null) return value.Clone();
        var raw = value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();
        return type switch
        {
            "text" or "email" or "date" or "datetime" or "relationship" => JsonSerializer.SerializeToElement(raw),
            "number" => JsonSerializer.SerializeToElement(value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : decimal.Parse(raw, NumberStyles.Number, CultureInfo.InvariantCulture)),
            "boolean" => JsonSerializer.SerializeToElement(value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : bool.Parse(raw)),
            "json" or "relationships" => ParseJsonValue(value.ValueKind == JsonValueKind.String ? raw : value.GetRawText()),
            _ => throw new FormatException("Unsupported target type.")
        };
    }

    private static JsonElement ParseJsonValue(string value)
    {
        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static SiteKitEntityRecord? FindMatch(string[] matchFields, JsonElement data, List<SiteKitEntityRecord> records)
    {
        var matches = records.Where(record => matchFields.All(field => data.TryGetProperty(field, out var incoming) && record.Data.TryGetProperty(field, out var existing) && JsonElement.DeepEquals(incoming, existing))).Take(2).ToArray();
        return matches.Length switch { 0 => null, 1 => matches[0], _ => throw new InvalidOperationException("Import match fields identify more than one existing record.") };
    }

    private static JsonElement Merge(JsonElement existing, JsonElement incoming)
    {
        var data = existing.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
        foreach (var property in incoming.EnumerateObject()) data[property.Name] = property.Value.Clone();
        return JsonSerializer.SerializeToElement(data);
    }

    private async Task<IReadOnlyList<string>> ValidateRelationshipsAsync(SiteKitEntityDefinition entity, JsonElement data, CancellationToken ct)
    {
        var errors = new List<string>();
        foreach (var field in entity.Fields.Where(field => field.Type is "relationship" or "relationships" && data.TryGetProperty(field.Alias, out _)))
        {
            var target = field.RelatedEntity!;
            if (await _entities.GetAsync(entity.ProjectId, target, ct) is null) { errors.Add($"Related entity '{target}' is not defined."); continue; }
            var ids = field.Type == "relationships" ? data.GetProperty(field.Alias).EnumerateArray().Select(value => value.GetString()!).ToArray() : [data.GetProperty(field.Alias).GetString()!];
            foreach (var id in ids) if (await _records.GetAsync(entity.ProjectId, target, id, ct) is null) errors.Add($"Field '{field.Alias}' references a missing {target} record.");
        }
        return errors;
    }

    private static List<List<string>> ParseCsv(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"' && field.Length == 0) quoted = true;
            else if (character == delimiter) { row.Add(field.ToString()); field.Clear(); }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(field.ToString()); field.Clear();
                if (row.Any(value => value.Length > 0)) rows.Add(row);
                row = [];
            }
            else field.Append(character);
        }
        if (quoted) throw new ArgumentException("CSV ends inside a quoted value.");
        row.Add(field.ToString());
        if (row.Any(value => value.Length > 0)) rows.Add(row);
        if (rows.Count > MaximumRows + 1) throw new ArgumentException($"Import exceeds the {MaximumRows} row limit.");
        return rows;
    }
}

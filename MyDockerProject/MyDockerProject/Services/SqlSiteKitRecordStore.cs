using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyDockerProject.Data;
using MyDockerProject.Models;
using SiteKit.Core.Data;

namespace MyDockerProject.Services;

/// <summary>SQL Server persistence for project operational records, with versioned JSON payloads and soft deletion.</summary>
public sealed class SqlSiteKitRecordStore : ISiteKitRecordStore
{
    private static readonly SemaphoreSlim SchemaGate = new(1, 1);
    private readonly BookingDbContext _database;

    public SqlSiteKitRecordStore(BookingDbContext database) => _database = database;

    public async Task<IReadOnlyList<SiteKitEntityRecord>> ListAsync(string projectId, string entityAlias, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var values = await _database.SiteKitOperationalRecords.AsNoTracking()
            .Where(record => record.ProjectId == projectId && record.EntityAlias == entityAlias && !record.IsDeleted)
            .OrderByDescending(record => record.UpdatedAt).ToListAsync(cancellationToken);
        return values.Select(ToContract).ToArray();
    }

    public async Task<SiteKitEntityRecord?> GetAsync(string projectId, string entityAlias, string id, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var key = NormalizeId(id);
        var record = await _database.SiteKitOperationalRecords.AsNoTracking().FirstOrDefaultAsync(
            item => item.ProjectId == projectId && item.EntityAlias == entityAlias && item.Id == key && !item.IsDeleted, cancellationToken);
        return record is null ? null : ToContract(record);
    }

    public async Task<SiteKitEntityRecord> SaveAsync(SiteKitEntityDefinition definition, JsonElement data, string actor, string? id = null, CancellationToken cancellationToken = default)
    {
        var errors = SiteKitEntityValidator.ValidateRecord(definition, data);
        if (errors.Count != 0) throw new ArgumentException(string.Join(" ", errors));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 320) throw new ArgumentException("An authenticated actor is required.", nameof(actor));
        await EnsureTableAsync(cancellationToken);
        var key = id is null ? Guid.NewGuid().ToString("N") : NormalizeId(id);
        if (data.TryGetProperty("id", out var requestedId) && Guid.TryParse(requestedId.GetString(), out var bodyId) && bodyId.ToString("N") != key)
            throw new ArgumentException("Body id does not match the route id.", nameof(data));
        var normalizedJson = JsonSerializer.Serialize(data.EnumerateObject().Where(property => property.Name != "id")
            .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal));
        using var normalizedDocument = JsonDocument.Parse(normalizedJson);
        var normalizedData = normalizedDocument.RootElement;

        await using var transaction = await _database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureRelationshipsExistAsync(definition, normalizedData, cancellationToken);
        var existing = await _database.SiteKitOperationalRecords.FromSqlInterpolated($"SELECT * FROM [SiteKitOperationalRecord] WITH (UPDLOCK, HOLDLOCK) WHERE [ProjectId] = {definition.ProjectId} AND [EntityAlias] = {definition.Alias} AND [Id] = {key}")
            .FirstOrDefaultAsync(cancellationToken);
        if (existing?.IsDeleted == true) existing = null;
        var otherRecords = await _database.SiteKitOperationalRecords.FromSqlInterpolated($"SELECT * FROM [SiteKitOperationalRecord] WITH (UPDLOCK, HOLDLOCK) WHERE [ProjectId] = {definition.ProjectId} AND [EntityAlias] = {definition.Alias} AND [IsDeleted] = 0")
            .AsNoTracking().ToListAsync(cancellationToken);
        foreach (var field in definition.Fields.Where(field => field.Unique && normalizedData.TryGetProperty(field.Alias, out _)))
        {
            var value = normalizedData.GetProperty(field.Alias);
            foreach (var other in otherRecords)
            {
                if (other.Id == key) continue;
                using var parsed = JsonDocument.Parse(other.DataJson);
                if (parsed.RootElement.TryGetProperty(field.Alias, out var otherValue) && JsonElement.DeepEquals(value, otherValue))
                    throw new InvalidOperationException($"The value of unique field '{field.Alias}' is already in use.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var row = existing ?? new SiteKitOperationalRecord { Id = key, ProjectId = definition.ProjectId, EntityAlias = definition.Alias, DataJson = normalizedJson, CreatedAt = now };
        row.SchemaVersion = definition.Version;
        row.DataJson = normalizedJson;
        row.CreatedBy ??= actor;
        row.UpdatedBy = actor;
        row.UpdatedAt = now;
        row.IsDeleted = false;
        if (existing is null) _database.SiteKitOperationalRecords.Add(row);
        await _database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToContract(row);
    }

    private async Task EnsureRelationshipsExistAsync(SiteKitEntityDefinition definition, JsonElement data, CancellationToken cancellationToken)
    {
        foreach (var field in definition.Fields.Where(field => field.Type is "relationship" or "relationships" && data.TryGetProperty(field.Alias, out _)))
        {
            var value = data.GetProperty(field.Alias);
            var ids = field.Type == "relationships"
                ? value.EnumerateArray().Select(item => NormalizeId(item.GetString()!))
                : [NormalizeId(value.GetString()!)];
            foreach (var relatedId in ids)
            {
                var related = await _database.SiteKitOperationalRecords.FromSqlInterpolated($"SELECT * FROM [SiteKitOperationalRecord] WITH (UPDLOCK, HOLDLOCK) WHERE [ProjectId] = {definition.ProjectId} AND [EntityAlias] = {field.RelatedEntity} AND [Id] = {relatedId} AND [IsDeleted] = 0")
                    .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                if (related is null)
                    throw new InvalidOperationException($"Relationship field '{field.Alias}' references a missing {field.RelatedEntity} record.");
            }
        }
    }

    public async Task<bool> DeleteAsync(string projectId, string entityAlias, string id, string actor, IReadOnlyList<SiteKitEntityDefinition> entityDefinitions, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var key = NormalizeId(id);
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 320) throw new ArgumentException("An authenticated actor is required.", nameof(actor));
        await using var transaction = await _database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var row = await _database.SiteKitOperationalRecords.FromSqlInterpolated($"SELECT * FROM [SiteKitOperationalRecord] WITH (UPDLOCK, HOLDLOCK) WHERE [ProjectId] = {projectId} AND [EntityAlias] = {entityAlias} AND [Id] = {key} AND [IsDeleted] = 0")
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return false;
        foreach (var source in entityDefinitions.Where(definition => definition.ProjectId == projectId))
        foreach (var field in source.Fields.Where(field => field.Type is "relationship" or "relationships" && field.RelatedEntity == entityAlias))
            if (await HasReferenceAsync(projectId, source.Alias, field, key, cancellationToken))
                throw new InvalidOperationException($"Cannot delete {entityAlias} record '{key}' because a {source.Alias} record references it through '{field.Alias}'.");
        row.IsDeleted = true;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        row.UpdatedBy = actor;
        await _database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> HasReferenceAsync(string projectId, string sourceAlias, SiteKitEntityField field, string recordId, CancellationToken cancellationToken)
    {
        var connection = _database.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = _database.Database.CurrentTransaction!.GetDbTransaction();
        var pathParameter = command.CreateParameter();
        pathParameter.ParameterName = "@path";
        // JSON serialization gives SQL Server's JSON path grammar the required
        // double-quoted property segment, including safe escaping for punctuation.
        pathParameter.Value = "$." + JsonSerializer.Serialize(field.Alias);
        command.Parameters.Add(pathParameter);
        var projectParameter = command.CreateParameter();
        projectParameter.ParameterName = "@projectId";
        projectParameter.Value = projectId;
        command.Parameters.Add(projectParameter);
        var sourceParameter = command.CreateParameter();
        sourceParameter.ParameterName = "@sourceAlias";
        sourceParameter.Value = sourceAlias;
        command.Parameters.Add(sourceParameter);
        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "@recordId";
        idParameter.Value = Guid.ParseExact(recordId, "N");
        idParameter.DbType = DbType.Guid;
        command.Parameters.Add(idParameter);
        var relationPredicate = field.Type == "relationships"
            ? "EXISTS (SELECT 1 FROM OPENJSON([DataJson], @path) AS relation WHERE TRY_CONVERT(uniqueidentifier, relation.[value]) = @recordId)"
            : "TRY_CONVERT(uniqueidentifier, JSON_VALUE([DataJson], @path)) = @recordId";
        command.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM [SiteKitOperationalRecord] WITH (UPDLOCK, HOLDLOCK) WHERE [ProjectId] = @projectId AND [EntityAlias] = @sourceAlias AND [IsDeleted] = 0 AND {relationPredicate}) THEN 1 ELSE 0 END";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_database.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) != true)
            throw new InvalidOperationException("The SQL record store requires the configured SQL Server provider.");
        await SchemaGate.WaitAsync(cancellationToken);
        try
        {
            await _database.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'[SiteKitOperationalRecord]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SiteKitOperationalRecord](
                        [Id] nvarchar(32) NOT NULL CONSTRAINT [PK_SiteKitOperationalRecord] PRIMARY KEY,
                        [ProjectId] nvarchar(64) NOT NULL,
                        [EntityAlias] nvarchar(64) NOT NULL,
                        [SchemaVersion] int NOT NULL,
                        [DataJson] nvarchar(max) NOT NULL,
                        [CreatedBy] nvarchar(320) NULL,
                        [UpdatedBy] nvarchar(320) NULL,
                        [CreatedAt] datetimeoffset NOT NULL,
                        [UpdatedAt] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL CONSTRAINT [DF_SiteKitOperationalRecord_IsDeleted] DEFAULT(0)
                    );
                    CREATE INDEX [IX_SiteKitOperationalRecord_Project_Entity_Updated]
                        ON [SiteKitOperationalRecord]([ProjectId],[EntityAlias],[UpdatedAt]);
                END
                """, cancellationToken);
        }
        finally { SchemaGate.Release(); }
    }

    private static SiteKitEntityRecord ToContract(SiteKitOperationalRecord row)
    {
        using var document = JsonDocument.Parse(row.DataJson);
        return new SiteKitEntityRecord { Id = row.Id, ProjectId = row.ProjectId, EntityAlias = row.EntityAlias, SchemaVersion = row.SchemaVersion,
            Data = document.RootElement.Clone(), CreatedBy = row.CreatedBy, UpdatedBy = row.UpdatedBy, CreatedAt = row.CreatedAt, UpdatedAt = row.UpdatedAt };
    }

    private static string NormalizeId(string id)
    {
        if (!Guid.TryParse(id, out var key)) throw new ArgumentException("Record id must be a UUID.", nameof(id));
        return key.ToString("N");
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using MyDockerProject.Security;
using SiteKit.Core.Data;
using SiteKit.Core.Importing;
using SiteKit.Core.Projects;

namespace MyDockerProject.Controllers.Api;

[ApiController]
[Route("api/sitekit/projects/{projectId}/imports")]
[TypeFilter(typeof(SiteKitAdminAuthorizationFilter))]
public sealed class SiteKitImportsController : ControllerBase
{
    private readonly ISiteKitImportMappingRegistry _mappings;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitPermissionRegistry _permissions;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly SiteKitDataImportService _imports;
    private readonly ISiteKitImportRunRegistry _runs;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private const long MaxUploadBytes = SiteKitDataImportService.MaximumBytes;

    public SiteKitImportsController(ISiteKitImportMappingRegistry mappings, ISiteKitEntityRegistry entities,
        ISiteKitPermissionRegistry permissions, ISiteKitProjectRegistry projects, SiteKitDataImportService imports, ISiteKitImportRunRegistry runs,
        IHttpClientFactory httpClientFactory, IConfiguration configuration)
    { _mappings = mappings; _entities = entities; _permissions = permissions; _projects = projects; _imports = imports; _runs = runs; _httpClientFactory = httpClientFactory; _configuration = configuration; }

    [HttpGet]
    public async Task<IActionResult> List(string projectId, CancellationToken ct)
        => !await ProjectExists(projectId, ct) ? NotFound() : Ok(await _mappings.ListAsync(projectId, ct));

    [HttpPut("{alias}")]
    [RequestSizeLimit(262_144)]
    public async Task<IActionResult> Save(string projectId, string alias, [FromBody] SiteKitImportMapping mapping, CancellationToken ct)
    {
        if (!await ProjectExists(projectId, ct)) return NotFound();
        if (mapping is null) return BadRequest(new { error = "Mapping definition is required." });
        var mappingErrors = JsonSiteKitImportMappingRegistry.Validate(mapping);
        if (mappingErrors.Count != 0) return BadRequest(new { error = string.Join(" ", mappingErrors) });
        if (mapping.ProjectId != projectId || mapping.Alias != alias) return BadRequest(new { error = "Route and mapping identifiers must match." });
        var entity = await _entities.GetAsync(projectId, mapping.EntityAlias, ct);
        if (entity is null) return BadRequest(new { error = "Target entity is not defined in this project." });
        if (!string.IsNullOrWhiteSpace(mapping.SourceUrl) && !IsAllowedSourceHost(mapping.SourceUrl))
            return BadRequest(new { error = "REST source host must be listed in SiteKit:Import:AllowedHosts." });
        var invalid = mapping.Fields.Values.Where(target => target == "id" || !entity.Fields.Any(field => field.Alias == target)).ToArray();
        if (invalid.Length != 0) return BadRequest(new { error = "Mapping targets undefined or generated fields.", fields = invalid });
        try { return Ok(await _mappings.SaveAsync(mapping, ct)); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{alias}/runs")]
    public async Task<IActionResult> Runs(string projectId, string alias, CancellationToken ct)
    {
        var mapping = await GetMapping(projectId, alias, ct);
        if (mapping is null) return NotFound();
        if (!await Can("read", projectId, mapping.EntityAlias, ct)) return Forbid();
        return Ok(await _runs.ListAsync(projectId, alias, ct));
    }

    [HttpPost("{alias}/preview")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Preview(string projectId, string alias, IFormFile? file, CancellationToken ct)
    {
        var mapping = await GetMapping(projectId, alias, ct);
        if (mapping is null) return NotFound();
        if (!HasRestSource(mapping) && (file is null || file.Length is 0 or > MaxUploadBytes)) return BadRequest(new { error = "Provide a file between 1 byte and 10 MiB, or configure an allowlisted REST source." });
        if (HasRestSource(mapping) && file is not null) return BadRequest(new { error = "Choose either the configured REST source or an uploaded file, not both." });
        if (!await Can("read", projectId, mapping.EntityAlias, ct)) return Forbid();
        var startedAt = DateTimeOffset.UtcNow;
        ReadOnlyMemory<byte> bytes = ReadOnlyMemory<byte>.Empty;
        try
        {
            bytes = await ReadImportContent(mapping, file, ct);
            var preview = await _imports.PreviewAsync(mapping, bytes, ct);
            await SaveRun(mapping, "preview", preview.Errors.Count == 0 ? "complete" : "partial", SHA256.HashData(bytes.Span),
                preview.RowsRead, 0, 0, preview.Errors.Count, startedAt, ct);
            return Ok(preview);
        }
        catch (ArgumentException exception) { await SaveFailedRun(mapping, "preview", bytes, startedAt, ct); return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { await SaveFailedRun(mapping, "preview", bytes, startedAt, ct); return Conflict(new { error = exception.Message }); }
        catch (HttpRequestException exception) { await SaveFailedRun(mapping, "preview", bytes, startedAt, ct); return StatusCode(502, new { error = "REST source could not be fetched: " + exception.Message }); }
    }

    [HttpPost("{alias}/run")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Run(string projectId, string alias, IFormFile? file, CancellationToken ct)
    {
        var mapping = await GetMapping(projectId, alias, ct);
        if (mapping is null) return NotFound();
        if (!HasRestSource(mapping) && (file is null || file.Length is 0 or > MaxUploadBytes)) return BadRequest(new { error = "Provide a file between 1 byte and 10 MiB, or configure an allowlisted REST source." });
        if (HasRestSource(mapping) && file is not null) return BadRequest(new { error = "Choose either the configured REST source or an uploaded file, not both." });
        if (!await Can("import", projectId, mapping.EntityAlias, ct)) return Forbid();
        var startedAt = DateTimeOffset.UtcNow;
        ReadOnlyMemory<byte> bytes = ReadOnlyMemory<byte>.Empty;
        try
        {
            bytes = await ReadImportContent(mapping, file, ct);
            var result = await _imports.ImportAsync(mapping, bytes, HttpContext.Session.GetString("UserEmail")!, ct);
            await SaveRun(mapping, "import", result.Errors.Count == 0 ? "complete" : "partial", SHA256.HashData(bytes.Span),
                result.RowsRead, result.Created, result.Updated, result.Errors.Count, startedAt, ct);
            return Ok(result);
        }
        catch (ArgumentException exception) { await SaveFailedRun(mapping, "import", bytes, startedAt, ct); return BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { await SaveFailedRun(mapping, "import", bytes, startedAt, ct); return Conflict(new { error = exception.Message }); }
        catch (HttpRequestException exception) { await SaveFailedRun(mapping, "import", bytes, startedAt, ct); return StatusCode(502, new { error = "REST source could not be fetched: " + exception.Message }); }
    }

    private async Task<SiteKitImportMapping?> GetMapping(string projectId, string alias, CancellationToken ct)
    {
        if (!SiteKitProjectValidator.IsValidId(projectId) || !await ProjectExists(projectId, ct)) return null;
        return await _mappings.GetAsync(projectId, alias, ct);
    }

    private async Task<bool> Can(string operation, string projectId, string entityAlias, CancellationToken ct)
    {
        var principal = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrWhiteSpace(principal)) return false;
        var grant = await _permissions.GetAsync(projectId, entityAlias, principal, ct);
        return grant?.Operations.Contains(operation, StringComparer.Ordinal) == true || grant?.Operations.Contains("manage", StringComparer.Ordinal) == true;
    }

    private Task<SiteKitImportRun> SaveRun(SiteKitImportMapping mapping, string mode, string status, byte[] contentHash,
        int rowsRead, int created, int updated, int errorCount, DateTimeOffset startedAt, CancellationToken ct)
    {
        var actor = HttpContext.Session.GetString("UserEmail")!;
        return _runs.SaveAsync(new SiteKitImportRun
        {
            Id = Guid.NewGuid().ToString("N"), ProjectId = mapping.ProjectId, MappingAlias = mapping.Alias,
            EntityAlias = mapping.EntityAlias, Actor = actor, Mode = mode, Status = status,
            ContentSha256 = Convert.ToHexString(contentHash), RowsRead = rowsRead, Created = created, Updated = updated,
            ErrorCount = errorCount, StartedAt = startedAt, FinishedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    private async Task SaveFailedRun(SiteKitImportMapping mapping, string mode, ReadOnlyMemory<byte> content,
        DateTimeOffset startedAt, CancellationToken ct)
    {
        if (content.IsEmpty) return;
        await SaveRun(mapping, mode, "failed", SHA256.HashData(content.Span), 0, 0, 0, 1, startedAt, ct);
    }

    private async Task<bool> ProjectExists(string projectId, CancellationToken ct)
        => SiteKitProjectValidator.IsValidId(projectId) && await _projects.GetAsync(projectId, ct) is not null;

    private static async Task<ReadOnlyMemory<byte>> ReadFile(IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, ct);
        return stream.ToArray();
    }

    private static bool HasRestSource(SiteKitImportMapping mapping) => !string.IsNullOrWhiteSpace(mapping.SourceUrl);

    private bool IsAllowedSourceHost(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri)) return false;
        var allowed = AllowedImportHosts();
        return allowed.Contains(uri.IdnHost.TrimEnd('.'), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ReadOnlyMemory<byte>> ReadImportContent(SiteKitImportMapping mapping, IFormFile? file, CancellationToken ct)
    {
        if (HasRestSource(mapping))
            return await new SiteKitRestImportSource(_httpClientFactory.CreateClient("SiteKitImportSource"), AllowedImportHosts()).FetchAsync(mapping, ct);
        return await ReadFile(file!, ct);
    }

    private string[] AllowedImportHosts()
    {
        var hosts = _configuration.GetSection("SiteKit:Import:AllowedHosts").GetChildren()
            .Select(section => section.Value).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToList();
        hosts.AddRange((_configuration["SiteKit:Import:AllowedHosts"] ?? string.Empty)
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return hosts.ToArray();
    }
}

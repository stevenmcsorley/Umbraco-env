using System.Net;
using System.Text;
using System.Text.Json;
using SiteKit.Core.Components;
using SiteKit.Core.Data;

namespace SiteKit.Core.Projects;

/// <summary>Writes independently deployable ASP.NET Core projects from trusted SiteKit manifests.</summary>
public sealed class SiteKitProjectGenerator
{
    private readonly string _root;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitScreenRegistry? _screens;
    private readonly ISiteKitEntityRegistry? _entities;
    private readonly ISiteKitComponentRegistry? _components;
    private readonly ISiteKitPlacementRegistry? _placements;

    public SiteKitProjectGenerator(string root, ISiteKitProjectRegistry projects, ISiteKitScreenRegistry? screens = null, ISiteKitEntityRegistry? entities = null, ISiteKitComponentRegistry? components = null, ISiteKitPlacementRegistry? placements = null)
    { _root = Path.GetFullPath(root); _projects = projects; _screens = screens; _entities = entities; _components = components; _placements = placements; }

    public async Task<string> GenerateAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var manifest = await _projects.GetAsync(projectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        var errors = SiteKitProjectValidator.Validate(manifest);
        if (errors.Count != 0) throw new InvalidOperationException(string.Join(" ", errors));
        if (manifest.Category is not ("website" or "application" or "combined" or "api"))
            throw new InvalidOperationException("Generation supports website, application, combined and API projects.");
        Directory.CreateDirectory(_root);
        var destination = Path.GetFullPath(Path.Combine(_root, projectId));
        var rootPrefix = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Generated project path is outside its configured output directory.");
        Directory.CreateDirectory(destination);

        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        await WriteAtomicAsync(Path.Combine(destination, "sitekit.project.json"), manifestJson, cancellationToken);
        if (_screens is not null)
            await WriteAtomicAsync(Path.Combine(destination, "sitekit.screens.json"), JsonSerializer.Serialize(await _screens.ListAsync(projectId, cancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
        if (_entities is not null)
            await WriteAtomicAsync(Path.Combine(destination, "sitekit.entities.json"), JsonSerializer.Serialize(await _entities.ListAsync(projectId, cancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
        await WriteAtomicAsync(Path.Combine(destination, "global.json"), "{\n  \"sdk\": { \"version\": \"10.0.100\", \"rollForward\": \"latestFeature\" }\n}\n", cancellationToken);
        var databaseReference = manifest.Category is "application" or "combined"
            ? "<ItemGroup><PackageReference Include=\"Microsoft.Data.Sqlite\" Version=\"10.0.13\" /></ItemGroup>"
            : string.Empty;
        await WriteAtomicAsync(Path.Combine(destination, $"{manifest.Id}.csproj"), $"<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>{databaseReference}</Project>\n", cancellationToken);
        var renderedComponents = await RenderPlacementsAsync(projectId, cancellationToken);
        await WriteAtomicAsync(Path.Combine(destination, "Program.cs"), BuildProgram(manifest, renderedComponents), cancellationToken);
        if (manifest.Category is "application" or "combined" && _screens is not null)
        {
            Directory.CreateDirectory(Path.Combine(destination, "wwwroot"));
            await WriteAtomicAsync(Path.Combine(destination, "wwwroot", "sitekit-runtime.js"), BuildScreenRuntime(), cancellationToken);
        }
        await WriteAtomicAsync(Path.Combine(destination, "Dockerfile"), $"FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build\nWORKDIR /src\nCOPY . .\nRUN dotnet publish {manifest.Id}.csproj -c Release -o /app/publish\nFROM mcr.microsoft.com/dotnet/aspnet:10.0\nWORKDIR /app\nCOPY --from=build /app/publish .\nEXPOSE 8080\nENTRYPOINT [\"dotnet\", \"{manifest.Id}.dll\"]\n", cancellationToken);
        var runtimeNotes = manifest.Category is "application" or "combined"
            ? "\n\nApplication API access requires bearer tokens configured through your deployment secret store. For a single administrator set `SITEKIT_ADMIN_TOKEN`. For entity-level grants set `SITEKIT_PRINCIPAL_TOKENS` to a JSON map of SHA-256 token hashes to principal names and `SITEKIT_ENTITY_GRANTS` to each principal's entity operation grants (`read`, `create`, `update`, `delete`). Records persist in SQLite at `App_Data/sitekit.db`. Integrate a production identity provider before exposing the application to end users, and back up the SQLite database as part of deployment.\n"
            : string.Empty;
        await WriteAtomicAsync(Path.Combine(destination, "README.md"), $"# {manifest.Name}\n\nGenerated by SiteKit. Project category: `{manifest.Category}`.\n\nBuild with `dotnet build {manifest.Id}.csproj`, run with `dotnet run --project {manifest.Id}.csproj`, or use the included Dockerfile.\n{runtimeNotes}\nProject settings are in `sitekit.project.json`.\n", cancellationToken);
        return destination;
    }

    private static async Task WriteAtomicAsync(string destination, string content, CancellationToken cancellationToken)
    {
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, content, new UTF8Encoding(false), cancellationToken);
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private async Task<string> RenderPlacementsAsync(string projectId, CancellationToken cancellationToken)
    {
        if (_components is null || _placements is null) return string.Empty;
        var rendered = new List<string>();
        foreach (var placement in await _placements.ListAsync(projectId, cancellationToken))
        {
            var component = await _components.GetAsync(placement.ComponentAlias, placement.Version, cancellationToken)
                ?? throw new InvalidOperationException($"Placed component '{placement.ComponentAlias}' was not found in the shared registry.");
            rendered.Add(SiteKitComponentRenderer.Render(component, placement.Data, placement.Variant, placement.StyleClass, placement.Appearance));
        }
        return string.Join("\n", rendered);
    }

    private static string BuildProgram(SiteKitProjectManifest manifest, string renderedComponents)
    {
        var name = JsonSerializer.Serialize(manifest.Name);
        if (manifest.Category == "api")
            return "var app = WebApplication.Create(args);\napp.MapGet(\"/\", () => Results.Ok(new { name = " + name + ", category = \"api\", platform = \"SiteKit\" }));\napp.MapGet(\"/health\", () => Results.Ok(new { status = \"healthy\" }));\napp.MapGet(\"/favicon.ico\", () => Results.NoContent());\napp.Run();\n";
        if (manifest.Category is "application" or "combined") return BuildApplicationProgram(manifest, renderedComponents);
        var fontFamily = manifest.ThemeSettings.FontFamily switch { "serif" => "Georgia,serif", "sans-serif" => "Arial,sans-serif", "monospace" => "ui-monospace,monospace", _ => "system-ui,sans-serif" };
        var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(manifest.Name)}</title><style>:root{{--primary:{manifest.ThemeSettings.PrimaryColor};--surface:{manifest.ThemeSettings.SurfaceColor};--text:{manifest.ThemeSettings.TextColor}}}body{{margin:0;background:var(--surface);color:var(--text);font:16px {fontFamily}}}main{{max-width:1100px;margin:auto;padding:4rem 2rem}}h1{{font-size:clamp(2rem,5vw,4rem)}}.button{{display:inline-block;background:var(--primary);color:#fff;padding:.8rem 1.1rem;border-radius:{manifest.ThemeSettings.Radius};text-decoration:none}}.notice{{padding:1rem;border:1px solid #dce3e8;border-radius:12px}}</style><main><small>SITEKIT {WebUtility.HtmlEncode(manifest.Category.ToUpperInvariant())}</small><h1>{WebUtility.HtmlEncode(manifest.Name)}</h1><p>{WebUtility.HtmlEncode(manifest.Description)}</p><div class=\"notice\">Your independent SiteKit project is ready. Add content, components and application modules from SiteKit Studio.</div><section class=\"sitekit-components\">{renderedComponents}</section></main></html>";
        var serializedHtml = JsonSerializer.Serialize(html);
        return "var app = WebApplication.Create(args);\napp.MapGet(\"/health\", () => Results.Ok(new { status = \"healthy\" }));\napp.MapGet(\"/favicon.ico\", () => Results.NoContent());\napp.MapGet(\"/\", () => Results.Content(" + serializedHtml + ", \"text/html; charset=utf-8\"));\napp.Run();\n";
    }

    private static string BuildApplicationProgram(SiteKitProjectManifest manifest, string renderedComponents)
    {
        var fontFamily = manifest.ThemeSettings.FontFamily switch { "serif" => "Georgia,serif", "sans-serif" => "Arial,sans-serif", "monospace" => "ui-monospace,monospace", _ => "system-ui,sans-serif" };
        var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(manifest.Name)}</title><style>:root{{--primary:{manifest.ThemeSettings.PrimaryColor};--surface:{manifest.ThemeSettings.SurfaceColor};--text:{manifest.ThemeSettings.TextColor}}}body{{margin:0;background:var(--surface);color:var(--text);font:16px {fontFamily}}}main{{max-width:1120px;margin:auto;padding:3rem 2rem}}h1{{font-size:clamp(2rem,5vw,4rem)}}table{{width:100%;border-collapse:collapse}}td,th{{padding:.65rem;text-align:left;border-bottom:1px solid #dce3e8}}input,button{{font:inherit;padding:.6rem;border:1px solid #b8c5cc;border-radius:6px}}button{{cursor:pointer}}.screen{{margin-top:2rem}}.form{{display:flex;gap:.6rem;flex-wrap:wrap;margin:1rem 0}}.form label{{display:grid;gap:.2rem}}</style><main><small>SITEKIT {WebUtility.HtmlEncode(manifest.Category.ToUpperInvariant())}</small><h1>{WebUtility.HtmlEncode(manifest.Name)}</h1><p>{WebUtility.HtmlEncode(manifest.Description)}</p><section class=\"sitekit-components\">{renderedComponents}</section><div id=\"sitekit-screens\">Loading application screens…</div></main><script src=\"/sitekit-runtime.js\"></script><script>SiteKit.start();</script></html>";
        var template = """
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
var app = WebApplication.Create(args);
app.UseStaticFiles();
var root = Path.Combine(app.Environment.ContentRootPath, "App_Data"); Directory.CreateDirectory(root);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(root, "sitekit.db"), ForeignKeys = true }.ToString();
await Migrate();
var screens = Load<Screen[]>("sitekit.screens.json", []);
var entities = Load<Entity[]>("sitekit.entities.json", []);
var tokenPrincipals = LoadPrincipals();
var grants = LoadGrants();
var writeGate = new SemaphoreSlim(1, 1);
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/sitekit"))
    {
        var supplied = context.Request.Headers.Authorization.ToString();
        var adminToken = Environment.GetEnvironmentVariable("SITEKIT_ADMIN_TOKEN");
        var bearer = supplied.StartsWith("Bearer ", StringComparison.Ordinal) ? supplied[7..] : string.Empty;
        var isAdmin = !string.IsNullOrWhiteSpace(adminToken) && FixedEquals(adminToken, bearer);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bearer)));
        var configured = !string.IsNullOrWhiteSpace(adminToken) || tokenPrincipals.Count > 0;
        if (string.IsNullOrWhiteSpace(bearer) || (!isAdmin && !tokenPrincipals.ContainsKey(hash)))
        {
            context.Response.StatusCode = configured ? 401 : 503;
            await context.Response.WriteAsJsonAsync(new { error = configured ? "A valid bearer token is required." : "Configure SITEKIT_ADMIN_TOKEN or SITEKIT_PRINCIPAL_TOKENS before using the application API." });
            return;
        }
        context.Items["sitekit-is-admin"] = isAdmin;
        context.Items["sitekit-principal"] = isAdmin ? "admin" : tokenPrincipals[hash];
    }
    await next();
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/sitekit/screens", (HttpContext context) => screens.Where(screen => screen.EntityAlias is null ? CanAnyRead(context) : Can(context, screen.EntityAlias, "read")));
app.MapGet("/api/sitekit/entities", (HttpContext context) => entities.Where(entity => Can(context, entity.Alias, "read")).Select(entity => new { entity.Alias, entity.Name, entity.Fields }));
app.MapGet("/api/sitekit/records/{entity}", async (HttpContext context, string entity) =>
{
    if (!Exists(entity)) return Results.NotFound(); if (!Can(context, entity, "read")) return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Json(await List(entity));
});
app.MapPost("/api/sitekit/records/{entity}", async (HttpContext context, string entity, JsonElement body) =>
{
    var model = Find(entity); if (model is null) return Results.NotFound(); if (!Can(context, entity, "create")) return Results.StatusCode(StatusCodes.Status403Forbidden);
    await writeGate.WaitAsync();
    try
    {
        var errors = await Validate(model, body); if (errors.Length > 0) return Problem(errors);
        var id = Guid.NewGuid().ToString("N");
        var row = new Row(id, Normalize(model, body, id), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var rows = await List(entity); rows.Add(row); await Save(entity, rows);
        return Results.Created("/api/sitekit/records/" + entity + "/" + id, row);
    }
    finally { writeGate.Release(); }
});
app.MapPut("/api/sitekit/records/{entity}/{id}", async (HttpContext context, string entity, string id, JsonElement body) =>
{
    var model = Find(entity); if (model is null || !Guid.TryParse(id, out _)) return Results.NotFound(); if (!Can(context, entity, "update")) return Results.StatusCode(StatusCodes.Status403Forbidden);
    await writeGate.WaitAsync();
    try
    {
        var rows = await List(entity); var index = rows.FindIndex(row => row.Id == id); if (index < 0) return Results.NotFound();
        var errors = await Validate(model, body, id); if (errors.Length > 0) return Problem(errors);
        var row = rows[index] with { Data = Normalize(model, body, id), UpdatedAt = DateTimeOffset.UtcNow };
        rows[index] = row; await Save(entity, rows); return Results.Ok(row);
    }
    finally { writeGate.Release(); }
});
app.MapDelete("/api/sitekit/records/{entity}/{id}", async (HttpContext context, string entity, string id) =>
{
    if (!Exists(entity) || !Guid.TryParse(id, out _)) return Results.NotFound(); if (!Can(context, entity, "delete")) return Results.StatusCode(StatusCodes.Status403Forbidden);
    await writeGate.WaitAsync();
    try { var rows = await List(entity); if (rows.RemoveAll(row => row.Id == id) == 0) return Results.NotFound(); await Save(entity, rows); return Results.NoContent(); }
    finally { writeGate.Release(); }
});
app.MapGet("/favicon.ico", () => Results.NoContent());
app.MapGet("/", () => Results.Content({{HTML}}, "text/html; charset=utf-8"));
app.Run();

Entity? Find(string alias) => Valid(alias) ? entities.FirstOrDefault(entity => entity.Alias == alias) : null;
bool Exists(string alias) => Find(alias) is not null;
bool Valid(string alias) => Regex.IsMatch(alias, "^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant);
bool IsAdmin(HttpContext context) => context.Items.TryGetValue("sitekit-is-admin", out var value) && value is true;
bool CanAnyRead(HttpContext context) => IsAdmin(context) ||
    (context.Items.TryGetValue("sitekit-principal", out var value) && value is string principal && grants.TryGetValue(principal, out var entityGrants) && entityGrants.Values.Any(operations => operations.Contains("read", StringComparer.Ordinal)));
bool Can(HttpContext context, string entity, string operation) => IsAdmin(context) ||
    (context.Items.TryGetValue("sitekit-principal", out var value) && value is string principal && grants.TryGetValue(principal, out var entityGrants) && entityGrants.TryGetValue(entity, out var operations) && operations.Contains(operation, StringComparer.Ordinal));
bool FixedEquals(string expected, string supplied) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));
Dictionary<string, string> LoadPrincipals()
{
    var raw = Environment.GetEnvironmentVariable("SITEKIT_PRINCIPAL_TOKENS");
    var parsed = string.IsNullOrWhiteSpace(raw) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(raw, jsonOptions) ?? [];
    return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
}
Dictionary<string, Dictionary<string, string[]>> LoadGrants()
{
    var raw = Environment.GetEnvironmentVariable("SITEKIT_ENTITY_GRANTS");
    return string.IsNullOrWhiteSpace(raw) ? new(StringComparer.Ordinal) : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(raw, jsonOptions) ?? new(StringComparer.Ordinal);
}
string FileFor(string alias) => Path.Combine(root, alias + ".json");
async Task<List<Row>> List(string alias)
{
    await using var connection = new SqliteConnection(connectionString); await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT record_id, data_json, created_at, updated_at FROM sitekit_records WHERE entity_alias = $entity ORDER BY updated_at DESC";
    command.Parameters.AddWithValue("$entity", alias);
    await using var reader = await command.ExecuteReaderAsync(); var records = new List<Row>();
    while (await reader.ReadAsync())
    {
        using var data = JsonDocument.Parse(reader.GetString(1));
        records.Add(new Row(reader.GetString(0), data.RootElement.Clone(), DateTimeOffset.Parse(reader.GetString(2)), DateTimeOffset.Parse(reader.GetString(3))));
    }
    return records;
}
async Task Save(string alias, List<Row> rows)
{
    await using var connection = new SqliteConnection(connectionString); await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    await using (var delete = connection.CreateCommand())
    {
        delete.Transaction = transaction; delete.CommandText = "DELETE FROM sitekit_records WHERE entity_alias = $entity"; delete.Parameters.AddWithValue("$entity", alias); await delete.ExecuteNonQueryAsync();
    }
    foreach (var row in rows)
    {
        await using var insert = connection.CreateCommand(); insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO sitekit_records (entity_alias, record_id, schema_version, data_json, created_at, updated_at) VALUES ($entity, $id, 1, $data, $created, $updated)";
        insert.Parameters.AddWithValue("$entity", alias); insert.Parameters.AddWithValue("$id", row.Id);
        insert.Parameters.AddWithValue("$data", JsonSerializer.Serialize(row.Data, jsonOptions));
        insert.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O")); insert.Parameters.AddWithValue("$updated", row.UpdatedAt.ToString("O"));
        await insert.ExecuteNonQueryAsync();
    }
    transaction.Commit();
}
async Task Migrate()
{
    await using var connection = new SqliteConnection(connectionString); await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "CREATE TABLE IF NOT EXISTS sitekit_migrations (version INTEGER PRIMARY KEY, name TEXT NOT NULL, applied_at TEXT NOT NULL); CREATE TABLE IF NOT EXISTS sitekit_records (entity_alias TEXT NOT NULL, record_id TEXT NOT NULL, schema_version INTEGER NOT NULL, data_json TEXT NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, PRIMARY KEY(entity_alias, record_id)); CREATE INDEX IF NOT EXISTS ix_sitekit_records_updated ON sitekit_records(entity_alias, updated_at); INSERT OR IGNORE INTO sitekit_migrations(version, name, applied_at) VALUES (1, 'initial-record-store', $applied);";
    command.Parameters.AddWithValue("$applied", DateTimeOffset.UtcNow.ToString("O")); await command.ExecuteNonQueryAsync();
}
JsonElement Normalize(Entity model, JsonElement body, string id) => JsonSerializer.SerializeToElement(model.Fields
    .Where(field => field.Alias == "id" || body.TryGetProperty(field.Alias, out _))
    .ToDictionary(field => field.Alias, field => field.Alias == "id" ? JsonSerializer.SerializeToElement(id) : body.GetProperty(field.Alias).Clone()), jsonOptions);
async Task<string[]> Validate(Entity model, JsonElement body, string? excludeId = null)
{
    if (body.ValueKind != JsonValueKind.Object) return ["A JSON object is required."];
    var errors = new List<string>(); var allowed = model.Fields.Select(field => field.Alias).ToHashSet(StringComparer.Ordinal);
    if (body.EnumerateObject().Any(property => !allowed.Contains(property.Name))) errors.Add("Input contains an unknown field.");
    var existing = await List(model.Alias);
    foreach (var field in model.Fields)
    {
        if (!body.TryGetProperty(field.Alias, out var value) || value.ValueKind == JsonValueKind.Null)
        { if (field.Alias != "id" && field.Required) errors.Add(field.Name + " is required."); continue; }
        if (field.Type is "text" or "email" && value.ValueKind != JsonValueKind.String) errors.Add(field.Name + " must be text.");
        if (field.Type == "number" && value.ValueKind != JsonValueKind.Number) errors.Add(field.Name + " must be a number.");
        if (field.Type == "boolean" && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) errors.Add(field.Name + " must be a boolean.");
        if (field.MaximumLength is int max && value.ValueKind == JsonValueKind.String && value.GetString()!.Length > max) errors.Add(field.Name + " is too long.");
        if (field.Unique && field.Alias != "id" && value.ValueKind != JsonValueKind.Null && existing.Any(row => row.Id != excludeId && row.Data.TryGetProperty(field.Alias, out var other) && JsonElement.DeepEquals(value, other))) errors.Add(field.Name + " must be unique.");
    }
    return errors.ToArray();
}
IResult Problem(string[] errors) => Results.ValidationProblem(errors.Select((error, index) => new { error, index }).ToDictionary(item => "error" + item.index, item => new[] { item.error }));
T Load<T>(string path, T fallback) => File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), jsonOptions) ?? fallback : fallback;
record Row(string Id, JsonElement Data, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
record Entity(string Alias, string Name, SiteKitField[] Fields);
record SiteKitField(string Alias, string Name, string Type, bool Required, bool Unique, int? MaximumLength);
record Screen(string Alias, string Name, string Type, string? EntityAlias, string[] Columns, string[] FormFields, string? GroupField);
""";
        return template.Replace("{{HTML}}", JsonSerializer.Serialize(html), StringComparison.Ordinal);
    }

    private static string BuildScreenRuntime() => """
(()=>{
  const esc=value=>String(value??'').replace(/[&<>"]/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[char]));
  let token=sessionStorage.getItem('sitekitToken')||'';
  async function request(url,options={}){options.headers={...options.headers,Authorization:`Bearer ${token}`};const response=await fetch(url,options);if(!response.ok)throw new Error(`Request failed (${response.status})`);return response.status===204?null:response.json()}
  function connect(){const host=document.getElementById('sitekit-screens');host.innerHTML='<form id="token-form"><label>Application access token <input name="token" type="password" autocomplete="current-password" required></label><button>Connect</button></form>';host.querySelector('form').onsubmit=async event=>{event.preventDefault();token=new FormData(event.currentTarget).get('token');sessionStorage.setItem('sitekitToken',token);await renderScreens()}}
  async function renderScreens(){
    const host=document.getElementById('sitekit-screens');
    try{
      const [screens,entities]=await Promise.all([request('/api/sitekit/screens'),request('/api/sitekit/entities')]);
      if(!screens.length){host.innerHTML='<p>No application screens are configured.</p>';return}
      host.innerHTML=screens.map(screen=>`<section class="screen"><h2>${esc(screen.name)}</h2><div id="screen-${esc(screen.alias)}">Loading…</div></section>`).join('');
      for(const screen of screens){
        const target=document.getElementById(`screen-${screen.alias}`);
        if(screen.type==='dashboard'){
          const stats=await Promise.all(entities.map(async entity=>({entity,count:(await request(`/api/sitekit/records/${encodeURIComponent(entity.alias)}`)).length})));
          target.innerHTML=`<div class="metrics">${stats.map(stat=>`<article><strong>${stat.count}</strong><span>${esc(stat.entity.name)}</span></article>`).join('')}</div>`;
          continue;
        }
        if(!screen.entityAlias){target.innerHTML='<p>This screen does not yet have an entity binding.</p>';continue}
        const model=entities.find(entity=>entity.alias===screen.entityAlias);
        if(!model){target.innerHTML='<p>You do not have read access to this entity.</p>';continue}
        const records=await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}`);
        const columns=screen.columns?.length?screen.columns:screen.formFields;
        const fields=(screen.formFields||[]).map(alias=>model.fields.find(field=>field.alias===alias)).filter(Boolean);
        const form=`<form class="form">${fields.map(field=>`<label>${esc(field.name)}<input name="${esc(field.alias)}" type="${field.type==='number'?'number':field.type==='date'?'date':field.type==='email'?'email':'text'}" ${field.required?'required':''}></label>`).join('')}<button>Create</button></form>`;
        if(screen.type==='kanban'&&screen.groupField){
          const lanes=[...new Set(records.map(record=>String(record.data[screen.groupField]||'Backlog')))];
          if(!lanes.length)lanes.push('Backlog');
          target.innerHTML=form+`<div class="kanban">${lanes.map(lane=>`<section><h3>${esc(lane)}</h3>${records.filter(record=>String(record.data[screen.groupField]||'Backlog')===lane).map(record=>`<article><strong>${esc(record.data[columns[0]]||record.id)}</strong><p>${columns.slice(1).map(field=>`${esc(field)}: ${esc(record.data[field])}`).join('<br>')}</p><label>Move to<select data-move="${esc(record.id)}">${lanes.map(option=>`<option ${option===lane?'selected':''}>${esc(option)}</option>`).join('')}</select></label><button data-delete="${esc(record.id)}">Delete</button></article>`).join('')}</section>`).join('')}</div>`;
          target.querySelectorAll('[data-move]').forEach(select=>select.onchange=async()=>{const record=records.find(item=>item.id===select.dataset.move);const data={...record.data,[screen.groupField]:select.value};await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}/${encodeURIComponent(record.id)}`,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});await renderScreens()});
        }else if(screen.type==='calendar'){
          const dateField=model.fields.find(field=>field.type==='date'||field.type==='datetime');
          target.innerHTML=form+`<ol>${records.map(record=>`<li>${esc(dateField?record.data[dateField.alias]:'No date set')} · ${esc(record.data[columns[0]]||record.id)}</li>`).join('')}</ol>`;
        }else{
          target.innerHTML=form+`<input aria-label="Search records" data-search placeholder="Search records"><table><thead><tr>${columns.map(field=>`<th>${esc(field)}</th>`).join('')}<th>Actions</th></tr></thead><tbody>${records.map(record=>`<tr>${columns.map(field=>`<td>${esc(record.data[field])}</td>`).join('')}<td><button data-delete="${esc(record.id)}">Delete</button></td></tr>`).join('')}</tbody></table>`;
          const search=target.querySelector('[data-search]');if(screen.type==='search')search.oninput=()=>target.querySelectorAll('tbody tr').forEach(row=>row.hidden=!row.textContent.toLowerCase().includes(search.value.toLowerCase()));
        }
        target.querySelector('form').onsubmit=async event=>{event.preventDefault();const data=Object.fromEntries(new FormData(event.currentTarget));for(const field of fields)if(field.type==='number'&&data[field.alias]!==undefined)data[field.alias]=Number(data[field.alias]);await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});await renderScreens()};
        target.querySelectorAll('[data-delete]').forEach(button=>button.onclick=async()=>{await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}/${encodeURIComponent(button.dataset.delete)}`,{method:'DELETE'});await renderScreens()});
      }
    }catch(error){host.innerHTML=`<p>${esc(error.message)} <button id="reconnect">Reconnect</button></p>`;host.querySelector('button').onclick=connect}
  }
  function start(){if(token)renderScreens();else connect()}
  window.SiteKit={start};
})();
""";
}

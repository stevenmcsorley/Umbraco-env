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
        {
            var availableScreens = await _screens.ListAsync(projectId, cancellationToken);
            var orderedScreens = manifest.Screens.Select(alias => availableScreens.FirstOrDefault(screen => screen.Alias == alias)
                ?? throw new InvalidOperationException($"Project screen '{alias}' was not found in the screen registry.")).ToArray();
            await WriteAtomicAsync(Path.Combine(destination, "sitekit.screens.json"), JsonSerializer.Serialize(orderedScreens, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), cancellationToken);
        }
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
            ? "\n\nApplication API access requires bearer tokens configured through your deployment secret store. For a single administrator set `SITEKIT_ADMIN_TOKEN`. For entity-level grants set `SITEKIT_PRINCIPAL_TOKENS` to a JSON map of SHA-256 token hashes to principal names, optional `SITEKIT_PRINCIPAL_ROLES` to map principal names to role aliases, and `SITEKIT_ENTITY_GRANTS` to operation grants (`read`, `create`, `update`, `delete`) keyed by principal or `role:<alias>`. A principal receives the union of its direct grants and grants for its assigned roles. Records persist in SQLite at `App_Data/sitekit.db`. Integrate a production identity provider before exposing the application to end users, and back up the SQLite database as part of deployment.\n"
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
        var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(manifest.Name)}</title><style>:root{{--primary:{manifest.ThemeSettings.PrimaryColor};--surface:{manifest.ThemeSettings.SurfaceColor};--text:{manifest.ThemeSettings.TextColor}}}*{{box-sizing:border-box}}body{{margin:0;background:linear-gradient(145deg,#f4f7fb,var(--surface) 45%,#eef3f8);color:var(--text);font:16px/1.6 {fontFamily};min-height:100vh}}main{{max-width:1160px;margin:auto;padding:clamp(2rem,7vw,6rem) 2rem}}main>small{{display:inline-flex;padding:.4rem .7rem;border-radius:999px;background:#e7f4f2;color:#12685f;letter-spacing:.12em;font-size:.7rem;font-weight:800}}h1{{font-size:clamp(2.8rem,7vw,5.4rem);letter-spacing:-.055em;line-height:1.02;margin:1.4rem 0 .8rem}}main>p{{max-width:700px;color:#59687b;font-size:1.1rem}}.sitekit-components{{display:grid;gap:1.25rem;margin-top:2.5rem}}.sitekit-component{{display:block;overflow:hidden}}.sitekit-card{{border:1px solid #ffffffb8;box-shadow:0 24px 70px #172b4d18;border-radius:24px}}.sitekit-component>.sitekit-card{{border:0;box-shadow:none;border-radius:inherit}}.sitekit-section{{padding:clamp(1.5rem,5vw,3.5rem)}}.sitekit-heading{{font-size:clamp(2.1rem,5vw,3.8rem);letter-spacing:-.045em;line-height:1.04;max-width:17ch;margin:.6rem 0 1rem}}.sitekit-text{{color:inherit;max-width:62ch}}.sitekit-component>.sitekit-card>.sitekit-text:first-child{{color:var(--primary);font-size:.75rem;font-weight:800;letter-spacing:.13em}}.sitekit-grid{{display:grid;grid-template-columns:repeat(var(--sitekit-columns,2),minmax(0,1fr));gap:1rem}}.sitekit-button{{display:inline-flex;margin-top:1.1rem;padding:.8rem 1.1rem;border-radius:{manifest.ThemeSettings.Radius};background:var(--primary);color:#fff;text-decoration:none;font-weight:700;box-shadow:0 8px 20px color-mix(in srgb,var(--primary),transparent 72%)}}@media(max-width:640px){{main{{padding:2.5rem 1.1rem}}.sitekit-grid{{grid-template-columns:1fr}}}}</style><main><small>SITEKIT {WebUtility.HtmlEncode(manifest.Category.ToUpperInvariant())}</small><h1>{WebUtility.HtmlEncode(manifest.Name)}</h1><p>{WebUtility.HtmlEncode(manifest.Description)}</p><section class=\"sitekit-components\">{renderedComponents}</section></main></html>";
        var serializedHtml = JsonSerializer.Serialize(html);
        return "var app = WebApplication.Create(args);\napp.MapGet(\"/health\", () => Results.Ok(new { status = \"healthy\" }));\napp.MapGet(\"/favicon.ico\", () => Results.NoContent());\napp.MapGet(\"/\", () => Results.Content(" + serializedHtml + ", \"text/html; charset=utf-8\"));\napp.Run();\n";
    }

    private static string BuildApplicationProgram(SiteKitProjectManifest manifest, string renderedComponents)
    {
        var fontFamily = manifest.ThemeSettings.FontFamily switch { "serif" => "Georgia,serif", "sans-serif" => "Arial,sans-serif", "monospace" => "ui-monospace,monospace", _ => "system-ui,sans-serif" };
        var css = $$$"""
            :root{--primary:{{{manifest.ThemeSettings.PrimaryColor}}};--surface:{{{manifest.ThemeSettings.SurfaceColor}}};--text:{{{manifest.ThemeSettings.TextColor}}};color-scheme:light}
            *{box-sizing:border-box}
            body{margin:0;min-height:100vh;background:radial-gradient(ellipse at 80% 0%,#e8f0ff 0,transparent 42%),#f4f6fa;color:#172033;font:15px/1.55 {{{fontFamily}}}}
            main{max-width:1280px;margin:auto;padding:clamp(2rem,5vw,4.5rem) clamp(1.1rem,4vw,3.5rem)}
            main>small{display:inline-flex;align-items:center;padding:.4rem .75rem;border-radius:999px;background:#e6f4f1;color:#17685e;letter-spacing:.12em;font-size:.68rem;font-weight:800}
            h1{font-size:clamp(2.5rem,5vw,4.25rem);line-height:1.05;letter-spacing:-.055em;margin:1rem 0 .5rem}
            main>p{color:#647087;margin:.25rem 0 2rem;font-size:1.05rem}
            #sitekit-screens{display:grid;gap:1.4rem}
            .screen{margin:0;padding:1.5rem;border:1px solid #e2e8f0;border-radius:18px;background:#fff;box-shadow:0 14px 40px #1e293b0a;min-width:0}
            .screen h2{font-size:1.12rem;letter-spacing:-.02em;margin:0 0 1rem}
            .form{display:flex;align-items:end;gap:.8rem;flex-wrap:wrap;margin:0 0 1rem;padding:1rem;background:#f8fafc;border:1px solid #e8edf4;border-radius:12px}
            .form label,#token-form label{display:grid;gap:.35rem;color:#536176;font-size:.82rem;font-weight:650}
            input,select,button{font:inherit;border-radius:9px}
            input,select{min-height:42px;padding:.62rem .78rem;border:1px solid #d8e0eb;background:#fff;color:#172033;outline:none}
            input:focus,select:focus{border-color:#17867a;box-shadow:0 0 0 3px #17867a20}
            button{min-height:42px;padding:.62rem 1rem;border:1px solid #087f73;background:#087f73;color:#fff;font-weight:700;cursor:pointer;box-shadow:0 5px 12px #087f7325;transition:transform .15s,box-shadow .15s}
            button:hover{transform:translateY(-1px);box-shadow:0 8px 18px #087f7330}
            table{width:100%;border-collapse:separate;border-spacing:0;overflow:hidden;border:1px solid #e5eaf1;border-radius:12px}
            th,td{padding:.85rem 1rem;text-align:left;border-bottom:1px solid #edf0f5}
            th{background:#f8fafc;color:#69768a;text-transform:uppercase;font-size:.7rem;letter-spacing:.08em}
            tbody tr:last-child td{border-bottom:0}tbody tr:hover{background:#fbfcfe}
            td button{min-height:34px;padding:.4rem .7rem;background:#fff;color:#59677a;border-color:#dce3ed;box-shadow:none;font-size:.8rem}
            .kanban{display:grid;grid-auto-flow:column;grid-auto-columns:minmax(230px,1fr);gap:1rem;overflow-x:auto;padding:.1rem .1rem .7rem}
            .kanban>section{padding:.85rem;background:#f4f6fa;border:1px solid #e8edf3;border-radius:14px;min-height:190px}
            .kanban>section h3{margin:.15rem .2rem .8rem;color:#536176;font-size:.78rem;text-transform:uppercase;letter-spacing:.08em}
            .kanban article{padding:1rem;margin:.55rem 0;background:#fff;border:1px solid #e7ebf2;border-radius:12px;box-shadow:0 5px 15px #172b4d0a}
            .kanban article strong{display:block;font-size:.92rem;line-height:1.35}
            .kanban article p{color:#758196;font-size:.78rem;margin:.5rem 0}
            .kanban article label{display:inline-flex;align-items:center;gap:.4rem;color:#758196;font-size:.72rem}
            .kanban article select{min-height:32px;padding:.3rem;font-size:.75rem}.kanban article button{margin-left:.3rem}
            .metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:1rem}
            .metrics article{display:grid;gap:.3rem;padding:1.25rem;border:1px solid #e6ebf2;border-radius:14px;background:linear-gradient(145deg,#fff,#f7fafc);box-shadow:0 8px 24px #172b4d08}
            .metrics strong{font-size:2rem;letter-spacing:-.05em}.metrics span{color:#69768a;font-size:.82rem}
            .chart{margin:0;padding:1rem;border:1px solid #e6ebf2;border-radius:14px;background:#fff;overflow:auto}.chart svg{display:block;width:100%;min-width:360px;max-height:520px}.chart-label{font:12px system-ui,sans-serif;fill:#536176}.chart-value{font:12px system-ui,sans-serif;fill:#17212b}.chart-bar{fill:#087f73}.chart figcaption{color:#69768a;font-size:.82rem;margin:.4rem 0}.chart-data{margin-top:1rem}
            #token-form{display:flex;align-items:end;gap:.8rem;flex-wrap:wrap;padding:1.3rem;border:1px solid #e2e8f0;border-radius:14px;background:white;box-shadow:0 12px 32px #172b4d0a}
            @media(max-width:640px){.screen{padding:1rem}.form{align-items:stretch}.form label{flex:1 1 100%}.kanban{grid-auto-columns:minmax(82vw,1fr)}th,td{padding:.65rem .7rem}}
            """;
        var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{WebUtility.HtmlEncode(manifest.Name)}</title><style>{css}</style><main><small>SITEKIT {WebUtility.HtmlEncode(manifest.Category.ToUpperInvariant())}</small><h1>{WebUtility.HtmlEncode(manifest.Name)}</h1><p>{WebUtility.HtmlEncode(manifest.Description)}</p><section class=\"sitekit-components\">{renderedComponents}</section><div id=\"sitekit-screens\">Loading application screens…</div></main><script src=\"/sitekit-runtime.js\"></script><script>SiteKit.start();</script></html>";
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
var principalRoles = LoadPrincipalRoles();
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
    try { var rows = await List(entity); if (rows.All(row => row.Id != id)) return Results.NotFound(); if (await IsReferenced(entity, id)) return Results.Conflict(new { error = "The record is referenced by another entity." }); rows.RemoveAll(row => row.Id == id); await Save(entity, rows); return Results.NoContent(); }
    finally { writeGate.Release(); }
});
app.MapGet("/favicon.ico", () => Results.NoContent());
app.MapGet("/", () => Results.Content({{HTML}}, "text/html; charset=utf-8"));
app.Run();

Entity? Find(string alias) => Valid(alias) ? entities.FirstOrDefault(entity => entity.Alias == alias) : null;
bool Exists(string alias) => Find(alias) is not null;
async Task<bool> IsReferenced(string alias, string id)
{
    foreach (var entity in entities)
        foreach (var field in entity.Fields.Where(field => field.RelatedEntity == alias))
            foreach (var row in await List(entity.Alias))
                if (row.Data.TryGetProperty(field.Alias, out var value) && (value.ValueKind == JsonValueKind.String && value.GetString() == id || value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == id))) return true;
    return false;
}
bool Valid(string alias) => Regex.IsMatch(alias, "^[a-z][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant);
bool IsAdmin(HttpContext context) => context.Items.TryGetValue("sitekit-is-admin", out var value) && value is true;
string[] PrincipalsFor(HttpContext context)
{
    if (!context.Items.TryGetValue("sitekit-principal", out var value) || value is not string principal) return [];
    var principals = new List<string> { principal };
    if (principalRoles.TryGetValue(principal, out var roles)) principals.AddRange(roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => "role:" + role));
    return principals.ToArray();
}
bool CanAnyRead(HttpContext context) => IsAdmin(context) || PrincipalsFor(context).Any(principal =>
    grants.TryGetValue(principal, out var entityGrants) && entityGrants.Values.Any(operations => operations.Contains("read", StringComparer.Ordinal)));
bool Can(HttpContext context, string entity, string operation) => IsAdmin(context) || PrincipalsFor(context).Any(principal =>
    grants.TryGetValue(principal, out var entityGrants) && entityGrants.TryGetValue(entity, out var operations) && operations.Contains(operation, StringComparer.Ordinal));
bool FixedEquals(string expected, string supplied) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));
Dictionary<string, string> LoadPrincipals()
{
    var raw = Environment.GetEnvironmentVariable("SITEKIT_PRINCIPAL_TOKENS");
    var parsed = string.IsNullOrWhiteSpace(raw) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(raw, jsonOptions) ?? [];
    return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
}
Dictionary<string, string[]> LoadPrincipalRoles()
{
    var raw = Environment.GetEnvironmentVariable("SITEKIT_PRINCIPAL_ROLES");
    var parsed = string.IsNullOrWhiteSpace(raw) ? new Dictionary<string, string[]>() : JsonSerializer.Deserialize<Dictionary<string, string[]>>(raw, jsonOptions) ?? [];
    return new Dictionary<string, string[]>(parsed.Where(pair => !string.IsNullOrWhiteSpace(pair.Key)), StringComparer.OrdinalIgnoreCase);
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
        if (field.Type is "relationship" or "relationships")
        {
            var target = field.RelatedEntity;
            if (target is null || !Exists(target)) { errors.Add(field.Name + " refers to an unknown entity."); continue; }
            var ids = field.Type == "relationship"
                ? value.ValueKind == JsonValueKind.String ? new[] { value.GetString()! } : []
                : value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String) ? value.EnumerateArray().Select(item => item.GetString()!).ToArray() : [];
            if ((field.Type == "relationship" && value.ValueKind != JsonValueKind.String) || (field.Type == "relationships" && value.ValueKind != JsonValueKind.Array)) { errors.Add(field.Name + " has an invalid relationship value."); continue; }
            var targetIds = (await List(target)).Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
            if (ids.Any(id => !targetIds.Contains(id))) errors.Add(field.Name + " refers to a record that does not exist.");
            continue;
        }
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
record SiteKitField(string Alias, string Name, string Type, bool Required, bool Unique, int? MaximumLength, string? RelatedEntity);
record Screen(string Alias, string Name, string Type, string? EntityAlias, string[] Columns, string[] FormFields, string? GroupField, string? ChartValueField);
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
        const controls=[];
        for(const field of fields){
          if(field.type==='relationship'||field.type==='relationships'){
            const relatedEntity=entities.find(entity=>entity.alias===field.relatedEntity);
            if(!relatedEntity){controls.push(`<label>${esc(field.name)}<span>You need read access to ${esc(field.relatedEntity)} records to select a value.</span></label>`);continue}
            const relatedRecords=await request(`/api/sitekit/records/${encodeURIComponent(field.relatedEntity)}`);
            const options=relatedRecords.map(record=>{const label=record.data.name??record.data.title??record.data.email??record.id;return `<option value="${esc(record.id)}">${esc(label)}</option>`}).join('');
            const placeholder=field.type==='relationship'?`<option value="">Select ${esc(field.name)}</option>`:'';
            controls.push(`<label>${esc(field.name)}<select name="${esc(field.alias)}" ${field.type==='relationships'?'multiple':''} ${field.required?'required':''}>${placeholder}${options}</select></label>`);
          }else controls.push(`<label>${esc(field.name)}<input name="${esc(field.alias)}" type="${field.type==='number'?'number':field.type==='date'?'date':field.type==='email'?'email':'text'}" ${field.required?'required':''}></label>`);
        }
        const form=`<form class="form"><input type="hidden" name="recordId"><div class="form-fields">${controls.join('')}</div><button type="submit">Create</button><button type="button" data-cancel hidden>Cancel edit</button></form>`;
        if(screen.type==='chart'&&screen.groupField){
          const totals=new Map();for(const record of records){const label=String(record.data[screen.groupField]??'Unassigned');const value=screen.chartValueField?Number(record.data[screen.chartValueField]||0):1;totals.set(label,(totals.get(label)||0)+(Number.isFinite(value)?value:0))}
          const rows=[...totals.entries()].sort((a,b)=>a[0].localeCompare(b[0])),max=Math.max(1,...rows.map(row=>row[1])),width=640,rowHeight=42,height=Math.max(110,rows.length*rowHeight+42);
          const bars=rows.map(([label,value],index)=>{const y=12+index*rowHeight,barWidth=Math.max(0,Math.round((width-220)*value/max));return `<text x="0" y="${y+20}" class="chart-label">${esc(label)}</text><rect x="170" y="${y}" width="${barWidth}" height="26" rx="6" class="chart-bar"></rect><text x="${180+barWidth}" y="${y+19}" class="chart-value">${esc(value)}</text>`}).join('');
          target.innerHTML=`<figure class="chart"><svg viewBox="0 0 ${width} ${height}" role="img" aria-labelledby="chart-title-${esc(screen.alias)}" preserveAspectRatio="xMinYMin meet"><title id="chart-title-${esc(screen.alias)}">${esc(screen.name)} by ${esc(screen.groupField)}</title>${bars}</svg><figcaption>${screen.chartValueField?`Total ${esc(screen.chartValueField)} by ${esc(screen.groupField)}`:`Records by ${esc(screen.groupField)}`}</figcaption><table class="chart-data"><caption>Chart values</caption><thead><tr><th scope="col">${esc(screen.groupField)}</th><th scope="col">${screen.chartValueField?`Total ${esc(screen.chartValueField)}`:'Records'}</th></tr></thead><tbody>${rows.map(([label,value])=>`<tr><th scope="row">${esc(label)}</th><td>${esc(value)}</td></tr>`).join('')||'<tr><td colspan="2">No data</td></tr>'}</tbody></table></figure>`;
          continue;
        }
        if(screen.type==='kanban'&&screen.groupField){
          const lanes=[...new Set(records.map(record=>String(record.data[screen.groupField]||'Backlog')))];
          if(!lanes.length)lanes.push('Backlog');
          target.innerHTML=form+`<div class="kanban">${lanes.map(lane=>`<section><h3>${esc(lane)}</h3>${records.filter(record=>String(record.data[screen.groupField]||'Backlog')===lane).map(record=>`<article><strong>${esc(record.data[columns[0]]||record.id)}</strong><p>${columns.slice(1).map(field=>`${esc(field)}: ${esc(record.data[field])}`).join('<br>')}</p><label>Move to<select data-move="${esc(record.id)}">${lanes.map(option=>`<option ${option===lane?'selected':''}>${esc(option)}</option>`).join('')}</select></label><button data-edit="${esc(record.id)}">Edit</button><button data-delete="${esc(record.id)}">Delete</button></article>`).join('')}</section>`).join('')}</div>`;
          target.querySelectorAll('[data-move]').forEach(select=>select.onchange=async()=>{const record=records.find(item=>item.id===select.dataset.move);const data={...record.data,[screen.groupField]:select.value};await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}/${encodeURIComponent(record.id)}`,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});await renderScreens()});
        }else if(screen.type==='calendar'){
          const dateField=model.fields.find(field=>field.type==='date'||field.type==='datetime');
          target.innerHTML=form+`<ol>${records.map(record=>`<li>${esc(dateField?record.data[dateField.alias]:'No date set')} · ${esc(record.data[columns[0]]||record.id)}</li>`).join('')}</ol>`;
        }else{
          target.innerHTML=form+`<input aria-label="Search records" data-search placeholder="Search records"><table><thead><tr>${columns.map(field=>`<th>${esc(field)}</th>`).join('')}<th>Actions</th></tr></thead><tbody>${records.map(record=>`<tr>${columns.map(field=>`<td>${esc(record.data[field])}</td>`).join('')}<td><button data-edit="${esc(record.id)}">Edit</button><button data-delete="${esc(record.id)}">Delete</button></td></tr>`).join('')}</tbody></table>`;
          const search=target.querySelector('[data-search]');if(screen.type==='search')search.oninput=()=>target.querySelectorAll('tbody tr').forEach(row=>row.hidden=!row.textContent.toLowerCase().includes(search.value.toLowerCase()));
        }
        const editor=target.querySelector('form');
        const submit=editor.querySelector('[type="submit"]');
        const cancel=editor.querySelector('[data-cancel]');
        target.querySelectorAll('[data-edit]').forEach(button=>button.onclick=()=>{const record=records.find(item=>item.id===button.dataset.edit);if(!record)return;editor.dataset.recordId=record.id;for(const field of fields){const input=editor.elements.namedItem(field.alias);if(!input)continue;if(field.type==='relationships'){const selected=new Set(record.data[field.alias]||[]);for(const option of input.options)option.selected=selected.has(option.value)}else input.value=record.data[field.alias]??''}submit.textContent='Save changes';cancel.hidden=false;editor.querySelector('[name="recordId"]').value=record.id});
        cancel.onclick=()=>{editor.reset();delete editor.dataset.recordId;submit.textContent='Create';cancel.hidden=true};
        editor.onsubmit=async event=>{event.preventDefault();const recordId=editor.dataset.recordId;const data=Object.fromEntries(new FormData(editor));delete data.recordId;for(const field of fields){const input=editor.elements.namedItem(field.alias);if(field.type==='number'&&data[field.alias]!==undefined)data[field.alias]=Number(data[field.alias]);if(field.type==='relationships'&&input)data[field.alias]=Array.from(input.selectedOptions,option=>option.value);if(field.type==='relationship'&&data[field.alias]==='')delete data[field.alias]}const url=`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}${recordId?`/${encodeURIComponent(recordId)}`:''}`;await request(url,{method:recordId?'PUT':'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});await renderScreens()};
        target.querySelectorAll('[data-delete]').forEach(button=>button.onclick=async()=>{await request(`/api/sitekit/records/${encodeURIComponent(screen.entityAlias)}/${encodeURIComponent(button.dataset.delete)}`,{method:'DELETE'});await renderScreens()});
      }
    }catch(error){host.innerHTML=`<p>${esc(error.message)} <button id="reconnect">Reconnect</button></p>`;host.querySelector('button').onclick=connect}
  }
  function start(){if(token)renderScreens();else connect()}
  window.SiteKit={start};
})();
""";
}

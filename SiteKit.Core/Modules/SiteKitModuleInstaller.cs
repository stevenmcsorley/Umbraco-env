using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using SiteKit.Core.Components;
using SiteKit.Core.Data;
using SiteKit.Core.Projects;

namespace SiteKit.Core.Modules;

/// <summary>Installs verified declarative modules into one project without executing package code.</summary>
public sealed class SiteKitModuleInstaller
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly string _packageRoot;
    private readonly ISiteKitModuleRegistry _modules;
    private readonly ISiteKitProjectRegistry _projects;
    private readonly ISiteKitComponentRegistry _components;
    private readonly ISiteKitEntityRegistry _entities;
    private readonly ISiteKitScreenRegistry _screens;

    public SiteKitModuleInstaller(string packageRoot, ISiteKitModuleRegistry modules, ISiteKitProjectRegistry projects, ISiteKitComponentRegistry components, ISiteKitEntityRegistry entities, ISiteKitScreenRegistry screens)
    { _packageRoot = Path.GetFullPath(packageRoot); _modules = modules; _projects = projects; _components = components; _entities = entities; _screens = screens; }

    public Task<SiteKitProjectManifest> InstallAsync(string projectId, string moduleId, string version, CancellationToken cancellationToken = default)
        => InstallInternalAsync(projectId, moduleId, version, false, cancellationToken);

    public Task<SiteKitProjectManifest> UpgradeAsync(string projectId, string moduleId, string version, CancellationToken cancellationToken = default)
        => InstallInternalAsync(projectId, moduleId, version, true, cancellationToken);

    /// <summary>Removes a module pin while preserving project assets and records created from the package.</summary>
    public async Task<SiteKitProjectManifest> UninstallAsync(string projectId, string moduleId, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        if (!project.Modules.Contains(moduleId, StringComparer.Ordinal))
            throw new InvalidOperationException($"Module '{moduleId}' is not installed in this project.");

        foreach (var installedId in project.Modules.Where(id => !string.Equals(id, moduleId, StringComparison.Ordinal)))
        {
            if (project.ModuleVersions is null || !project.ModuleVersions.TryGetValue(installedId, out var installedVersion))
                throw new InvalidOperationException($"Cannot safely remove '{moduleId}' because installed module '{installedId}' has no version pin to verify its dependencies.");
            var installedModule = await _modules.GetAsync(installedId, installedVersion, cancellationToken)
                ?? throw new InvalidOperationException($"Cannot safely remove '{moduleId}' because installed module '{installedId}' v{installedVersion} is unavailable for dependency validation.");
            if (installedModule.Dependencies.Contains(moduleId, StringComparer.Ordinal))
                throw new InvalidOperationException($"Module '{installedId}' depends on '{moduleId}' and must be removed first.");
        }

        var versions = new Dictionary<string, string>(project.ModuleVersions ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        versions.Remove(moduleId);
        return await _projects.SaveAsync(project with
        {
            Modules = project.Modules.Where(id => !string.Equals(id, moduleId, StringComparison.Ordinal)).ToArray(),
            ModuleVersions = versions
        }, cancellationToken);
    }

    private async Task<SiteKitProjectManifest> InstallInternalAsync(string projectId, string moduleId, string version, bool allowUpgrade, CancellationToken cancellationToken)
    {
        var project = await _projects.GetAsync(projectId, cancellationToken) ?? throw new InvalidOperationException("Project not found.");
        var module = await _modules.GetAsync(moduleId, version, cancellationToken) ?? throw new InvalidOperationException("Module version is not installed in the local registry.");
        var alreadyInstalled = project.Modules.Contains(module.Id, StringComparer.Ordinal);
        var hasVersionPin = (project.ModuleVersions ?? new Dictionary<string, string>()).TryGetValue(module.Id, out var installedVersion);
        if (alreadyInstalled && !hasVersionPin && !allowUpgrade)
            throw new InvalidOperationException($"Module '{module.Id}' is installed without a version pin; use the explicit upgrade operation to pin {module.Version}.");
        if (hasVersionPin)
        {
            if (string.IsNullOrWhiteSpace(installedVersion)) throw new InvalidDataException($"Project module version pin for '{module.Id}' is empty.");
            if (!string.Equals(installedVersion, module.Version, StringComparison.Ordinal))
            {
                if (!allowUpgrade) throw new InvalidOperationException($"Module '{module.Id}' is pinned to {installedVersion}; use the explicit upgrade operation to install {module.Version}.");
                if (SiteKitModuleVersion.Compare(module.Version, installedVersion) <= 0) throw new InvalidOperationException($"Module upgrade must move '{module.Id}' forward from {installedVersion}; downgrades are not allowed.");
            }
        }
        var manifestErrors = JsonSiteKitModuleRegistry.ValidateManifest(module);
        if (manifestErrors.Count > 0) throw new InvalidDataException(string.Join(" ", manifestErrors));
        if (module.ProjectCategories.Length > 0 && !module.ProjectCategories.Contains(project.Category, StringComparer.Ordinal))
            throw new InvalidOperationException($"Module '{module.Id}' is not compatible with project category '{project.Category}'.");
        foreach (var dependency in module.Dependencies)
        {
            if (!project.Modules.Contains(dependency, StringComparer.Ordinal)) throw new InvalidOperationException($"Required module '{dependency}' must be installed first.");
            if (module.DependencyVersions.TryGetValue(dependency, out var requiredRange))
            {
                if (project.ModuleVersions is null || !project.ModuleVersions.TryGetValue(dependency, out var installedDependencyVersion))
                    throw new InvalidOperationException($"Required module '{dependency}' must have a pinned version to validate range '{requiredRange}'.");
                if (!SiteKitModuleVersion.Satisfies(installedDependencyVersion, requiredRange))
                    throw new InvalidOperationException($"Installed module '{dependency}' v{installedDependencyVersion} does not satisfy required range '{requiredRange}'.");
            }
        }
        if (allowUpgrade)
        {
            foreach (var installedId in project.Modules.Where(id => id != module.Id))
            {
                if (project.ModuleVersions is null || !project.ModuleVersions.TryGetValue(installedId, out var dependentVersion)) continue;
                var installedModule = await _modules.GetAsync(installedId, dependentVersion, cancellationToken);
                if (installedModule?.DependencyVersions.TryGetValue(module.Id, out var dependentRange) == true &&
                    !SiteKitModuleVersion.Satisfies(module.Version, dependentRange))
                    throw new InvalidOperationException($"Upgrade to {module.Version} violates dependent module '{installedId}' range '{dependentRange}'.");
            }
        }

        var directory = Path.GetFullPath(Path.Combine(_packageRoot, module.Id, module.Version));
        var root = Path.GetFullPath(_packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!directory.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Module package path is invalid.");
        var installedComponents = new List<string>();
        var installedEntities = new List<string>();
        var installedScreens = new List<SiteKitApplicationScreenDefinition>();
        var definitions = new List<SiteKitComponentDefinition>();
        var entityDefinitions = new List<SiteKitEntityDefinition>();
        var integrityInput = new StringBuilder();
        foreach (var relative in module.PackageFiles.Keys.Order(StringComparer.Ordinal))
        {
            var path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!module.PackageFiles.TryGetValue(relative, out var expectedDigest) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(digest), Encoding.ASCII.GetBytes(expectedDigest.ToLowerInvariant())))
                throw new InvalidDataException($"Installed module file '{relative}' failed its SHA-256 check.");
            integrityInput.Append(relative).Append('\n').Append(digest).Append('\n');
            if (relative.StartsWith("components/", StringComparison.Ordinal) && relative.EndsWith(".json", StringComparison.Ordinal))
            {
                var definition = JsonSerializer.Deserialize<SiteKitComponentDefinition>(bytes, Options) ?? throw new InvalidDataException($"Module component '{relative}' is invalid.");
                if (SiteKitComponentValidator.ValidateDefinition(definition).Count > 0) throw new InvalidDataException($"Module component '{relative}' failed validation.");
                definitions.Add(definition);
            }
            if (relative.StartsWith("entities/", StringComparison.Ordinal) && relative.EndsWith(".json", StringComparison.Ordinal))
            {
                var definition = JsonSerializer.Deserialize<SiteKitEntityDefinition>(bytes, Options) ?? throw new InvalidDataException($"Module entity '{relative}' is invalid.");
                definition = definition with { ProjectId = projectId };
                if (SiteKitEntityValidator.Validate(definition).Count > 0) throw new InvalidDataException($"Module entity '{relative}' failed validation.");
                entityDefinitions.Add(definition);
            }
            if (relative.StartsWith("screens/", StringComparison.Ordinal) && relative.EndsWith(".json", StringComparison.Ordinal))
            {
                var definition = JsonSerializer.Deserialize<SiteKitApplicationScreenDefinition>(bytes, Options) ?? throw new InvalidDataException($"Module screen '{relative}' is invalid.");
                definition = definition with { ProjectId = projectId };
                if (SiteKitScreenValidator.Validate(definition).Count > 0) throw new InvalidDataException($"Module screen '{relative}' failed validation.");
                installedScreens.Add(definition);
            }
        }
        var expectedIntegrity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(integrityInput.ToString()))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expectedIntegrity), Encoding.ASCII.GetBytes(module.IntegritySha256.ToLowerInvariant())))
            throw new InvalidDataException("Installed module integrity check failed.");
        var newComponentAliases = definitions.Select(definition => definition.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var definition in definitions)
            foreach (var dependency in definition.Dependencies)
                if (!newComponentAliases.Contains(dependency) && await _components.GetAsync(dependency, cancellationToken: cancellationToken) is null)
                    throw new InvalidOperationException($"Component dependency '{dependency}' is not installed.");
        var newEntityAliases = entityDefinitions.Select(definition => definition.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var definition in entityDefinitions)
            foreach (var field in definition.Fields.Where(field => field.Type is "relationship" or "relationships" && SiteKitProjectValidator.IsValidId(field.RelatedEntity)))
                if (!newEntityAliases.Contains(field.RelatedEntity!) && await _entities.GetAsync(projectId, field.RelatedEntity!, cancellationToken) is null)
                    throw new InvalidOperationException($"Entity dependency '{field.RelatedEntity}' is not installed in the project.");

        foreach (var definition in definitions)
        {
            var installed = await _components.GetAsync(definition.Alias, definition.Version, cancellationToken);
            if (installed is not null && !string.Equals(JsonSerializer.Serialize(installed with { CreatedAt = definition.CreatedAt, UpdatedAt = definition.UpdatedAt }, Options), JsonSerializer.Serialize(definition, Options), StringComparison.Ordinal))
                throw new InvalidOperationException($"Component '{definition.Alias}' v{definition.Version} already exists with different content.");
        }
        var localEntityAliases = entityDefinitions.Select(definition => definition.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var definition in installedScreens)
        {
            if (definition.EntityAlias is not null && !localEntityAliases.Contains(definition.EntityAlias) && await _entities.GetAsync(projectId, definition.EntityAlias, cancellationToken) is null)
                throw new InvalidOperationException($"Screen entity '{definition.EntityAlias}' is not installed.");
            foreach (var component in definition.Components)
                if (await _components.GetAsync(component, cancellationToken: cancellationToken) is null && definitions.All(candidate => candidate.Alias != component))
                    throw new InvalidOperationException($"Screen component '{component}' is not installed.");
        }
        foreach (var definition in entityDefinitions)
        {
            var installed = await _entities.GetAsync(projectId, definition.Alias, cancellationToken);
            if (installed is not null && definition.Version < installed.Version)
                throw new InvalidOperationException($"Entity '{definition.Alias}' package schema is older than the installed project schema.");
            if (installed is not null && definition.Version == installed.Version &&
                !string.Equals(JsonSerializer.Serialize(installed with { ProjectId = definition.ProjectId, UpdatedAt = definition.UpdatedAt }, Options), JsonSerializer.Serialize(definition, Options), StringComparison.Ordinal))
                throw new InvalidOperationException($"Entity '{definition.Alias}' v{definition.Version} already exists with different content.");
        }
        foreach (var screen in installedScreens)
        {
            var entity = screen.EntityAlias is null ? null : entityDefinitions.FirstOrDefault(item => item.Alias == screen.EntityAlias) ?? await _entities.GetAsync(projectId, screen.EntityAlias, cancellationToken);
            if (SiteKitScreenValidator.Validate(screen, entity).Count > 0)
                throw new InvalidOperationException($"Screen '{screen.Alias}' does not match its installed entity schema.");
        }

        installedComponents.AddRange(definitions.Select(definition => definition.Alias));
        installedEntities.AddRange(entityDefinitions.Select(definition => definition.Alias));
        var updated = project with
        {
            Modules = project.Modules.Contains(module.Id, StringComparer.Ordinal) ? project.Modules : project.Modules.Append(module.Id).ToArray(),
            ModuleVersions = new Dictionary<string, string>(project.ModuleVersions ?? new Dictionary<string, string>(), StringComparer.Ordinal) { [module.Id] = module.Version },
            Components = project.Components.Concat(installedComponents).Distinct(StringComparer.Ordinal).ToArray(),
            Entities = project.Entities.Concat(installedEntities).Distinct(StringComparer.Ordinal).ToArray(),
            Screens = project.Screens.Concat(installedScreens.Select(screen => screen.Alias)).Distinct(StringComparer.Ordinal).ToArray()
        };
        var errors = SiteKitProjectValidator.Validate(updated);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));

        foreach (var definition in definitions)
        {
            await _components.SaveAsync(definition, cancellationToken);
        }
        foreach (var definition in entityDefinitions)
        {
            await _entities.SaveAsync(definition, cancellationToken);
        }
        foreach (var screen in installedScreens) await _screens.SaveAsync(screen, cancellationToken);
        return await _projects.SaveAsync(updated, cancellationToken);
    }

}

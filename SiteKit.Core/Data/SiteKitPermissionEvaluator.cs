namespace SiteKit.Core.Data;

/// <summary>Combines direct principal grants with project-role grants.</summary>
public sealed class SiteKitPermissionEvaluator
{
    private readonly ISiteKitPermissionRegistry _permissions;
    private readonly ISiteKitProjectRoleRegistry _roles;

    public SiteKitPermissionEvaluator(ISiteKitPermissionRegistry permissions, ISiteKitProjectRoleRegistry roles)
    { _permissions = permissions; _roles = roles; }

    public async Task<IReadOnlySet<string>> GetOperationsAsync(string projectId, string entityAlias, string principal, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(principal)) return new HashSet<string>(StringComparer.Ordinal);
        var operations = new HashSet<string>(StringComparer.Ordinal);
        var direct = await _permissions.GetAsync(projectId, entityAlias, principal, cancellationToken);
        if (direct is not null) operations.UnionWith(direct.Operations);

        foreach (var role in await _roles.ListAsync(projectId, cancellationToken))
        {
            if (!role.Members.Contains(principal, StringComparer.OrdinalIgnoreCase)) continue;
            var grant = await _permissions.GetAsync(projectId, entityAlias, RolePrincipal(role.Alias), cancellationToken);
            if (grant is not null) operations.UnionWith(grant.Operations);
        }
        return operations;
    }

    public async Task<bool> CanAsync(string projectId, string entityAlias, string principal, string operation, CancellationToken cancellationToken = default)
    {
        var operations = await GetOperationsAsync(projectId, entityAlias, principal, cancellationToken);
        return operations.Contains(operation) || operations.Contains("manage");
    }

    public static string RolePrincipal(string roleAlias) => "role:" + roleAlias;

    public static bool TryGetRoleAlias(string principal, out string roleAlias)
    {
        const string prefix = "role:";
        roleAlias = principal.StartsWith(prefix, StringComparison.Ordinal) ? principal[prefix.Length..] : string.Empty;
        return roleAlias.Length > 0 && SiteKit.Core.Projects.SiteKitProjectValidator.IsValidId(roleAlias);
    }
}

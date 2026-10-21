namespace SiteKit.AgentHost;

public sealed record ChatGptAccount
{
    public required string Issuer { get; init; }
    public required string Subject { get; init; }
    public string? Email { get; init; }
    public required string ClientId { get; init; }
    public required string HostId { get; init; }
    public required string IdToken { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required string[] Scopes { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset SavedAt { get; init; }

    public bool HasPlanUsageConsent => Scopes.Contains("chatgpt.tokens.use.direct", StringComparer.Ordinal);
    public bool IsEligibleForPlanUsage => HasPlanUsageConsent &&
        Scopes.Contains("resource.invoke", StringComparer.Ordinal) && Scopes.Contains("offline_access", StringComparer.Ordinal);

    public static string[] ParseGrantedScopes(string? value)
        => (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).ToArray();
}

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Scoping;

namespace MyDockerProject.Composers;

public class OpenIddictRedirectUriComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Temporarily disabled - redirect URIs are set manually via SQL
        // builder.Components().Append<OpenIddictRedirectUriComponent>();
    }
}

public class OpenIddictRedirectUriComponent : Umbraco.Cms.Core.Composing.IAsyncComponent
{
    private readonly IScopeProvider _scopeProvider;
    private readonly ILogger<OpenIddictRedirectUriComponent> _logger;

    public OpenIddictRedirectUriComponent(IScopeProvider scopeProvider, ILogger<OpenIddictRedirectUriComponent> logger)
    {
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(bool isAfterRuntime, CancellationToken cancellationToken)
    {
        if (!isAfterRuntime)
        {
            // Only run after runtime is initialized
            return;
        }
        
        try
        {
            // Wait longer for Umbraco to fully initialize and set up OpenIddict
            await Task.Delay(15000, cancellationToken);

            // Include both HTTPS (for browser requests) and HTTP (for internal)
            // Browser requests come via HTTPS from Cloudflare, but app sees HTTP internally
            var redirectUris = new[]
            {
                "https://hotel.halfagiraf.com/umbraco/oauth_complete",
                "http://hotel.halfagiraf.com/umbraco/oauth_complete"
            };

            var postLogoutUris = new[]
            {
                "https://hotel.halfagiraf.com/umbraco/oauth_complete",
                "http://hotel.halfagiraf.com/umbraco/oauth_complete",
                "https://hotel.halfagiraf.com/umbraco/logout",
                "http://hotel.halfagiraf.com/umbraco/logout"
            };

            var redirectUrisJson = JsonSerializer.Serialize(redirectUris);
            var postLogoutUrisJson = JsonSerializer.Serialize(postLogoutUris);

            using var scope = _scopeProvider.CreateScope();
            var db = scope.Database;

            var sql = @"
                UPDATE umbracoOpenIddictApplications 
                SET RedirectUris = @redirectUris,
                    PostLogoutRedirectUris = @postLogoutUris
                WHERE ClientId = 'umbraco-back-office'";

            db.Execute(sql, new { redirectUris = redirectUrisJson, postLogoutUris = postLogoutUrisJson });
            scope.Complete();

            _logger.LogInformation("Updated OpenIddict redirect URIs to include HTTPS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update OpenIddict redirect URIs");
        }
    }

    public Task TerminateAsync(bool isBeforeRuntime, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Terminate()
    {
    }
}


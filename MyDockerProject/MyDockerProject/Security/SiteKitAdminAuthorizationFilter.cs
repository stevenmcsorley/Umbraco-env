using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyDockerProject.Security;

/// <summary>
/// Restricts the legacy hotel administration surface to signed-in customer
/// accounts explicitly allowlisted by the site operator.
/// </summary>
public sealed class SiteKitAdminAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration;

    public SiteKitAdminAuthorizationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var email = context.HttpContext.Session.GetString("UserEmail");
        var userId = context.HttpContext.Session.GetString("UserId");
        var configuredAdmins = _configuration["SiteKit:AdminEmails"] ?? string.Empty;
        var isAdministrator = Guid.TryParse(userId, out _) && !string.IsNullOrWhiteSpace(email) && configuredAdmins
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(email, StringComparer.OrdinalIgnoreCase);

        if (!isAdministrator)
        {
            context.Result = context.HttpContext.Request.Path.StartsWithSegments("/api")
                ? new UnauthorizedObjectResult(new { error = "Administrator sign-in required." })
                : new RedirectToActionResult("Login", "Auth", null);
        }

        return Task.CompletedTask;
    }
}

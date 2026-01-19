using Madtorio.Data;
using Microsoft.AspNetCore.Identity;

namespace Madtorio.Middleware;

/// <summary>
/// Middleware that enforces password change requirement for users with temporary passwords.
/// Users with RequirePasswordChange = true are redirected to the change password page
/// unless they are accessing allowed paths (logout, change password page, static files).
/// </summary>
public class PasswordChangeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PasswordChangeMiddleware> _logger;

    // Paths that are allowed even when password change is required
    private static readonly string[] AllowedPaths = new[]
    {
        "/Account/Logout",
        "/Account/Manage/ChangePassword",
        "/css/",
        "/js/",
        "/_framework/",
        "/lib/",
        ".json",
        ".css",
        ".js"
    };

    public PasswordChangeMiddleware(RequestDelegate next, ILogger<PasswordChangeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        // Check if path is allowed BEFORE checking authentication
        // This ensures logout can happen without password change requirement
        if (IsPathAllowed(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Only process authenticated users for password change requirement
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Get current user
        var user = await userManager.GetUserAsync(context.User);
        if (user == null)
        {
            await _next(context);
            return;
        }

        // Check if password change is required
        if (user.RequirePasswordChange)
        {
            _logger.LogInformation("Redirecting user {UserId} to change password", user.Id);
            context.Response.Redirect("/Account/Manage/ChangePassword");
            return;
        }

        await _next(context);
    }

    private static bool IsPathAllowed(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? string.Empty;

        // Check against allowed paths
        foreach (var allowedPath in AllowedPaths)
        {
            if (pathValue.StartsWith(allowedPath.ToLower()))
            {
                return true;
            }
        }

        return false;
    }
}

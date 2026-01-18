using System.Security.Claims;
using Madtorio.Services;

namespace Madtorio.Middleware;

public class PageViewMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PageViewMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PageViewMiddleware(
        RequestDelegate next,
        ILogger<PageViewMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Track page views for specific routes only (GDPR-compliant - no PII)
            var path = context.Request.Path.Value?.ToLower();
            var trackedPaths = new[] { "/", "/downloads", "/rules", "/server-info" };

            if (!string.IsNullOrEmpty(path) && trackedPaths.Contains(path))
            {
                var userId = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    : null;

                // Log page view in background with its own scope to avoid DbContext threading issues
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
                        await statisticsService.LogPageViewAsync(path, userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error logging page view in background task");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PageViewMiddleware");
        }

        await _next(context);
    }
}

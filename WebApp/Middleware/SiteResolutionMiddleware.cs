using SiteEngine.Sites;
using SiteEngine.Services;

namespace WebApp.Middleware;

public class SiteResolutionMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;

	public async Task InvokeAsync(HttpContext context, ISiteContext siteContext, IPlatformConfigurationService platformConfigurationService)
	{
		if (await platformConfigurationService.IsInitialSetupRequiredAsync(context.RequestAborted)
			&& !IsSetupBypassPath(context.Request.Path))
		{
			context.Items["SetupMode"] = true;
			return;
		}

		await siteContext.InitializeAsync(context.Request.Host.Host, context.RequestAborted);
		await _next(context);
	}

	private static bool IsSetupBypassPath(PathString path)
	{
		return path.StartsWithSegments("/setup", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/images", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/site-data", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
			|| path.StartsWithSegments("/favicon", StringComparison.OrdinalIgnoreCase);
	}
}

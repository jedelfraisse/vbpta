using SiteEngine.Sites;

namespace WebApp.Middleware;

public class SiteResolutionMiddleware(RequestDelegate next)
{
	private readonly RequestDelegate _next = next;

	public async Task InvokeAsync(HttpContext context, ISiteContext siteContext)
	{
		await siteContext.InitializeAsync(context.Request.Host.Host, context.RequestAborted);
		await _next(context);
	}
}

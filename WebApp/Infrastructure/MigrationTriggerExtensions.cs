using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;

namespace WebApp.Infrastructure;

public static class MigrationTriggerExtensions
{
	public static async Task RunPendingMigrationsIfRequestedAsync(this WebApplication app)
	{
		var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
		var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationTrigger");
		await MigrationTriggerProcessor.ProcessAsync(
			webRootPath,
			async () =>
			{
				using var scope = app.Services.CreateScope();
				var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
				await dbContext.Database.MigrateAsync();
			},
			logger);
	}
}

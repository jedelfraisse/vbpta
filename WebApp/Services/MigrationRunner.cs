using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;
using SiteEngine.Identity;

namespace WebApp.Services;

public static class MigrationRunner
{
	public static async Task RunAsync(IServiceProvider services, string? logFile)
	{
		void Log(string message)
		{
			if (logFile == null) return;
			File.AppendAllText(logFile, message + "\n");
		}

		try
		{
			using var scope = services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			Log($"=== Migration Start: {DateTime.Now} ===");

			// Test connection
			if (!db.Database.CanConnect())
			{
				Log("✗ Cannot connect to database.");
				throw new Exception("Database connection failed.");
			}

			Log("✓ Database connection successful.");

			// Run migrations
			Log("Running migrations...");
			await db.Database.MigrateAsync();
			Log("✓ Migrations completed.");

			// Ensure PortalConfig exists
			var config = await db.PortalConfigs.FindAsync(SeedData.DefaultGlobalConfigId);
			if (config == null)
			{
				config = SeedData.DefaultGlobalConfig;
				db.PortalConfigs.Add(config);
				Log("✓ PortalConfig created.");
			}

			// Ensure Portal Site exists
			var portalSite = await db.Sites.FindAsync(SeedData.DefaultPortalSiteId);
			if (portalSite == null)
			{
				portalSite = SeedData.DefaultPortalSite;
				db.Sites.Add(portalSite);
				Log("✓ Portal site created.");
			}

			await db.SaveChangesAsync();
			Log("✓ Seeding completed.");

			Log($"=== Migration Success: {DateTime.Now} ===");
		}
		catch (Exception ex)
		{
			if (logFile != null)
			{
				File.AppendAllText(logFile, $"✗ Migration failed: {ex.Message}\n{ex.StackTrace}\n");
			}
			throw;
		}
	}
}

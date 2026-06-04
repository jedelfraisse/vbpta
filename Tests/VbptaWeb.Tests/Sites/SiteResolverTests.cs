using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Options;
using SiteEngine.Sites;
using VbptaWeb.Tests.Support;

namespace VbptaWeb.Tests.Sites;

public class SiteResolverTests
{
	[Fact]
	public async Task ResolveAsync_UsesHostMappingAndReturnsAdminContext()
	{
		var optionsBuilder = CreateDbContextOptions();
		await using (var seedDb = new AppDbContext(optionsBuilder.Options))
		{
			await seedDb.Database.EnsureCreatedAsync();
		}

		var options = Options.Create(new SiteHostMappingOptions
		{
			Hosts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["admin.vbpta.delfraisse.com"] = "admin.localhost"
			}
		});
		var resolver = new SiteResolver(
			new TestDbContextFactory(optionsBuilder.Options),
			new MemoryCache(new MemoryCacheOptions()),
			new TestHostEnvironment { EnvironmentName = Environments.Production },
			options);

		var result = await resolver.ResolveAsync("admin.vbpta.delfraisse.com");

		Assert.NotNull(result);
		Assert.True(result!.IsAdminContext);
		Assert.Equal("admin.localhost", result.Site.Hostname);
	}

	private static DbContextOptionsBuilder<AppDbContext> CreateDbContextOptions()
	{
		var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
		return new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite($"Data Source={dbPath}");
	}

	private sealed class TestHostEnvironment : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Production;
		public string ApplicationName { get; set; } = "VbptaWeb.Tests";
		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
		public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
			new Microsoft.Extensions.FileProviders.NullFileProvider();
	}
}

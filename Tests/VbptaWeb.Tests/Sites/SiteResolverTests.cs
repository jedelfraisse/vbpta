using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Options;
using SiteEngine.Sites;

namespace VbptaWeb.Tests.Sites;

public class SiteResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesHostMappingAndReturnsAdminContext()
    {
        var optionsBuilder = await CreateInitializedDbContextOptionsAsync();
        await using var db = new AppDbContext(optionsBuilder.Options);

        var options = Options.Create(new SiteHostMappingOptions
        {
            Hosts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin.vbpta.delfraisse.com"] = "admin.localhost"
            }
        });

        var resolver = CreateResolver(db, options);

        var result = await resolver.ResolveAsync("admin.vbpta.delfraisse.com");

        Assert.NotNull(result);
        Assert.True(result!.IsAdminContext);
        Assert.Equal("admin", result.Site.Hostname);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsCityWideForRootDomain()
    {
        var optionsBuilder = await CreateInitializedDbContextOptionsAsync();
        await using var db = new AppDbContext(optionsBuilder.Options);

        var resolver = CreateResolver(db, Options.Create(new SiteHostMappingOptions()));

        var result = await resolver.ResolveAsync("localhost");

        Assert.NotNull(result);
        Assert.True(result!.Site.IsCityWide);
        Assert.False(result.SiteNotFound);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsSubdomainSite_WhenHostnameMatches()
    {
        var optionsBuilder = await CreateInitializedDbContextOptionsAsync(async db =>
        {
            db.Sites.Add(new Site
            {
                Id = Guid.NewGuid(),
                PtaId = "12345678",
                Hostname = "luxfordes",
                Domain = string.Empty,
                IsAdminPortal = false,
                IsCityWide = false,
                SiteName = "Luxford",
                LogoUrl = "images/logo.png",
                BannerUrl = "images/banner.png",
                PrimaryColor = "#003366",
                AccentColor = "#FFCC00",
                WelcomeText = "Welcome",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        await using var db = new AppDbContext(optionsBuilder.Options);
        var resolver = CreateResolver(db, Options.Create(new SiteHostMappingOptions()));

        var result = await resolver.ResolveAsync("luxfordes.localhost");

        Assert.NotNull(result);
        Assert.Equal("12345678", result!.Site.PtaId);
        Assert.False(result.SiteNotFound);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackToCityWide_WhenSubdomainMissing()
    {
        var optionsBuilder = await CreateInitializedDbContextOptionsAsync();
        await using var db = new AppDbContext(optionsBuilder.Options);

        var resolver = CreateResolver(db, Options.Create(new SiteHostMappingOptions()));

        var result = await resolver.ResolveAsync("unknown.localhost");

        Assert.NotNull(result);
        Assert.True(result!.Site.IsCityWide);
        Assert.True(result.SiteNotFound);
    }

    [Fact]
    public async Task ResolveAsync_UsesCustomDomainWhenConfigured()
    {
        var optionsBuilder = await CreateInitializedDbContextOptionsAsync(async db =>
        {
            db.Sites.Add(new Site
            {
                Id = Guid.NewGuid(),
                PtaId = "87654321",
                Hostname = "customunit",
                Domain = "custom.example.org",
                IsAdminPortal = false,
                IsCityWide = false,
                SiteName = "Custom Unit",
                LogoUrl = "images/logo.png",
                BannerUrl = "images/banner.png",
                PrimaryColor = "#003366",
                AccentColor = "#FFCC00",
                WelcomeText = "Welcome",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        await using var db = new AppDbContext(optionsBuilder.Options);
        var resolver = CreateResolver(db, Options.Create(new SiteHostMappingOptions()));

        var result = await resolver.ResolveAsync("custom.example.org");

        Assert.NotNull(result);
        Assert.Equal("87654321", result!.Site.PtaId);
        Assert.False(result.SiteNotFound);
    }

    private static SiteResolver CreateResolver(
        AppDbContext db,
        IOptions<SiteHostMappingOptions> options)
    {
        return new SiteResolver(
            db,
            new MemoryCache(new MemoryCacheOptions()),
            new TestHostEnvironment { EnvironmentName = Environments.Development },
            options);
    }

    private static async Task<DbContextOptionsBuilder<AppDbContext>> CreateInitializedDbContextOptionsAsync(
        Func<AppDbContext, Task>? customize = null)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}");

        await using var db = new AppDbContext(optionsBuilder.Options);
        await db.Database.EnsureCreatedAsync();

        if (customize is not null)
        {
            await customize(db);
        }

        return optionsBuilder;
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

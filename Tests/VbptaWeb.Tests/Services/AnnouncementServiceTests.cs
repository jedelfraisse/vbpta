using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Services;
using SiteEngine.Sites;
using VbptaWeb.Tests.Support;

namespace VbptaWeb.Tests.Services;

public class AnnouncementServiceTests
{
    [Fact]
    public async Task NonAdminContext_FiltersAnnouncementsByCurrentSite()
    {
        var options = CreateDbContextOptions();
        var siteA = new Site { Id = Guid.NewGuid(), PtaId = "11111111", Hostname = "a.localhost", SiteName = "A", LogoUrl = "/a.png", BannerUrl = "/b.png", PrimaryColor = "#000", AccentColor = "#111", WelcomeText = "a" };
        var siteB = new Site { Id = Guid.NewGuid(), PtaId = "22222222", Hostname = "b.localhost", SiteName = "B", LogoUrl = "/a.png", BannerUrl = "/b.png", PrimaryColor = "#000", AccentColor = "#111", WelcomeText = "b" };

        // Seed database
        await using (var seedDb = new AppDbContext(options))
        {
            await seedDb.Database.EnsureCreatedAsync();
            seedDb.Sites.AddRange(siteA, siteB);
            seedDb.Announcements.AddRange(
                new Announcement { SiteId = siteA.Id, Title = "A1", Content = "A1" },
                new Announcement { SiteId = siteB.Id, Title = "B1", Content = "B1" });
            await seedDb.SaveChangesAsync();
        }

        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => new AppDbContext(options))
            .BuildServiceProvider();

        var service = new AnnouncementService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new SiteContextStub { CurrentSite = siteA, IsAdminContext = false, SiteConfig = siteA.ToSiteConfig() });

        var results = await service.GetVisibleAnnouncementsAsync();

        Assert.Single(results);
        Assert.Equal(siteA.Id, results[0].SiteId);
    }

    [Fact]
    public async Task AdminContext_CanSeeCrossSiteAnnouncements()
    {
        var options = CreateDbContextOptions();
        var siteA = new Site { Id = Guid.NewGuid(), PtaId = "33333333", Hostname = "a.localhost", SiteName = "A", LogoUrl = "/a.png", BannerUrl = "/b.png", PrimaryColor = "#000", AccentColor = "#111", WelcomeText = "a" };
        var siteB = new Site { Id = Guid.NewGuid(), PtaId = "44444444", Hostname = "b.localhost", SiteName = "B", LogoUrl = "/a.png", BannerUrl = "/b.png", PrimaryColor = "#000", AccentColor = "#111", WelcomeText = "b" };

        // Seed database
        await using (var seedDb = new AppDbContext(options))
        {
            await seedDb.Database.EnsureCreatedAsync();
            seedDb.Sites.AddRange(siteA, siteB);
            seedDb.Announcements.AddRange(
                new Announcement { SiteId = siteA.Id, Title = "A1", Content = "A1" },
                new Announcement { SiteId = siteB.Id, Title = "B1", Content = "B1" });
            await seedDb.SaveChangesAsync();
        }

        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => new AppDbContext(options))
            .BuildServiceProvider();

        var service = new AnnouncementService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            new SiteContextStub { CurrentSite = siteA, IsAdminContext = true, SiteConfig = siteA.ToSiteConfig() });

        var results = await service.GetVisibleAnnouncementsAsync();

        Assert.Equal(2, results.Count);
    }

    private static DbContextOptions<AppDbContext> CreateDbContextOptions()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
    }
}

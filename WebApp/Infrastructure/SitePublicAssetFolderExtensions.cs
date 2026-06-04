using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;

namespace WebApp.Infrastructure;

public static class SitePublicAssetFolderExtensions
{
    public static async Task EnsureSitePublicAssetFoldersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        // Root folder for all site-specific assets
        var root = Path.Combine(env.WebRootPath, "sites");
        Directory.CreateDirectory(root);

        // Default assets folder (inside wwwroot/defaults)
        var defaultsPath = Path.Combine(env.WebRootPath, "defaults");

        var sites = await db.Sites.AsNoTracking().ToListAsync();

        foreach (var site in sites)
        {
            var siteRoot = Path.Combine(root, site.PtaId);
            Directory.CreateDirectory(siteRoot);

            // Subfolders for site assets
            var images = Path.Combine(siteRoot, "images");
            var documents = Path.Combine(siteRoot, "documents");
            var uploads = Path.Combine(siteRoot, "uploads");

            Directory.CreateDirectory(images);
            Directory.CreateDirectory(documents);
            Directory.CreateDirectory(uploads);

            // Copy default logo/banner if missing
            CopyIfMissing(
                Path.Combine(defaultsPath, "default-logo.png"),
                Path.Combine(images, "logo.png"));

            CopyIfMissing(
                Path.Combine(defaultsPath, "default-banner.png"),
                Path.Combine(images, "banner.png"));
        }
    }

    private static void CopyIfMissing(string source, string destination)
    {
        if (File.Exists(source) && !File.Exists(destination))
        {
            File.Copy(source, destination);
        }
    }
}

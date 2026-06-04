using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Entities;
using SiteEngine.Identity;

namespace SiteEngine.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<SiteUser>(options)
{
	public DbSet<Site> Sites => Set<Site>();
	public DbSet<Announcement> Announcements => Set<Announcement>();
	public DbSet<SiteEvent> Events => Set<SiteEvent>();
	public DbSet<SiteUserRole> SiteUserRoles => Set<SiteUserRole>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Site>(entity =>
		{
			entity.ToTable("Sites");
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Hostname).HasMaxLength(255).IsRequired();
			entity.HasIndex(x => x.Hostname).IsUnique();
			entity.Property(x => x.SiteName).HasMaxLength(256).IsRequired();
			entity.Property(x => x.LogoUrl).HasMaxLength(512).IsRequired();
			entity.Property(x => x.BannerUrl).HasMaxLength(512).IsRequired();
			entity.Property(x => x.PrimaryColor).HasMaxLength(16).IsRequired();
			entity.Property(x => x.AccentColor).HasMaxLength(16).IsRequired();
			entity.Property(x => x.WelcomeText).HasMaxLength(1024).IsRequired();

			entity.HasData(SeedData.DefaultAdminSite, SeedData.DefaultCitySite);
		});

		modelBuilder.Entity<Announcement>(entity =>
		{
			entity.ToTable("Announcements");
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
			entity.Property(x => x.Content).HasMaxLength(4096).IsRequired();
			entity.HasIndex(x => x.SiteId);
			entity.HasOne(x => x.Site)
				.WithMany(x => x.Announcements)
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<SiteEvent>(entity =>
		{
			entity.ToTable("Events");
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
			entity.Property(x => x.Description).HasMaxLength(4096).IsRequired();
			entity.Property(x => x.Location).HasMaxLength(512);
			entity.HasIndex(x => x.SiteId);
			entity.HasOne(x => x.Site)
				.WithMany(x => x.Events)
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<SiteUserRole>(entity =>
		{
			entity.ToTable("SiteUserRoles");
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => new { x.UserId, x.SiteId, x.Role }).IsUnique();
			entity.HasOne(x => x.User)
				.WithMany(x => x.SiteRoles)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(x => x.Site)
				.WithMany(x => x.UserRoles)
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}


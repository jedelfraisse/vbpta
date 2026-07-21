using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Entities;
using SiteEngine.Identity;

namespace SiteEngine.Data;

// Configuration is entirely the caller's responsibility (via DbContextOptions<AppDbContext>).
// This type never reads IConfiguration or decides its own provider — that keeps it correct
// under DI (Program.cs), factory creation (IDbContextFactory<AppDbContext>), and tests
// (TestDbContextFactory) with a single, predictable code path.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<PortalConfig> PortalConfigs => Set<PortalConfig>();
	public DbSet<Site> Sites => Set<Site>();
	public DbSet<SiteUser> SiteUsers => Set<SiteUser>();
	public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
	public DbSet<SiteUserRole> SiteUserRoles => Set<SiteUserRole>();
	public DbSet<BoardPosition> BoardPositions => Set<BoardPosition>();
public DbSet<PortalTools> PortalTools { get; set; }
public DbSet<ToolRule> ToolRules { get; set; }
public DbSet<ToolPermission> ToolPermissions { get; set; }


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// ---------------------------
		// PortalConfig
		// ---------------------------
		modelBuilder.Entity<PortalConfig>(entity =>
		{
			entity.ToTable("PortalConfig");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Id)
				.ValueGeneratedNever();

			entity.Property(x => x.PortalName).HasMaxLength(255).IsRequired();
			entity.Property(x => x.PortalDomain).HasMaxLength(255).IsRequired();
			entity.Property(x => x.SmtpHost).HasMaxLength(255).IsRequired();
			entity.Property(x => x.SmtpPort).IsRequired();
			entity.Property(x => x.SmtpFromAddress).HasMaxLength(255).IsRequired();
			entity.Property(x => x.SmtpUsername).HasMaxLength(255).IsRequired();
			entity.Property(x => x.SmtpPassword).HasMaxLength(255).IsRequired();
			entity.Property(x => x.UseSsl).IsRequired();
		});

		// ---------------------------
		// Sites
		// ---------------------------
		modelBuilder.Entity<Site>(entity =>
		{
			entity.ToTable("Sites");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.PtaId).HasColumnType("char(8)").IsRequired();
			entity.Property(x => x.Hostname).HasMaxLength(255).IsRequired();
			entity.Property(x => x.Domain).HasMaxLength(255).IsRequired();
			entity.Property(x => x.SiteName).HasMaxLength(256).IsRequired();
			entity.Property(x => x.LogoUrl).HasMaxLength(512).IsRequired();
			entity.Property(x => x.BannerUrl).HasMaxLength(512).IsRequired();
			entity.Property(x => x.PrimaryColor).HasMaxLength(16).IsRequired();
			entity.Property(x => x.AccentColor).HasMaxLength(16).IsRequired();
			entity.Property(x => x.HeaderText).HasMaxLength(1024).IsRequired();

			entity.HasIndex(x => x.PtaId).IsUnique();
			entity.HasIndex(x => x.Hostname).IsUnique().HasFilter("[Hostname] <> ''");
			entity.HasIndex(x => x.Domain).IsUnique().HasFilter("[Domain] <> ''");

			entity.HasOne(x => x.ParentSite)
				.WithMany()
				.HasForeignKey(x => x.ParentSiteId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		// ---------------------------
		// SiteUser
		// ---------------------------
		modelBuilder.Entity<SiteUser>(entity =>
		{
			entity.ToTable("SiteUsers");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
			entity.Property(x => x.LastName).HasMaxLength(128).IsRequired();
			entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();

			entity.Property(x => x.PreferredEmail).HasMaxLength(255);
			entity.Property(x => x.Phone).HasMaxLength(32);

			entity.HasOne(x => x.IdentityUser)
				.WithMany()
				.HasForeignKey(x => x.IdentityUserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// ---------------------------
		// CustomRole
		// ---------------------------
		modelBuilder.Entity<CustomRole>(entity =>
		{
			entity.ToTable("CustomRoles");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
			entity.Property(x => x.SchoolYear).HasMaxLength(16).IsRequired();

			entity.HasOne<Site>()
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Restrict);
		});

		// ---------------------------
		// SiteUserRole
		// ---------------------------
		modelBuilder.Entity<SiteUserRole>(entity =>
		{
			entity.ToTable("SiteUserRoles");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.SchoolYear).HasMaxLength(16).IsRequired();

			entity.HasOne(x => x.Site)
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.SiteUser)
				.WithMany()
				.HasForeignKey(x => x.SiteUserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.CustomRole)
				.WithMany()
				.HasForeignKey(x => x.CustomRoleId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		// ---------------------------
		// ⭐ BoardPosition (NEW)
		// ---------------------------
		modelBuilder.Entity<BoardPosition>(entity =>
		{
			entity.ToTable("BoardPositions");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.PositionName).HasMaxLength(128).IsRequired();
			entity.Property(x => x.SchoolYear).HasMaxLength(16).IsRequired();

			entity.HasOne(x => x.SiteUser)
				.WithMany()
				.HasForeignKey(x => x.SiteUserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Site)
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.Restrict);
		});
	}
}

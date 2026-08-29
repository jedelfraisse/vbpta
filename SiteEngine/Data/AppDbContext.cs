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
public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
public DbSet<LoginHistorySummary> LoginHistorySummaries => Set<LoginHistorySummary>();
public DbSet<BannedEmail> BannedEmails => Set<BannedEmail>();
public DbSet<OrganizationType> OrganizationTypes => Set<OrganizationType>();
public DbSet<OrganizationLevel> OrganizationLevels => Set<OrganizationLevel>();
public DbSet<Organization> Organizations => Set<Organization>();
public DbSet<OperationalCycle> OperationalCycles => Set<OperationalCycle>();
public DbSet<ParentAccessGrant> ParentAccessGrants => Set<ParentAccessGrant>();


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// ---------------------------
		// ApplicationUser (login analytics)
		// ---------------------------
		modelBuilder.Entity<ApplicationUser>(entity =>
		{
			entity.HasOne<Site>()
				.WithMany()
				.HasForeignKey(x => x.LastLoginSiteId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		// ---------------------------
		// LoginHistory
		// ---------------------------
		modelBuilder.Entity<LoginHistory>(entity =>
		{
			entity.ToTable("LoginHistory");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
			entity.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();

			entity.HasOne<ApplicationUser>()
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Site)
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.SetNull);

			entity.HasIndex(x => x.UserId);
			entity.HasIndex(x => x.LoginUtc);
		});

		// ---------------------------
		// LoginHistorySummary
		// ---------------------------
		modelBuilder.Entity<LoginHistorySummary>(entity =>
		{
			entity.ToTable("LoginHistorySummaries");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
			entity.Property(x => x.SchoolYear).HasMaxLength(16).IsRequired();

			entity.HasOne<ApplicationUser>()
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Site)
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.SetNull);

			entity.HasIndex(x => new { x.UserId, x.SchoolYear }).IsUnique();
		});

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

			entity.Property(x => x.LogoTemplateUrl).HasMaxLength(512);
			entity.Property(x => x.LogoTemplateFontFamily).HasMaxLength(100).IsRequired();
			entity.Property(x => x.LogoTemplateFontColor).HasMaxLength(20).IsRequired();
			entity.Property(x => x.LogoTemplateTextAlign).HasMaxLength(20).IsRequired();
		});

		// ---------------------------
		// OrganizationType
		// ---------------------------
		modelBuilder.Entity<OrganizationType>(entity =>
		{
			entity.ToTable("OrganizationTypes");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
			entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
			entity.Property(x => x.IconClass).HasMaxLength(100);
			entity.Property(x => x.IdentifierLabel).HasMaxLength(100);
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
			entity.Property(x => x.LogoUrl).HasMaxLength(512);
			entity.Property(x => x.BannerUrl).HasMaxLength(512).IsRequired();
			entity.Property(x => x.PTALogoVariantUrl).HasMaxLength(512);
			entity.Property(x => x.PartnerLogoUrl).HasMaxLength(512);
			entity.Property(x => x.DistrictLogoUrl).HasMaxLength(512);
			entity.Property(x => x.SchoolCrestUrl).HasMaxLength(512);
			entity.Property(x => x.HeaderText).HasMaxLength(1024).IsRequired();

			entity.Property(x => x.PrimaryColor).HasMaxLength(32);
			entity.Property(x => x.AccentColor).HasMaxLength(32);
			entity.Property(x => x.TopBarColor).HasMaxLength(32);
			entity.Property(x => x.FooterColor1).HasMaxLength(32);
			entity.Property(x => x.FooterColor2).HasMaxLength(32);
			entity.Property(x => x.FooterColor3).HasMaxLength(32);
			entity.Property(x => x.FooterColor4).HasMaxLength(32);

			entity.Property(x => x.MenuBackgroundImageUrl).HasMaxLength(512);
			entity.Property(x => x.PageBackgroundImageUrl).HasMaxLength(512);

			entity.Property(x => x.ExternalUrl).HasMaxLength(512);

			entity.HasIndex(x => x.PtaId).IsUnique();
			entity.HasIndex(x => x.Hostname).IsUnique().HasFilter("[Hostname] <> ''");
			entity.HasIndex(x => x.Domain).IsUnique().HasFilter("[Domain] <> ''");

			// Backs the Unit Sites browse page (DashboardService.GetUnitSitesAsync):
			// every query there filters on SiteType, optionally narrows by
			// ParentSiteId (the division filter), and always orders by SiteName.
			entity.HasIndex(x => new { x.SiteType, x.ParentSiteId, x.SiteName });

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

		// ---------------------------
		// OrganizationLevel
		// ---------------------------
		modelBuilder.Entity<OrganizationLevel>(entity =>
		{
			entity.ToTable("OrganizationLevels");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Name).HasMaxLength(128).IsRequired();

			entity.HasOne(x => x.OrganizationType)
				.WithMany()
				.HasForeignKey(x => x.OrganizationTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			// A level name only needs to be unique within its own Organization
			// Type — "Council" can exist for both PTA and a different type.
			entity.HasIndex(x => new { x.OrganizationTypeId, x.Name }).IsUnique();
		});

		// ---------------------------
		// Organization
		// ---------------------------
		modelBuilder.Entity<Organization>(entity =>
		{
			entity.ToTable("Organizations");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
			entity.Property(x => x.IdentifierValue).HasMaxLength(64);
			entity.Property(x => x.Description).HasMaxLength(2000);
			entity.Property(x => x.ExternalUrl).HasMaxLength(512);

			entity.HasOne(x => x.OrganizationType)
				.WithMany()
				.HasForeignKey(x => x.OrganizationTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.OrganizationLevel)
				.WithMany()
				.HasForeignKey(x => x.OrganizationLevelId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.ParentOrganization)
				.WithMany(x => x.ChildOrganizations)
				.HasForeignKey(x => x.ParentOrganizationId)
				.OnDelete(DeleteBehavior.Restrict);

			// SetNull (not Restrict) — deleting a Site should orphan the
			// Organization's SiteId, not block the delete or take the
			// Organization down with it. An Organization outliving its Site is
			// exactly the "0 or 1 Site" relationship this framework models.
			entity.HasOne(x => x.Site)
				.WithMany()
				.HasForeignKey(x => x.SiteId)
				.OnDelete(DeleteBehavior.SetNull);

			// An Organization has at most one Site, so the reverse direction
			// needs enforcing too — filtered so multiple NULLs (the common case
			// for non-site-eligible Organizations) don't collide under the
			// unique constraint.
			entity.HasIndex(x => x.SiteId).IsUnique().HasFilter("[SiteId] IS NOT NULL");

			// Unique within its own Organization Type, not globally — two
			// different Types can run entirely separate numbering schemes
			// without colliding (see Organization.IdentifierValue).
			entity.HasIndex(x => new { x.OrganizationTypeId, x.IdentifierValue })
				.IsUnique()
				.HasFilter("[IdentifierValue] IS NOT NULL");

			entity.HasIndex(x => x.ParentOrganizationId);
			entity.HasIndex(x => x.OrganizationTypeId);
		});

		// ---------------------------
		// OperationalCycle
		// ---------------------------
		modelBuilder.Entity<OperationalCycle>(entity =>
		{
			entity.ToTable("OperationalCycles");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.CycleTypeName).HasMaxLength(128).IsRequired();
			entity.Property(x => x.DisplayLabel).HasMaxLength(128).IsRequired();

			entity.HasOne(x => x.OrganizationType)
				.WithMany()
				.HasForeignKey(x => x.OrganizationTypeId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(x => x.OrganizationTypeId);
		});

		// ---------------------------
		// ParentAccessGrant
		// ---------------------------
		modelBuilder.Entity<ParentAccessGrant>(entity =>
		{
			entity.ToTable("ParentAccessGrants");
			entity.HasKey(x => x.Id);

			entity.HasOne(x => x.ParentOrganization)
				.WithMany()
				.HasForeignKey(x => x.ParentOrganizationId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.ChildOrganization)
				.WithMany()
				.HasForeignKey(x => x.ChildOrganizationId)
				.OnDelete(DeleteBehavior.Restrict);

			// One grant per (parent, child) pair — revoking access deletes the
			// row rather than leaving a second, conflicting one behind.
			entity.HasIndex(x => new { x.ParentOrganizationId, x.ChildOrganizationId }).IsUnique();
		});

		// ---------------------------
		// BannedEmail
		// ---------------------------
		modelBuilder.Entity<BannedEmail>(entity =>
		{
			entity.ToTable("BannedEmails");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
			entity.Property(x => x.Reason).HasMaxLength(1024);

			entity.HasIndex(x => x.Email).IsUnique();
		});
	}
}

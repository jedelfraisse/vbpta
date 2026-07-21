using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Enums;
using WebApp.Models;

namespace WebApp.Services;

public class SetupStateService
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;
	private readonly SetupConnectionStringProvider _connectionProvider;

	// We cache status only for the lifetime of a single request.
	// Refresh() clears it so the next call re-evaluates everything.
	private SetupStatus? _cachedStatus;

	public SetupStateService(IDbContextFactory<AppDbContext> dbFactory, SetupConnectionStringProvider connectionProvider)
	{
		_dbFactory = dbFactory;
		_connectionProvider = connectionProvider;
	}

	public string GetConnectionString()
	{
		return _connectionProvider.Current ?? "";
	}

	public SetupStatus GetStatus()
	{
		// Cached status avoids repeated heavy checks during a single render cycle.
		if (_cachedStatus != null)
			return _cachedStatus;

		var status = new SetupStatus();

		// ============================================================
		// STEP 1 — SQL Connection String
		// ============================================================
		status.HasConnectionString = _connectionProvider.IsConfigured;

		if (status.HasConnectionString)
		{
			var cs = _connectionProvider.Current;

			try
			{
				using var conn = new SqlConnection(cs);
				conn.Open();
				status.CanConnectToDatabase = true;

				var builder = new SqlConnectionStringBuilder(cs);
				status.SqlServer = builder.DataSource;
				status.DatabaseName = builder.InitialCatalog;
				status.SqlUsername = builder.UserID;
			}
			catch
			{
				status.CanConnectToDatabase = false;
			}
		}

		// ============================================================
		// If DB is NOT ready yet, STOP HERE.
		// Steps 2-4 rely on EF and must not run until DB is configured.
		// ============================================================
		if (!status.CanConnectToDatabase)
		{
			status.HasEmailSettings = false;
			status.IsEmailWorking = false;
			status.HasAdminUser = false;

			return _cachedStatus = FinalizeStatus(status);
		}

		using var db = _dbFactory.CreateDbContext();

		// ============================================================
		// STEP 2 — SMTP Settings (from PortalConfig)
		// ============================================================
		var cfg = db.PortalConfigs.FirstOrDefault(c => c.Id == SeedData.DefaultGlobalConfigId);

		status.SmtpHost = cfg?.SmtpHost ?? "";
		status.SmtpPort = cfg?.SmtpPort ?? 0;
		status.SmtpUsername = cfg?.SmtpUsername ?? "";
		status.SmtpFrom = cfg?.SmtpFromAddress ?? "";
		status.SmtpUseSsl = cfg?.UseSsl ?? false;

		status.HasEmailSettings =
			!string.IsNullOrWhiteSpace(status.SmtpHost) &&
			!string.IsNullOrWhiteSpace(status.SmtpUsername) &&
			!string.IsNullOrWhiteSpace(status.SmtpFrom);

		status.IsEmailWorking = status.HasEmailSettings;

		// ============================================================
		// STEP 3 — Admin User
		// ============================================================
		try
		{
			status.HasAdminUser = db.SiteUserRoles
				.Any(r =>
					(r.Role == SiteRole.SuperAdmin || r.Role == SiteRole.SiteAdmin) &&
					r.SiteId == SeedData.DefaultPortalSiteId);

			if (status.HasAdminUser)
			{
				var adminRole = db.SiteUserRoles
					.Include(r => r.SiteUser)
					.Where(r =>
						(r.Role == SiteRole.SuperAdmin || r.Role == SiteRole.SiteAdmin) &&
						r.SiteId == SeedData.DefaultPortalSiteId)
					.OrderBy(r => r.StartUtc)
					.FirstOrDefault();

				if (adminRole?.SiteUser != null)
				{
					status.AdminEmail = adminRole.SiteUser.PreferredEmail;
					status.AdminName = adminRole.SiteUser.DisplayName;
				}
			}
		}
		catch
		{
			status.HasAdminUser = false;
		}

		// ============================================================
		// STEP 4 — Portal Info
		// ============================================================
		try
		{
			if (cfg != null)
			{
				status.PortalName = cfg.PortalName ?? "";
				status.PortalDomain = cfg.PortalDomain ?? "";
				status.HasPortalInfo = !string.IsNullOrWhiteSpace(status.PortalName);
			}
		}
		catch
		{
			status.HasPortalInfo = false;
		}

		return _cachedStatus = FinalizeStatus(status);
	}

	public void Refresh()
	{
		_cachedStatus = null;
	}

	// ============================================================
	// Final step progression logic
	// ============================================================
	private SetupStatus FinalizeStatus(SetupStatus status)
	{
		status.NeedsSql = !(status.HasConnectionString && status.CanConnectToDatabase);
		status.NeedsSmtp = !status.NeedsSql && !status.HasEmailSettings;
		status.NeedsAdmin = !status.NeedsSql && !status.NeedsSmtp && !status.HasAdminUser;
		status.NeedsPortalInfo =
			!status.NeedsSql &&
			!status.NeedsSmtp &&
			!status.NeedsAdmin &&
			!status.HasPortalInfo;

		status.IsFullyConfigured =
			status.HasConnectionString &&
			status.CanConnectToDatabase &&
			status.HasEmailSettings &&
			status.HasAdminUser &&
			status.HasPortalInfo;

		return status;
	}
}

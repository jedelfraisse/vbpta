using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;
using SiteEngine.Identity;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebApp.Models;

namespace WebApp.Services;

public class SetupService
{
	private readonly IWebHostEnvironment _env;
	private readonly IHostApplicationLifetime _lifetime;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IDbContextFactory<AppDbContext> _dbFactory;
	private readonly SetupConnectionStringProvider _connectionProvider;
	private readonly ILogger<SetupService> _logger;
	private readonly SetupStateService _setupState;
	private readonly IDataProtector _protector;
	private readonly SystemDiagnosticsState _diagnostics;


	private static readonly Dictionary<string, string> _adminCodes = new();

	public SetupService(
		IWebHostEnvironment env,
		IHostApplicationLifetime lifetime,
		IServiceScopeFactory scopeFactory,
		IDbContextFactory<AppDbContext> dbFactory,
		SetupConnectionStringProvider connectionProvider,
		ILogger<SetupService> logger,
		SetupStateService setupState,
		IDataProtectionProvider provider,
		SystemDiagnosticsState diagnostics)
	{
		_env = env;
		_lifetime = lifetime;
		_scopeFactory = scopeFactory;
		_dbFactory = dbFactory;
		_connectionProvider = connectionProvider;
		_logger = logger;
		_setupState = setupState;
		_diagnostics = diagnostics;

		_protector = provider.CreateProtector("PortalConfig.SmtpPassword");
	}


	// ------------------------------------------------------------
	// 1. SQL CONNECTION TEST
	// ------------------------------------------------------------
	public async Task TestSqlConnectionAsync(SetupSetupModel model)
	{
		var cs = BuildConnectionString(model);

		using var conn = new SqlConnection(cs);
		await conn.OpenAsync();
	}

	// ------------------------------------------------------------
	// 1b. RUN MIGRATIONS + SEED, then publish the connection string
	// ------------------------------------------------------------
	public async Task RunMigrationsAsync(SetupSetupModel model)
	{
		var cs = BuildConnectionString(model);

		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlServer(cs)
			.Options;

		await using var migrationDb = new AppDbContext(options);

		await migrationDb.Database.MigrateAsync();

		SeedData.EnsureSeedData(migrationDb);

		// Only becomes "current" once migration has proven it's valid. Every
		// AppDbContext created after this line (factory or DI-scoped) sees it
		// immediately — no file re-read, no IConfiguration reload to wait on.
		_connectionProvider.Set(cs);
	}

	// ------------------------------------------------------------
	// 2. SMTP TEST
	// ------------------------------------------------------------
	public async Task TestSmtpAsync(SetupSetupModel model)
	{
		using var client = new SmtpClient(model.SmtpHost, model.SmtpPort)
		{
			EnableSsl = model.SmtpUseSsl,
			Credentials = new NetworkCredential(model.SmtpUsername, model.SmtpPassword)
		};

		var msg = new MailMessage
		{
			From = new MailAddress(model.SmtpFrom),
			Subject = "PTA Setup: SMTP Test",
			Body = "SMTP configuration is working."
		};

		msg.To.Add(model.SmtpFrom);

		await client.SendMailAsync(msg);
	}

	// ------------------------------------------------------------
	// 3. SEND ADMIN VERIFICATION CODE
	// ------------------------------------------------------------
	public async Task SendAdminVerificationCodeAsync(string adminEmail)
	{
		var code = new Random().Next(100000, 999999).ToString();
		_adminCodes[adminEmail] = code;

		var smtp = await LoadSmtpSettingsAsync();

		using var client = new SmtpClient(smtp.Host, smtp.Port)
		{
			EnableSsl = smtp.UseSsl,
			Credentials = new NetworkCredential(smtp.User, smtp.Password)
		};

		var msg = new MailMessage
		{
			From = new MailAddress(smtp.From),
			Subject = "PTA Setup: Admin Verification Code",
			Body = $"Your verification code is: {code}"
		};

		msg.To.Add(adminEmail);

		await client.SendMailAsync(msg);
	}

	private async Task<(string Host, int Port, string User, string Password, string From, bool UseSsl)> LoadSmtpSettingsAsync()
	{
		await using var db = await _dbFactory.CreateDbContextAsync();

		var cfg = await db.PortalConfigs.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId);

		if (cfg == null)
			throw new InvalidOperationException("PortalConfig not found. SMTP settings are not available.");

		var password = _protector.Unprotect(cfg.SmtpPassword);

		return (
			Host: cfg.SmtpHost,
			Port: cfg.SmtpPort,
			User: cfg.SmtpUsername,
			Password: password,
			From: cfg.SmtpFromAddress,
			UseSsl: cfg.UseSsl
		);
	}


	// ------------------------------------------------------------
	// 4. VALIDATE ADMIN CODE
	// ------------------------------------------------------------
	public bool ValidateAdminCode(string adminEmail, string code)
	{
		if (_adminCodes.TryGetValue(adminEmail, out var stored))
			return stored == code;

		return false;
	}

	// Retires a pending code — used when the admin resends to a corrected
	// email address, so a stale code left behind for a mistyped address
	// can't still be redeemed.
	public void InvalidateAdminCode(string adminEmail)
	{
		_adminCodes.Remove(adminEmail);
	}

	// ------------------------------------------------------------
	// 5. CREATE ADMIN USER (EF SAFE)
	// ------------------------------------------------------------
	public async Task CreateAdminUserAsync(string email, string displayName)
	{
		_logger.LogInformation("Creating admin user: {Email}", email);

		var identityUser = new ApplicationUser
		{
			UserName = email,
			Email = email,
			EmailConfirmed = true
		};

		// Resolve UserManager from a brand-new DI scope instead of constructor
		// injection. Constructor injection would force Identity's UserStore (and
		// its ambient scoped AppDbContext) to resolve as soon as SetupService
		// itself is constructed — at Step 1's page load, before a connection
		// string exists — pinning that circuit's scoped AppDbContext as
		// unconfigured for good. A fresh scope here always resolves a fresh,
		// correctly-configured AppDbContext at the moment it's actually needed
		// (Step 3, after the connection string is already known-good).
		using var identityScope = _scopeFactory.CreateScope();
		var userManager = identityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

		var identityResult = await userManager.CreateAsync(identityUser);
		if (!identityResult.Succeeded)
		{
			var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
			throw new Exception($"Failed to create IdentityUser: {errors}");
		}

		await using var db = await _dbFactory.CreateDbContextAsync();

		// identityUser was created and saved through a *different* AppDbContext
		// instance (the fresh identityScope above). Only set the FK scalar here —
		// setting the IdentityUser navigation would make db's change tracker treat
		// it as a new, untracked entity and try to INSERT it again on SaveChanges,
		// colliding with the row Identity already wrote (duplicate primary key).
		var siteUser = new SiteUser
		{
			IdentityUserId = identityUser.Id,
			DisplayName = displayName,
			FirstName = displayName,
			LastName = "",
			PreferredEmail = email,
			CreatedAtUtc = DateTimeOffset.UtcNow,
			UpdatedAtUtc = DateTimeOffset.UtcNow
		};

		db.SiteUsers.Add(siteUser);

		try
		{
			await db.SaveChangesAsync();
		}
		catch (DbUpdateException ex)
		{
			_logger.LogError(ex, "Failed to save SiteUser for {Email}: {InnerMessage}",
				email, ex.InnerException?.Message ?? ex.Message);
			throw;
		}

		db.SiteUserRoles.Add(new SiteUserRole
		{
			SiteId = SeedData.DefaultPortalSiteId,
			SiteUserId = siteUser.Id,
			Role = SiteRole.SuperAdmin,
			SchoolYear = "GLOBAL",
			StartUtc = DateTimeOffset.UtcNow
		});

		db.SiteUserRoles.Add(new SiteUserRole
		{
			SiteId = SeedData.DefaultPortalSiteId,
			SiteUserId = siteUser.Id,
			Role = SiteRole.SiteAdmin,
			SchoolYear = "GLOBAL",
			StartUtc = DateTimeOffset.UtcNow
		});

		await db.SaveChangesAsync();

		_logger.LogInformation("Admin user created successfully: {Email}", email);
	}

	// ------------------------------------------------------------
	// 6. SAVE STEP (SQL, SMTP, PORTAL INFO)
	// ------------------------------------------------------------
	public async Task SaveStepAsync(SetupSetupModel model)
	{
		var settingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
		var root = LoadOrCreateAppSettings(settingsPath);

		if (!root.ContainsKey("ConnectionStrings"))
			root["ConnectionStrings"] = new JsonObject();

		var csSection = root["ConnectionStrings"]!.AsObject();

		// Save SQL
		if (!string.IsNullOrWhiteSpace(model.SqlServer))
			csSection["DefaultConnection"] = BuildConnectionString(model);

		// Save JSON file (durable copy — survives the process restart at the end of setup)
		File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

		// In-memory correctness for *this* running process comes from the connection
		// string provider, which RunMigrationsAsync already published. No re-read of
		// the file or IConfiguration here — nothing to race against.
		if (_connectionProvider.IsConfigured)
		{
			await using var db = await _dbFactory.CreateDbContextAsync();

			var cfg = await db.PortalConfigs
				.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId);

			if (cfg != null)
			{
				// SaveStepAsync runs after every wizard step against one shared
				// model — but a page reload or dropped circuit between steps
				// starts a *fresh* SetupSetupModel with these fields back at
				// their defaults. Without these guards, e.g. Step 1's SQL save
				// (before SMTP is even filled in) or a fresh Step 3/4 model
				// would silently blank out SMTP settings a prior step already
				// saved — which is exactly how "Port" ends up showing 0.
				if (!string.IsNullOrWhiteSpace(model.PortalName))
					cfg.PortalName = model.PortalName;
				if (!string.IsNullOrWhiteSpace(model.PortalDomain))
					cfg.PortalDomain = model.PortalDomain;

				if (!string.IsNullOrWhiteSpace(model.SmtpHost))
				{
					cfg.SmtpHost = model.SmtpHost;
					cfg.SmtpPort = model.SmtpPort;
					cfg.SmtpFromAddress = model.SmtpFrom;
					cfg.SmtpUsername = model.SmtpUsername;
					cfg.UseSsl = model.SmtpUseSsl;

					// Only overwrite the stored (encrypted) password when this
					// step actually carried one — an empty password here means
					// "not part of this step's model", not "clear it".
					if (!string.IsNullOrEmpty(model.SmtpPassword))
						cfg.SmtpPassword = _protector.Protect(model.SmtpPassword);
				}

				await db.SaveChangesAsync();
			}
		}

		// Refresh SetupStateService so SetupSetup.razor sees updated values
		_setupState.Refresh();
	}

	// ------------------------------------------------------------
	// 7. GLOBAL ADMIN — SYSTEM SETTINGS (Database Connection / Email Server)
	// ------------------------------------------------------------
	// Back the SuperAdmin-only pages under /globaladmin/system/database and
	// /globaladmin/system/email. Deliberately NOT built on top of SaveStepAsync:
	// that method writes the SQL connection string and every SMTP/portal field
	// together, unconditionally for SMTP. That's correct for the one-time setup
	// wizard (one form, one submit) but wrong here, where the two pages save
	// independently — reusing it would blank out SMTP fields when only the SQL
	// connection is being edited, and vice versa.

	// Dry-run a raw, already-assembled connection string (used by the "Validate"
	// button before saving). Never touches SetupConnectionStringProvider.
	public async Task TestRawSqlConnectionAsync(string connectionString)
	{
		using var conn = new SqlConnection(connectionString);
		await conn.OpenAsync();
	}

	public async Task<(bool Success, string? Error)> ValidateDatabaseConnectionAsync(SetupSetupModel model)
	{
		try
		{
			await TestRawSqlConnectionAsync(BuildConnectionString(model));
			return (true, null);
		}
		catch (Exception ex)
		{
			return (false, ex.Message);
		}
	}

	// Persists the new connection string to appsettings.json only — it does NOT
	// call _connectionProvider.Set(...). Unlike the setup wizard (which only
	// publishes a connection string in-memory once RunMigrationsAsync has proven
	// it against a real, migrated schema), a connection string edited here later
	// might point at a database with a different schema entirely. Taking effect
	// only after a restart is exactly what the "Restart Required" banner communicates.
	public void SaveDatabaseConnection(SetupSetupModel model)
	{
		var cs = BuildConnectionString(model);

		var settingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
		var root = LoadOrCreateAppSettings(settingsPath);

		if (!root.ContainsKey("ConnectionStrings"))
			root["ConnectionStrings"] = new JsonObject();

		root["ConnectionStrings"]!.AsObject()["DefaultConnection"] = cs;

		File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
	}

	public async Task<(bool Success, string? Error)> TestSmtpSettingsAsync(SetupSetupModel model)
	{
		try
		{
			await TestSmtpAsync(model);
			return (true, null);
		}
		catch (Exception ex)
		{
			return (false, ex.Message);
		}
	}

	// Updates only the SMTP fields on PortalConfig — PortalName/PortalDomain are
	// left untouched, unlike SaveStepAsync's all-in-one write.
	public async Task SaveSmtpSettingsAsync(SetupSetupModel model, bool passwordChanged)
	{
		await using var db = await _dbFactory.CreateDbContextAsync();

		var cfg = await db.PortalConfigs.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId)
			?? throw new InvalidOperationException("PortalConfig not found.");

		cfg.SmtpHost = model.SmtpHost;
		cfg.SmtpPort = model.SmtpPort;
		cfg.SmtpUsername = model.SmtpUsername;
		cfg.SmtpFromAddress = model.SmtpFrom;
		cfg.SmtpReplyToAddress = model.SmtpReplyTo;
		cfg.UseSsl = model.SmtpUseSsl;

		// The field is shown masked and left blank when unchanged — only overwrite
		// the stored (encrypted) password when the admin actually typed a new one,
		// so leaving it blank doesn't wipe out the working secret.
		if (passwordChanged)
			cfg.SmtpPassword = _protector.Protect(model.SmtpPassword);

		await db.SaveChangesAsync();
	}

	// Sends a one-off test message using whatever SMTP settings are currently
	// saved on PortalConfig (i.e. the last-saved settings, not unsaved form
	// edits) to the logged-in admin's own address, and records the outcome on
	// SystemDiagnosticsState for the "Last successful send" field.
	public async Task<(bool Success, string? Error)> SendSystemTestEmailAsync(string toEmail)
	{
		try
		{
			await using var db = await _dbFactory.CreateDbContextAsync();
			var cfg = await db.PortalConfigs.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId)
				?? throw new InvalidOperationException("PortalConfig not found.");

			var password = _protector.Unprotect(cfg.SmtpPassword);

			using var client = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
			{
				EnableSsl = cfg.UseSsl,
				Credentials = new NetworkCredential(cfg.SmtpUsername, password)
			};

			var msg = new MailMessage
			{
				From = new MailAddress(cfg.SmtpFromAddress),
				Subject = "Global Admin: SMTP test email",
				Body = "This is a test message sent from Global Admin → System & Developer → System Settings → Email Server. " +
					"If you received this, the saved SMTP configuration is working."
			};

			if (!string.IsNullOrWhiteSpace(cfg.SmtpReplyToAddress))
				msg.ReplyToList.Add(new MailAddress(cfg.SmtpReplyToAddress));

			msg.To.Add(toEmail);

			await client.SendMailAsync(msg);

			_diagnostics.RecordEmailTest(succeeded: true);
			return (true, null);
		}
		catch (Exception ex)
		{
			_diagnostics.RecordEmailTest(succeeded: false);
			return (false, ex.Message);
		}
	}

	// ------------------------------------------------------------
	// 8. TRIGGER RESTART
	// ------------------------------------------------------------
	public void TriggerRestart()
	{
		var webConfigPath = Path.Combine(_env.ContentRootPath, "web.config");
		if (File.Exists(webConfigPath))
		{
			File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
			return;
		}

		_lifetime.StopApplication();
	}

	// ------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------

	// Reads appsettings.json into a mutable JsonObject, or starts a fresh empty
	// one if the file doesn't exist yet on disk — e.g. a brand-new deployment
	// whose CI pipeline deliberately excludes appsettings.json from the FTP
	// upload (so redeploys don't clobber a live-configured connection string)
	// and this is the very first time anything has written it on this server.
	private static JsonObject LoadOrCreateAppSettings(string settingsPath)
	{
		if (!File.Exists(settingsPath))
			return new JsonObject();

		var json = File.ReadAllText(settingsPath);
		return JsonNode.Parse(json)!.AsObject();
	}

	private string BuildConnectionString(SetupSetupModel model)
	{
		var builder = new SqlConnectionStringBuilder
		{
			DataSource = model.SqlServer,
			InitialCatalog = model.DatabaseName,
			UserID = model.SqlUsername,
			Password = model.SqlPassword,
			TrustServerCertificate = true,
			MultipleActiveResultSets = true
		};

		return builder.ConnectionString;
	}
}

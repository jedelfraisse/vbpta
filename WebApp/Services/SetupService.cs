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


	private static readonly Dictionary<string, string> _adminCodes = new();

	public SetupService(
		IWebHostEnvironment env,
		IHostApplicationLifetime lifetime,
		IServiceScopeFactory scopeFactory,
		IDbContextFactory<AppDbContext> dbFactory,
		SetupConnectionStringProvider connectionProvider,
		ILogger<SetupService> logger,
		SetupStateService setupState,
		IDataProtectionProvider provider)
	{
		_env = env;
		_lifetime = lifetime;
		_scopeFactory = scopeFactory;
		_dbFactory = dbFactory;
		_connectionProvider = connectionProvider;
		_logger = logger;
		_setupState = setupState;

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
		var json = File.ReadAllText(settingsPath);

		var root = JsonNode.Parse(json)!.AsObject();

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
				cfg.PortalName = model.PortalName;
				cfg.PortalDomain = model.PortalDomain;

				cfg.SmtpHost = model.SmtpHost;
				cfg.SmtpPort = model.SmtpPort;
				cfg.SmtpFromAddress = model.SmtpFrom;
				cfg.SmtpUsername = model.SmtpUsername;
				// Encrypt the password before saving
				cfg.SmtpPassword = _protector.Protect(model.SmtpPassword);
				cfg.UseSsl = model.SmtpUseSsl;

				await db.SaveChangesAsync();
			}
		}

		// Refresh SetupStateService so SetupSetup.razor sees updated values
		_setupState.Refresh();
	}

	// ------------------------------------------------------------
	// 7. TRIGGER RESTART
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

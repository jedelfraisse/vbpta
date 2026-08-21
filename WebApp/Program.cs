using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Identity;
using WebApp.Authentication;
using WebApp.Components;
using WebApp.Models;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Razor Components hosting model (.NET 8)
// ------------------------------------------------------------
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// ------------------------------------------------------------
// Always-safe services (available in setup + runtime)
// ------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<SetupStateService>();
builder.Services.AddScoped<SetupService>();

// ------------------------------------------------------------
// Single source of truth for "what's the connection string right now".
// Loaded once from appsettings.json at process start; updated in-memory the
// instant the setup wizard proves a new one works (SetupService.RunMigrationsAsync).
// Nothing downstream re-reads IConfiguration or the file to decide this.
// ------------------------------------------------------------
builder.Services.AddSingleton<SetupConnectionStringProvider>();

// Process-lifetime diagnostics (last startup validation, last test email) shown
// on the Global Admin > System Settings > Database Connection / Email Server pages.
builder.Services.AddSingleton<SystemDiagnosticsState>();

// ------------------------------------------------------------
// AppDbContext factory — the ONLY place DbContextOptions<AppDbContext> gets
// built. Registered as a plain singleton service (not via AddDbContextFactory,
// which would cache DbContextOptions itself as a singleton and freeze whatever
// connection string existed at first resolution — see LiveConnectionDbContextFactory
// for why that's wrong here). LiveConnectionDbContextFactory builds fresh options
// from SetupConnectionStringProvider on every single CreateDbContext() call.
// ------------------------------------------------------------
builder.Services.AddSingleton<IDbContextFactory<AppDbContext>, LiveConnectionDbContextFactory>();

// ------------------------------------------------------------
// Scoped AppDbContext — exists only because Identity's
// AddEntityFrameworkStores<AppDbContext>() requires TContext to be resolvable
// as a normal DI service. It delegates to the same factory above, so it's never
// out of sync with the live connection string — but it IS still cached for the
// rest of the circuit once first resolved, same as any scoped service. Setup-time
// code must not depend on this; use IDbContextFactory<AppDbContext> directly instead.
// ------------------------------------------------------------
builder.Services.AddScoped<AppDbContext>(sp =>
	sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// ------------------------------------------------------------
// Identity — always registered, but DB may not be available yet
// ------------------------------------------------------------
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
	options.User.RequireUniqueEmail = true;
	options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
	.AddIdentityCookies();

// Propagates HttpContext.User into AuthenticationStateProvider for the Razor
// Components tree. Without this, AuthorizeView / [Inject] AuthenticationStateProvider
// never reflect the Identity auth cookie, no matter how it was set.
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorization();
builder.Services.AddDataProtection();


builder.Services.AddSingleton<PasswordlessCodeStore>();
builder.Services.AddScoped<IEmailLoginSender, EmailLoginSender>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<MembershipLookupService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<SiteAdminService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<PtaLogoGenerationService>();
builder.Services.AddScoped<LoginTrackingService>();
builder.Services.AddScoped<LoginAnalyticsService>();
builder.Services.AddScoped<PasswordlessSignInService>();

// ------------------------------------------------------------
// Site context services — always registered
// (They will gracefully degrade if DB is unavailable)
// ------------------------------------------------------------
builder.Services.AddScoped<SiteContextResolver>();
builder.Services.AddScoped<RuntimeSiteContext>();
builder.Services.AddScoped<SetupSiteContext>();
builder.Services.AddScoped<SiteContext>();
builder.Services.AddScoped<SiteRoleResolver>();

// ------------------------------------------------------------
// Build the app
// ------------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------------
// Startup migration check. Only meaningful once a connection string
// already exists — a fresh, unconfigured install has no DB to check yet,
// and the setup wizard (SetupService.RunMigrationsAsync) applies the
// initial schema itself once the operator submits one. This is what keeps
// an already-running deployment's schema current across later deploys —
// nothing else re-applies migrations after the wizard's one-time run.
//
// Before trusting IsConfigured, we verify the target database actually
// exists and has a schema. SetupConnectionStringProvider only remembers
// whatever appsettings.json said at process start (or whatever the setup
// wizard last proved worked) — it has no way to notice that the database
// was deleted, renamed, or swapped out from under it. If we skipped this
// check, a stale-but-"configured" connection string would make Routes.razor
// treat the app as fully set up and try to build AppDbContext/Identity
// against a target that doesn't exist. Detecting that here and resetting
// the provider is what lets Routes.razor fall back into setup mode instead.
// ------------------------------------------------------------
using (var startupScope = app.Services.CreateScope())
{
	var connectionProvider = startupScope.ServiceProvider.GetRequiredService<SetupConnectionStringProvider>();
	var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

	var isConfigured = connectionProvider.IsConfigured;
	var databaseExists = false;
	var hasTables = false;
	var hasPendingMigrations = false;

	if (isConfigured)
	{
		var cs = connectionProvider.Current!;

		try
		{
			using var conn = new SqlConnection(cs);
			await conn.OpenAsync();
			databaseExists = true;

			using var tableCountCmd = conn.CreateCommand();
			tableCountCmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
			var tableCount = Convert.ToInt32(await tableCountCmd.ExecuteScalarAsync());
			hasTables = tableCount > 0;
		}
		catch (SqlException ex)
		{
			// Server unreachable, or reachable but the database itself doesn't exist
			// (e.g. "Cannot open database 'X' requested by the login").
			startupLogger.LogWarning(ex, "Could not open the configured database.");
			databaseExists = false;
		}

		if (!databaseExists || !hasTables)
		{
			startupLogger.LogWarning(
				"Configured connection string does not point to a usable database (exists={DatabaseExists}, hasTables={HasTables}). Falling back to setup mode.",
				databaseExists, hasTables);

			connectionProvider.Reset();
			isConfigured = false;
		}
		else
		{
			try
			{
				var dbFactory = startupScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
				await using var startupDb = await dbFactory.CreateDbContextAsync();

				var pendingMigrations = (await startupDb.Database.GetPendingMigrationsAsync()).ToList();
				hasPendingMigrations = pendingMigrations.Count > 0;

				if (hasPendingMigrations)
				{
					startupLogger.LogWarning(
						"Applying {Count} pending migration(s): {Migrations}",
						pendingMigrations.Count, string.Join(", ", pendingMigrations));

					await startupDb.Database.MigrateAsync();

					startupLogger.LogInformation("Pending migrations applied successfully.");
				}
			}
			catch (Exception ex)
			{
				startupLogger.LogError(ex, "Startup migration check failed.");
			}
		}
	}

	startupLogger.LogInformation(
		"Startup DB diagnostics: connectionStringConfigured={IsConfigured}, databaseExists={DatabaseExists}, hasTables={HasTables}, pendingMigrations={HasPendingMigrations}",
		isConfigured, databaseExists, hasTables, hasPendingMigrations);

	// Recorded regardless of outcome — the Database Connection admin page shows
	// this as "Last startup validation" either way; isConfigured/databaseExists
	// above already cover whether it succeeded.
	startupScope.ServiceProvider.GetRequiredService<SystemDiagnosticsState>().RecordStartupValidation();
}

// ------------------------------------------------------------
// Middleware pipeline
// ------------------------------------------------------------
app.UseStaticFiles();
app.UseRouting();

// ------------------------------------------------------------
// Setup-mode cookie guard: while the DB isn't configured (fresh install,
// or reset back into setup mode above after a DB reset/swap), strip any
// Identity auth cookies before UseAuthentication() runs. Otherwise
// Identity's cookie handler tries to validate them, which resolves
// AppDbContext and throws "Cannot create AppDbContext: no connection
// string has been configured yet." before the setup wizard ever gets a
// chance to run. This also has the effect of logging everyone out
// whenever setup mode is (re-)entered. Once IsConfigured is true again,
// this middleware is a no-op and normal cookie-based login persists.
// ------------------------------------------------------------
app.Use(async (context, next) =>
{
	var connectionProvider = context.RequestServices.GetRequiredService<SetupConnectionStringProvider>();
	if (!connectionProvider.IsConfigured)
	{
		foreach (var cookieName in context.Request.Cookies.Keys.Where(name => name.StartsWith(".AspNetCore.Identity.", StringComparison.Ordinal)).ToList())
		{
			context.Response.Cookies.Delete(cookieName);
		}
	}

	await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ------------------------------------------------------------
// Razor Components endpoint
// ------------------------------------------------------------
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();


// ------------------------------------------------------------
// Health endpoint for setup restart detection
// ------------------------------------------------------------
app.MapGet("/health", () => Results.Ok("OK"));

// ------------------------------------------------------------
// Passwordless login completion. Login.razor does a full-page
// NavigateTo(forceLoad: true) here instead of an in-app Blazor navigation
// because signing in has to write the Identity auth cookie onto the HTTP
// response, which a Blazor Server circuit (no HttpContext of its own after
// the initial request) can't do directly.
// ------------------------------------------------------------
app.MapGet("/auth/passwordless/complete", async (
	string email,
	string code,
	string? returnUrl,
	HttpContext context,
	PasswordlessSignInService signInService) =>
{
	var ipAddress = context.Connection.RemoteIpAddress?.ToString();
	var result = await signInService.SignInWithCodeAsync(email, code, context.Request.Host.Host, ipAddress);

	if (!result.Succeeded)
	{
		var error = Uri.EscapeDataString("That code is invalid or has expired. Please request a new one.");
		return Results.Redirect($"/login?Error={error}");
	}

	var target = LocalUrlGuard.IsLocalUrl(returnUrl) ? returnUrl! : "/";

	if (result.RequiresProfileCompletion)
	{
		var encodedReturnUrl = Uri.EscapeDataString(target);
		return Results.Redirect($"/complete-profile?returnUrl={encodedReturnUrl}");
	}

	return Results.Redirect(target);
});

// ------------------------------------------------------------
// Sign-out completion. Logout.razor does a full-page NavigateTo(forceLoad: true)
// here for the same reason as passwordless completion above: clearing the
// Identity auth cookie has to happen on an HTTP response, not inside a
// Blazor Server circuit.
// ------------------------------------------------------------
app.MapGet("/auth/logout", async (PasswordlessSignInService signInService) =>
{
	await signInService.SignOutAsync();
	return Results.Redirect("/");
});


// ------------------------------------------------------------
// Run the app
// ------------------------------------------------------------
app.Run();

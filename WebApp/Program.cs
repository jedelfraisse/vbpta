using Microsoft.AspNetCore.Identity;
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
builder.Services.AddScoped<PasswordlessSignInService>();

// ------------------------------------------------------------
// Site context services — always registered
// (They will gracefully degrade if DB is unavailable)
// ------------------------------------------------------------
builder.Services.AddScoped<SiteContextResolver>();
builder.Services.AddScoped<RuntimeSiteContext>();
builder.Services.AddScoped<SetupSiteContext>();
builder.Services.AddScoped<SiteContext>();

// ------------------------------------------------------------
// Build the app
// ------------------------------------------------------------
var app = builder.Build();

// ------------------------------------------------------------
// Middleware pipeline
// ------------------------------------------------------------
app.UseStaticFiles();
app.UseRouting();
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
	var success = await signInService.SignInWithCodeAsync(email, code, context.Request.Host.Host);

	if (!success)
	{
		var error = Uri.EscapeDataString("That code is invalid or has expired. Please request a new one.");
		return Results.Redirect($"/login?Error={error}");
	}

	return Results.Redirect(IsLocalUrl(returnUrl) ? returnUrl! : "/");
});

// Prevents the returnUrl query parameter (attacker-controlled) from being used
// for an open redirect — only same-origin, root-relative paths are allowed.
static bool IsLocalUrl(string? url) =>
	!string.IsNullOrWhiteSpace(url) &&
	url.StartsWith('/') &&
	!url.StartsWith("//") &&
	!url.StartsWith("/\\");

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

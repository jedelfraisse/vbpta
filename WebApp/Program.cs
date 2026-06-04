using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SiteEngine.Data;
using SiteEngine.Identity;
using SiteEngine.Options;
using SiteEngine.Services;
using SiteEngine.Sites;
using WebApp.Authentication;
using WebApp.Components;
using WebApp.Infrastructure;
using WebApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection"),
		sqlOptions => sqlOptions.EnableRetryOnFailure(
			maxRetryCount: 5,
			maxRetryDelay: TimeSpan.FromSeconds(10),
			errorNumbersToAdd: null)));
builder.Services.Configure<SiteHostMappingOptions>(
	builder.Configuration.GetSection(SiteHostMappingOptions.SectionName));
builder.Services.Configure<EmailLoginOptions>(
	builder.Configuration.GetSection(EmailLoginOptions.SectionName));
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
	.AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services
	.AddIdentityCore<SiteUser>(options =>
	{
		options.User.RequireUniqueEmail = true;
		options.SignIn.RequireConfirmedEmail = false;
	})
	.AddEntityFrameworkStores<AppDbContext>()
	.AddSignInManager()
	.AddDefaultTokenProviders();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISiteResolver, SiteResolver>();
builder.Services.AddScoped<ISiteUserService, SiteUserService>();
builder.Services.AddScoped<ISiteContext, SiteContext>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IPlatformConfigurationService, PlatformConfigurationService>();
builder.Services.AddScoped<ISitePublicAssetService, SitePublicAssetService>();
builder.Services.AddScoped<PasswordlessSignInService>();
builder.Services.AddScoped<IEmailLoginSender, SmtpEmailLoginSender>();

var app = builder.Build();

await app.RunPendingMigrationsIfRequestedAsync();
await app.EnsureSitePublicAssetFoldersAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<SiteResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();


app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet(
	"/auth/passwordless/complete",
	async Task<IResult> (
		HttpContext httpContext,
		PasswordlessSignInService passwordlessSignInService,
		string email,
		string code,
		string? returnUrl) =>
	{
		try
		{
			var valid = await passwordlessSignInService.SignInWithCodeAsync(email, code, httpContext.Request.Host.Host);
			if (!valid)
			{
				return Results.LocalRedirect("/login?error=Invalid%20or%20expired%20code.");
			}
		}
		catch (InvalidOperationException ex)
		{
			return Results.LocalRedirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
		}

		var destination = IsLocalReturnUrl(returnUrl) ? returnUrl! : "/";
		return Results.LocalRedirect(destination);
	});
app.MapGet("/auth/logout", async Task<IResult> (PasswordlessSignInService passwordlessSignInService) =>
{
	await passwordlessSignInService.SignOutAsync();
	return Results.LocalRedirect("/");
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

static bool IsLocalReturnUrl(string? returnUrl)
{
	if (string.IsNullOrWhiteSpace(returnUrl))
	{
		return false;
	}

	return Uri.TryCreate(returnUrl, UriKind.Relative, out _)
		&& returnUrl.StartsWith("/", StringComparison.Ordinal)
		&& !returnUrl.StartsWith("//", StringComparison.Ordinal);
}

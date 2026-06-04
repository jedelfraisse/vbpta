using System.Text.RegularExpressions;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Identity;
using SiteEngine.Sites;

namespace SiteEngine.Services;

public class PlatformConfigurationService(
	IDbContextFactory<AppDbContext> dbContextFactory,
	ISiteContext siteContext,
	ISiteResolver siteResolver,
	ISiteUserService siteUserService,
	UserManager<SiteUser> userManager) : IPlatformConfigurationService
{
	private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
	private readonly ISiteContext _siteContext = siteContext;
	private readonly ISiteResolver _siteResolver = siteResolver;
	private readonly ISiteUserService _siteUserService = siteUserService;
	private readonly UserManager<SiteUser> _userManager = userManager;

	public async Task<bool> IsInitialSetupRequiredAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return !await dbContext.Sites.AnyAsync(cancellationToken);
	}

	private static string NormalizeAndValidateHexColor(string color, string errorMessage)
	{
		var normalized = color?.Trim() ?? string.Empty;
		if (!Regex.IsMatch(normalized, "^#([A-Fa-f0-9]{6})$"))
		{
			throw new InvalidOperationException(errorMessage);
		}

		return normalized.ToUpperInvariant();
	}

	public async Task TestSmtpConnectionAsync(InitialSetupRequest request, CancellationToken cancellationToken = default)
	{
		var smtpHost = RequireValue(request.SmtpHost, "SMTP host is required.");
		if (request.SmtpPort <= 0 || request.SmtpPort > 65535)
		{
			throw new InvalidOperationException("SMTP port must be between 1 and 65535.");
		}

		using var tcpClient = new TcpClient();
		await tcpClient.ConnectAsync(smtpHost, request.SmtpPort, cancellationToken);
	}

	public async Task<string> SendSetupTestEmailAsync(InitialSetupRequest request, CancellationToken cancellationToken = default)
	{
		var adminEmail = NormalizeAndValidateEmail(request.AdminEmail);
		var smtpHost = RequireValue(request.SmtpHost, "SMTP host is required.");
		var smtpFromAddress = NormalizeAndValidateEmail(request.SmtpFromAddress);
		var smtpUsername = (request.SmtpUsername ?? string.Empty).Trim();
		var smtpPassword = (request.SmtpPassword ?? string.Empty).Trim();

		// Generate 6-8 digit verification code
		var verificationCode = GenerateVerificationCode();

		using var message = new MailMessage
		{
			From = new MailAddress(smtpFromAddress),
			Subject = "VBPTA setup SMTP test",
			Body = $"""
				This is a setup verification email from VBPTA.
				
				Verification code: {verificationCode}
				
				Please enter this code in the setup form to confirm this email was delivered correctly.
				This code is valid for one setup session.
				""",
			IsBodyHtml = false
		};
		message.To.Add(adminEmail);

		using var client = new SmtpClient(smtpHost, request.SmtpPort)
		{
			EnableSsl = request.UseSsl
		};
		if (!string.IsNullOrWhiteSpace(smtpUsername))
		{
			client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
		}

		cancellationToken.ThrowIfCancellationRequested();
		await client.SendMailAsync(message);
		
		return verificationCode;
	}

	public async Task CompleteInitialSetupAsync(InitialSetupRequest request, CancellationToken cancellationToken = default)
	{
		var adminEmail = NormalizeAndValidateEmail(request.AdminEmail);
		var cityWidePtaId = NormalizeAndValidatePtaId(request.CityWidePtaId);
		var rootDomain = NormalizeAndValidateDomain(request.RootDomain);
		var platformDomain = NormalizeAndValidateDomain(request.PlatformDomain);
		var cityWideName = RequireValue(request.CityWideSiteName, "City-wide PTA name is required.");
		var cityWidePrimaryColor = NormalizeAndValidateHexColor(request.CityWidePrimaryColor, "City-wide primary color is invalid.");
		var cityWideAccentColor = NormalizeAndValidateHexColor(request.CityWideAccentColor, "City-wide accent color is invalid.");
		var cityWideWelcomeText = RequireValue(request.CityWideWelcomeText, "City-wide welcome text is required.");
		var smtpFromAddress = NormalizeAndValidateEmail(request.SmtpFromAddress);

		await using (var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
		{
			if (await dbContext.Sites.AnyAsync(cancellationToken))
			{
				throw new InvalidOperationException("Initial setup has already been completed.");
			}

			var now = DateTimeOffset.UtcNow;
			dbContext.Sites.Add(new Site
			{
				Id = SeedData.DefaultAdminSiteId,
				PtaId = SeedData.DefaultAdminPtaId,
				Hostname = "admin",
				Domain = string.Empty,
				IsAdminPortal = true,
				IsCityWide = false,
				SiteName = SeedData.DefaultAdminSite.SiteName,
				LogoUrl = "/images/logo.png",
				BannerUrl = "/images/banner.png",
				PrimaryColor = SeedData.DefaultAdminSite.PrimaryColor,
				AccentColor = SeedData.DefaultAdminSite.AccentColor,
				WelcomeText = SeedData.DefaultAdminSite.WelcomeText,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			});
			dbContext.Sites.Add(new Site
			{
				Id = SeedData.DefaultCitySiteId,
				PtaId = cityWidePtaId,
				Hostname = string.Empty,
				Domain = string.Empty,
				IsAdminPortal = false,
				IsCityWide = true,
				SiteName = cityWideName,
				LogoUrl = "/images/logo.png",
				BannerUrl = "/images/banner.png",
				PrimaryColor = cityWidePrimaryColor,
				AccentColor = cityWideAccentColor,
				WelcomeText = cityWideWelcomeText,
				CreatedAtUtc = now,
				UpdatedAtUtc = now
			});
			dbContext.GlobalConfigs.Add(new GlobalConfig
			{
				RootDomain = rootDomain,
				PlatformDomain = platformDomain,
				SmtpHost = (request.SmtpHost ?? string.Empty).Trim(),
				SmtpPort = request.SmtpPort,
				SmtpFromAddress = smtpFromAddress,
				SmtpUsername = (request.SmtpUsername ?? string.Empty).Trim(),
				SmtpPassword = (request.SmtpPassword ?? string.Empty).Trim(),
				UseSsl = request.UseSsl
			});
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		var user = await _userManager.FindByEmailAsync(adminEmail);
		if (user is null)
		{
			user = new SiteUser
			{
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true
			};
			var createResult = await _userManager.CreateAsync(user);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
			}
		}

		await _siteUserService.AssignRoleAsync(user.Id, SeedData.DefaultAdminSiteId, SiteRole.Admin);
		_siteResolver.InvalidateHost(rootDomain);
		_siteResolver.InvalidateHost($"admin.{rootDomain}");
		_siteResolver.InvalidateHost(platformDomain);
		_siteResolver.InvalidateHost($"admin.{platformDomain}");
	}

	public async Task<PlatformSettingsDetail> GetPlatformSettingsAsync(string? currentUserId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		var globalConfig = await dbContext.GlobalConfigs
			.AsNoTracking()
			.OrderBy(x => x.Id)
			.FirstOrDefaultAsync(cancellationToken)
			?? SeedData.DefaultGlobalConfig;
		var cityWideSite = await dbContext.Sites.AsNoTracking().SingleOrDefaultAsync(x => x.IsCityWide, cancellationToken)
			?? throw new InvalidOperationException("No city-wide site exists.");

		return new PlatformSettingsDetail
		{
			RootDomain = globalConfig.RootDomain,
			PlatformDomain = globalConfig.PlatformDomain,
			SmtpHost = globalConfig.SmtpHost,
			SmtpPort = globalConfig.SmtpPort,
			SmtpFromAddress = globalConfig.SmtpFromAddress,
			SmtpUsername = globalConfig.SmtpUsername,
			SmtpPassword = globalConfig.SmtpPassword,
			UseSsl = globalConfig.UseSsl,
			CityWideSiteId = cityWideSite.Id,
			CityWidePtaId = cityWideSite.PtaId,
			CityWideSiteName = cityWideSite.SiteName
		};
	}

	public async Task UpdatePlatformSettingsAsync(string? currentUserId, PlatformSettingsUpdateRequest request, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);

		var rootDomain = NormalizeAndValidateDomain(request.RootDomain);
		var platformDomain = NormalizeAndValidateDomain(request.PlatformDomain);
		var cityWidePtaId = NormalizeAndValidatePtaId(request.CityWidePtaId);
		var cityWideSiteName = RequireValue(request.CityWideSiteName, "City-wide PTA name is required.");
		var smtpHost = (request.SmtpHost ?? string.Empty).Trim();
		var smtpFromAddress = NormalizeAndValidateEmail(request.SmtpFromAddress);
		var smtpUser = (request.SmtpUsername ?? string.Empty).Trim();
		var smtpPassword = (request.SmtpPassword ?? string.Empty).Trim();

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var globalConfig = await dbContext.GlobalConfigs
			.OrderBy(x => x.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (globalConfig is null)
		{
			globalConfig = new GlobalConfig();
			dbContext.GlobalConfigs.Add(globalConfig);
		}

		var cityWideSite = await dbContext.Sites.SingleOrDefaultAsync(x => x.IsCityWide, cancellationToken);
		if (cityWideSite is null)
		{
			throw new InvalidOperationException("No city-wide site exists.");
		}

		globalConfig.RootDomain = rootDomain;
		globalConfig.PlatformDomain = platformDomain;
		globalConfig.SmtpHost = smtpHost;
		globalConfig.SmtpPort = request.SmtpPort;
		globalConfig.SmtpFromAddress = smtpFromAddress;
		globalConfig.SmtpUsername = smtpUser;
		globalConfig.SmtpPassword = smtpPassword;
		globalConfig.UseSsl = request.UseSsl;
		cityWideSite.PtaId = cityWidePtaId;
		cityWideSite.SiteName = cityWideSiteName;
		cityWideSite.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
		_siteResolver.InvalidateHost(rootDomain);
		_siteResolver.InvalidateHost($"admin.{rootDomain}");
		_siteResolver.InvalidateHost(platformDomain);
		_siteResolver.InvalidateHost($"admin.{platformDomain}");
	}

	private async Task EnsureAuthorizedAsync(string? currentUserId)
	{
		if (!_siteContext.IsAdminContext)
		{
			throw new InvalidOperationException("Admin actions are only available in admin site context.");
		}

		var isAdmin = await _siteContext.UserHasRoleAtCurrentSiteAsync(currentUserId, SiteRole.Admin);
		if (!isAdmin)
		{
			throw new InvalidOperationException("You must be a global admin to access this page.");
		}
	}

	private static string NormalizeAndValidateEmail(string email)
	{
		var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
		if (!Regex.IsMatch(normalized, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
		{
			throw new InvalidOperationException("Admin email is not valid.");
		}

		return normalized;
	}

	private static string NormalizeAndValidatePtaId(string ptaId)
	{
		var normalized = ptaId?.Trim() ?? string.Empty;
		if (!Regex.IsMatch(normalized, "^\\d{8}$"))
		{
			throw new InvalidOperationException("PTA ID must be exactly 8 digits.");
		}

		return normalized;
	}

	private static string NormalizeAndValidateDomain(string domain)
	{
		var normalized = domain?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			throw new InvalidOperationException("Domain is required.");
		}

		if (normalized.Contains("://", StringComparison.Ordinal) ||
			normalized.Contains('/', StringComparison.Ordinal) ||
			normalized.Contains(':', StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Domain must be host only (no scheme, path, or port).");
		}

		if (!Regex.IsMatch(normalized, "^[a-z0-9.-]+$"))
		{
			throw new InvalidOperationException("Domain contains unsupported characters.");
		}

		return normalized;
	}

	private static string RequireValue(string? value, string errorMessage)
	{
		var normalized = value?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			throw new InvalidOperationException(errorMessage);
		}

		return normalized;
	}

	private static string GenerateVerificationCode()
	{
		// Generate a random 6-8 digit code
		var random = new Random();
		var codeLength = random.Next(6, 9); // 6 to 8 digits
		var code = random.Next((int)Math.Pow(10, codeLength - 1), (int)Math.Pow(10, codeLength));
		return code.ToString();
	}
}

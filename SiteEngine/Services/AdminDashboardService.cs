using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Identity;
using SiteEngine.Sites;
using System.Text.RegularExpressions;

namespace SiteEngine.Services;

public class AdminDashboardService(
	IDbContextFactory<AppDbContext> dbContextFactory,
	ISiteContext siteContext,
	ISiteResolver siteResolver,
	ISitePublicAssetService sitePublicAssetService,
	UserManager<SiteUser> userManager) : IAdminDashboardService
{
	private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
	private readonly ISiteContext _siteContext = siteContext;
	private readonly ISiteResolver _siteResolver = siteResolver;
	private readonly ISitePublicAssetService _sitePublicAssetService = sitePublicAssetService;
	private readonly UserManager<SiteUser> _userManager = userManager;

	public async Task<AdminDashboardOverview> GetOverviewAsync(string? currentUserId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		var totalSites = await dbContext.Sites.CountAsync(cancellationToken);
		var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
		var assignedUsers = await dbContext.SiteUserRoles
			.Select(x => x.UserId)
			.Distinct()
			.CountAsync(cancellationToken);
		var globalAdmins = await dbContext.SiteUserRoles
			.Where(x => x.SiteId == SeedData.DefaultAdminSiteId && x.Role == SiteRole.Admin)
			.Select(x => x.UserId)
			.Distinct()
			.CountAsync(cancellationToken);

		return new AdminDashboardOverview
		{
			TotalSites = totalSites,
			TotalUsers = totalUsers,
			AssignedUsers = assignedUsers,
			GlobalAdmins = globalAdmins
		};
	}

	public async Task<IReadOnlyList<AdminSiteSummary>> GetSiteSummariesAsync(string? currentUserId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.Sites
			.AsNoTracking()
			.OrderBy(x => x.SiteName)
			.Select(x => new AdminSiteSummary
			{
				SiteId = x.Id,
				PtaId = x.PtaId,
				Hostname = x.Hostname,
				Domain = x.Domain,
				SiteName = x.SiteName,
				IsAdminPortal = x.IsAdminPortal,
				IsCityWide = x.IsCityWide,
				AnnouncementCount = x.Announcements.Count,
				EventCount = x.Events.Count,
				HealthStatus = "Healthy"
			})
			.ToListAsync(cancellationToken);
	}

	public async Task<AdminSiteDetail?> GetSiteDetailAsync(string? currentUserId, Guid siteId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		var site = await dbContext.Sites
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Id == siteId, cancellationToken);
		if (site is null)
		{
			return null;
		}

		var announcementCount = await dbContext.Announcements
			.AsNoTracking()
			.CountAsync(x => x.SiteId == siteId, cancellationToken);
		var eventCount = await dbContext.Events
			.AsNoTracking()
			.CountAsync(x => x.SiteId == siteId, cancellationToken);
		var assignedUsers = await dbContext.SiteUserRoles
			.AsNoTracking()
			.Where(x => x.SiteId == siteId)
			.Select(x => x.UserId)
			.Distinct()
			.CountAsync(cancellationToken);

		var roleCounts = await dbContext.SiteUserRoles
			.AsNoTracking()
			.Where(x => x.SiteId == siteId)
			.GroupBy(x => x.Role)
			.Select(g => new { Role = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
			.ToListAsync(cancellationToken);

		return new AdminSiteDetail
		{
			SiteId = site.Id,
			PtaId = site.PtaId,
			Hostname = site.Hostname,
			Domain = site.Domain,
			SiteName = site.SiteName,
			IsAdminPortal = site.IsAdminPortal,
			IsCityWide = site.IsCityWide,
			LogoUrl = site.LogoUrl,
			BannerUrl = site.BannerUrl,
			PrimaryColor = site.PrimaryColor,
			AccentColor = site.AccentColor,
			WelcomeText = site.WelcomeText,
			AnnouncementCount = announcementCount,
			EventCount = eventCount,
			AssignedUsers = assignedUsers,
			AdminCount = roleCounts.SingleOrDefault(x => x.Role == SiteRole.Admin)?.Count ?? 0,
			BoardMemberCount = roleCounts.SingleOrDefault(x => x.Role == SiteRole.BoardMember)?.Count ?? 0,
			VolunteerCount = roleCounts.SingleOrDefault(x => x.Role == SiteRole.Volunteer)?.Count ?? 0
		};
	}

	public async Task<Guid> CreateSiteAsync(string? currentUserId, AdminCreateSiteRequest request, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		var normalizedPtaId = NormalizeAndValidatePtaId(request.PtaId);
		var normalizedHostname = NormalizeAndValidateHostname(request.Hostname);
		var normalizedDomain = NormalizeAndValidateDomain(request.Domain);
		ValidateRoutingInputs(normalizedHostname, normalizedDomain, request.IsCityWide);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		var ptaIdExists = await dbContext.Sites
			.AsNoTracking()
			.AnyAsync(x => x.PtaId == normalizedPtaId, cancellationToken);
		if (ptaIdExists)
		{
			throw new InvalidOperationException($"A site with PTA ID '{normalizedPtaId}' already exists.");
		}

		if (!string.IsNullOrWhiteSpace(normalizedHostname))
		{
			var hostnameExists = await dbContext.Sites
				.AsNoTracking()
				.AnyAsync(x => x.Hostname == normalizedHostname, cancellationToken);
			if (hostnameExists)
			{
				throw new InvalidOperationException($"A site with hostname '{normalizedHostname}' already exists.");
			}
		}

		if (!string.IsNullOrWhiteSpace(normalizedDomain))
		{
			var domainExists = await dbContext.Sites
				.AsNoTracking()
				.AnyAsync(x => x.Domain == normalizedDomain, cancellationToken);
			if (domainExists)
			{
				throw new InvalidOperationException($"A site with domain '{normalizedDomain}' already exists.");
			}
		}

		if (request.IsCityWide)
		{
			var cityWideExists = await dbContext.Sites
				.AsNoTracking()
				.AnyAsync(x => x.IsCityWide, cancellationToken);
			if (cityWideExists)
			{
				throw new InvalidOperationException("Only one city-wide site is allowed.");
			}
		}

		var now = DateTimeOffset.UtcNow;
		var useDefaultLogo = string.IsNullOrWhiteSpace(request.LogoUrl);
		var useDefaultBanner = string.IsNullOrWhiteSpace(request.BannerUrl);
		var site = new Site
		{
			Id = Guid.NewGuid(),
			PtaId = normalizedPtaId,
			Hostname = normalizedHostname,
			Domain = normalizedDomain,
			IsAdminPortal = false,
			IsCityWide = request.IsCityWide,
			SiteName = GetValueOrDefault(request.SiteName, SeedData.DefaultCitySite.SiteName),
			LogoUrl = useDefaultLogo
				? "images/logo.png"
				: GetValueOrDefault(request.LogoUrl, SeedData.DefaultCitySite.LogoUrl),
			BannerUrl = useDefaultBanner
				? "images/banner.png"
				: GetValueOrDefault(request.BannerUrl, SeedData.DefaultCitySite.BannerUrl),
			PrimaryColor = GetValueOrDefault(request.PrimaryColor, SeedData.DefaultCitySite.PrimaryColor),
			AccentColor = GetValueOrDefault(request.AccentColor, SeedData.DefaultCitySite.AccentColor),
			WelcomeText = GetValueOrDefault(request.WelcomeText, SeedData.DefaultCitySite.WelcomeText),
			CreatedAtUtc = now,
			UpdatedAtUtc = now
		};

		dbContext.Sites.Add(site);
		await dbContext.SaveChangesAsync(cancellationToken);
		await _sitePublicAssetService.EnsureSiteFoldersAsync(normalizedPtaId, seedDefaults: useDefaultLogo || useDefaultBanner, cancellationToken);
		if (!string.IsNullOrWhiteSpace(normalizedHostname))
		{
			_siteResolver.InvalidateHost(normalizedHostname);
		}
		if (!string.IsNullOrWhiteSpace(normalizedDomain))
		{
			_siteResolver.InvalidateHost(normalizedDomain);
		}
		return site.Id;
	}

	public async Task<bool> UpdateSiteAsync(string? currentUserId, Guid siteId, AdminUpdateSiteRequest request, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		var normalizedPtaId = NormalizeAndValidatePtaId(request.PtaId);
		var normalizedHostname = NormalizeAndValidateHostname(request.Hostname);
		var normalizedDomain = NormalizeAndValidateDomain(request.Domain);
		ValidateRoutingInputs(normalizedHostname, normalizedDomain, request.IsCityWide);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var site = await dbContext.Sites.SingleOrDefaultAsync(x => x.Id == siteId, cancellationToken);
		if (site is null)
		{
			return false;
		}

		var duplicatePtaId = await dbContext.Sites
			.AsNoTracking()
			.AnyAsync(x => x.Id != siteId && x.PtaId == normalizedPtaId, cancellationToken);
		if (duplicatePtaId)
		{
			throw new InvalidOperationException($"A site with PTA ID '{normalizedPtaId}' already exists.");
		}

		var duplicateHostname = await dbContext.Sites
			.AsNoTracking()
			.AnyAsync(x => x.Id != siteId && x.Hostname == normalizedHostname && x.Hostname != string.Empty, cancellationToken);
		if (!string.IsNullOrWhiteSpace(normalizedHostname) && duplicateHostname)
		{
			throw new InvalidOperationException($"A site with hostname '{normalizedHostname}' already exists.");
		}

		var duplicateDomain = await dbContext.Sites
			.AsNoTracking()
			.AnyAsync(x => x.Id != siteId && x.Domain == normalizedDomain && x.Domain != string.Empty, cancellationToken);
		if (!string.IsNullOrWhiteSpace(normalizedDomain) && duplicateDomain)
		{
			throw new InvalidOperationException($"A site with domain '{normalizedDomain}' already exists.");
		}

		if (request.IsCityWide)
		{
			var duplicateCityWide = await dbContext.Sites
				.AsNoTracking()
				.AnyAsync(x => x.Id != siteId && x.IsCityWide, cancellationToken);
			if (duplicateCityWide)
			{
				throw new InvalidOperationException("Only one city-wide site is allowed.");
			}
		}

		var originalHostname = site.Hostname;
		var originalDomain = site.Domain;
		var originalPtaId = site.PtaId;
		site.PtaId = normalizedPtaId;
		site.Hostname = normalizedHostname;
		site.Domain = normalizedDomain;
		site.IsCityWide = request.IsCityWide;
		site.SiteName = GetValueOrDefault(request.SiteName, site.SiteName);
		site.LogoUrl = GetValueOrDefault(request.LogoUrl, site.LogoUrl);
		site.BannerUrl = GetValueOrDefault(request.BannerUrl, site.BannerUrl);
		site.PrimaryColor = GetValueOrDefault(request.PrimaryColor, site.PrimaryColor);
		site.AccentColor = GetValueOrDefault(request.AccentColor, site.AccentColor);
		site.WelcomeText = GetValueOrDefault(request.WelcomeText, site.WelcomeText);
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await dbContext.SaveChangesAsync(cancellationToken);
		await _sitePublicAssetService.RenameSiteFolderAsync(originalPtaId, normalizedPtaId, cancellationToken);
		await _sitePublicAssetService.EnsureSiteFoldersAsync(normalizedPtaId, seedDefaults: false, cancellationToken);
		if (!string.IsNullOrWhiteSpace(originalHostname))
		{
			_siteResolver.InvalidateHost(originalHostname);
		}
		if (!string.IsNullOrWhiteSpace(normalizedHostname))
		{
			_siteResolver.InvalidateHost(normalizedHostname);
		}
		if (!string.IsNullOrWhiteSpace(originalDomain))
		{
			_siteResolver.InvalidateHost(originalDomain);
		}
		if (!string.IsNullOrWhiteSpace(normalizedDomain))
		{
			_siteResolver.InvalidateHost(normalizedDomain);
		}
		return true;
	}

	public async Task<IReadOnlyList<AdminUserSummary>> GetUserSummariesAsync(string? currentUserId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

		var summaries = await dbContext.Users
			.AsNoTracking()
			.OrderBy(x => x.Email)
			.Select(x => new AdminUserSummary
			{
				UserId = x.Id,
				Email = x.Email ?? x.UserName ?? string.Empty,
				IsGlobalAdmin = dbContext.SiteUserRoles.Any(r => r.UserId == x.Id && r.SiteId == SeedData.DefaultAdminSiteId && r.Role == SiteRole.Admin),
				AssignedSiteCount = dbContext.SiteUserRoles
					.Where(r => r.UserId == x.Id)
					.Select(r => r.SiteId)
					.Distinct()
					.Count()
			})
			.ToListAsync(cancellationToken);

		return summaries;
	}

	public async Task<AdminUserDetail?> GetUserDetailAsync(string? currentUserId, string userId, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		if (string.IsNullOrWhiteSpace(userId))
		{
			return null;
		}

		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var user = await dbContext.Users
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null)
		{
			return null;
		}

		var siteRoles = await dbContext.SiteUserRoles
			.AsNoTracking()
			.Where(x => x.UserId == userId)
			.Include(x => x.Site)
			.Select(x => new AdminUserSiteRole
			{
				SiteId = x.SiteId,
				SiteName = x.Site.SiteName,
				Hostname = x.Site.Hostname,
				Role = x.Role
			})
			.OrderBy(x => x.SiteName)
			.ThenBy(x => x.Role)
			.ToListAsync(cancellationToken);

		return new AdminUserDetail
		{
			UserId = user.Id,
			Email = user.Email ?? user.UserName ?? string.Empty,
			IsGlobalAdmin = siteRoles.Any(x => x.SiteId == SeedData.DefaultAdminSiteId && x.Role == SiteRole.Admin),
			SiteRoles = siteRoles
		};
	}

	public async Task<string> CreateUserAsync(string? currentUserId, AdminCreateUserRequest request, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		var normalizedEmail = NormalizeAndValidateEmail(request.Email);
		var existing = await _userManager.FindByEmailAsync(normalizedEmail);
		if (existing is not null)
		{
			throw new InvalidOperationException("A user with that email already exists.");
		}

		var user = new SiteUser
		{
			UserName = normalizedEmail,
			Email = normalizedEmail,
			EmailConfirmed = true
		};
		var result = await _userManager.CreateAsync(user);
		if (!result.Succeeded)
		{
			throw new InvalidOperationException(BuildIdentityErrors(result));
		}

		await SetGlobalAdminAsync(user.Id, request.IsGlobalAdmin, cancellationToken);
		return user.Id;
	}

	public async Task<bool> UpdateUserAsync(string? currentUserId, string userId, AdminUpdateUserRequest request, CancellationToken cancellationToken = default)
	{
		await EnsureAuthorizedAsync(currentUserId);
		if (string.IsNullOrWhiteSpace(userId))
		{
			return false;
		}

		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
		{
			return false;
		}

		var normalizedEmail = NormalizeAndValidateEmail(request.Email);
		if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
		{
			var existing = await _userManager.FindByEmailAsync(normalizedEmail);
			if (existing is not null && !string.Equals(existing.Id, userId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("A user with that email already exists.");
			}

			var setUserName = await _userManager.SetUserNameAsync(user, normalizedEmail);
			if (!setUserName.Succeeded)
			{
				throw new InvalidOperationException(BuildIdentityErrors(setUserName));
			}

			var setEmail = await _userManager.SetEmailAsync(user, normalizedEmail);
			if (!setEmail.Succeeded)
			{
				throw new InvalidOperationException(BuildIdentityErrors(setEmail));
			}
		}

		await SetGlobalAdminAsync(userId, request.IsGlobalAdmin, cancellationToken);
		return true;
	}

	private async Task SetGlobalAdminAsync(string userId, bool shouldBeGlobalAdmin, CancellationToken cancellationToken)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var existing = await dbContext.SiteUserRoles
			.SingleOrDefaultAsync(
				x => x.UserId == userId && x.SiteId == SeedData.DefaultAdminSiteId && x.Role == SiteRole.Admin,
				cancellationToken);

		if (shouldBeGlobalAdmin && existing is null)
		{
			dbContext.SiteUserRoles.Add(new SiteUserRole
			{
				UserId = userId,
				SiteId = SeedData.DefaultAdminSiteId,
				Role = SiteRole.Admin,
				CreatedAt = DateTime.UtcNow
			});
			await dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		if (!shouldBeGlobalAdmin && existing is not null)
		{
			dbContext.SiteUserRoles.Remove(existing);
			await dbContext.SaveChangesAsync(cancellationToken);
		}
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

	private static string NormalizeAndValidateHostname(string hostname)
	{
		var normalized = hostname?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return string.Empty;
		}

		if (normalized.Contains("://", StringComparison.Ordinal) ||
			normalized.Contains('/', StringComparison.Ordinal) ||
			normalized.Contains(':', StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Hostname must not include scheme, path, or port.");
		}

		if (!Regex.IsMatch(normalized, "^[a-z0-9.-]+$"))
		{
			throw new InvalidOperationException("Hostname contains unsupported characters.");
		}

		return normalized;
	}

	private static string NormalizeAndValidateDomain(string domain)
	{
		var normalized = domain?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return string.Empty;
		}

		if (normalized.Contains("://", StringComparison.Ordinal) ||
			normalized.Contains('/', StringComparison.Ordinal) ||
			normalized.Contains(':', StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Domain must not include scheme, path, or port.");
		}

		if (!Regex.IsMatch(normalized, "^[a-z0-9.-]+$"))
		{
			throw new InvalidOperationException("Domain contains unsupported characters.");
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

	private static void ValidateRoutingInputs(string hostname, string domain, bool isCityWide)
	{
		if (isCityWide)
		{
			if (!string.IsNullOrWhiteSpace(hostname) || !string.IsNullOrWhiteSpace(domain))
			{
				throw new InvalidOperationException("City-wide sites cannot have Hostname or Domain values.");
			}

			return;
		}

		if (string.IsNullOrWhiteSpace(hostname) && string.IsNullOrWhiteSpace(domain))
		{
			throw new InvalidOperationException("Provide either a Hostname or a Domain.");
		}
	}

	private static string NormalizeAndValidateEmail(string email)
	{
		var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			throw new InvalidOperationException("Email is required.");
		}

		if (!Regex.IsMatch(normalized, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
		{
			throw new InvalidOperationException("Email is not valid.");
		}

		return normalized;
	}

	private static string GetValueOrDefault(string? value, string fallback)
	{
		var normalized = value?.Trim();
		return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
	}

	private static string BuildIdentityErrors(IdentityResult result)
	{
		return string.Join("; ", result.Errors.Select(x => x.Description));
	}
}

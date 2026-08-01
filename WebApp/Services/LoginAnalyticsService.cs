using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Enums;

namespace WebApp.Services;

public record RecentLoginEntry(string UserId, string Email, DateTimeOffset LoginUtc, Guid? SiteId, string? SiteName, string IpAddress, LoginNetworkType NetworkType);
public record SiteLoginCount(Guid? SiteId, string SiteName, int LoginCount);
public record NetworkBreakdownEntry(LoginNetworkType NetworkType, int LoginCount);
public record YearlySummaryEntry(string Email, string SchoolYear, string? SiteName, int TotalLogins, int SchoolNetworkLogins, int HomeNetworkLogins, int MobileNetworkLogins, int UniqueSites, DateTimeOffset FirstLoginUtc, DateTimeOffset LastLoginUtc);
public record UserLoginEntry(DateTimeOffset LoginUtc, string? SiteName, string IpAddress);

// Read-only queries behind the Global Admin "Login Analytics" tab and the
// Users tab's per-user activity section. Pairs with LoginTrackingService,
// which owns the writes these read.
public class LoginAnalyticsService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<List<RecentLoginEntry>> GetRecentLoginsAsync(int take = 50, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.LoginHistories
			.OrderByDescending(h => h.LoginUtc)
			.Take(take)
			.Select(h => new RecentLoginEntry(
				h.UserId,
				db.Users.Where(u => u.Id == h.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
				h.LoginUtc,
				h.SiteId,
				h.Site != null ? h.Site.SiteName : null,
				h.IpAddress,
				h.NetworkType))
			.ToListAsync(cancellationToken);
	}

	public async Task<List<SiteLoginCount>> GetLoginsPerSiteAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var counts = await db.LoginHistories
			.GroupBy(h => h.SiteId)
			.Select(g => new { SiteId = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		var siteNames = await db.Sites.ToDictionaryAsync(s => s.Id, s => s.SiteName, cancellationToken);

		return counts
			.OrderByDescending(x => x.Count)
			.Select(x => new SiteLoginCount(
				x.SiteId,
				x.SiteId is Guid siteId && siteNames.TryGetValue(siteId, out var name) ? name : "Unknown site",
				x.Count))
			.ToList();
	}

	// Always returns all four LoginNetworkType values, zero-filled, so the UI
	// can render a stable breakdown even before classification exists (#4 —
	// every login is LoginNetworkType.Unknown today).
	public async Task<List<NetworkBreakdownEntry>> GetNetworkBreakdownAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var counts = await db.LoginHistories
			.GroupBy(h => h.NetworkType)
			.Select(g => new { NetworkType = g.Key, Count = g.Count() })
			.ToDictionaryAsync(x => x.NetworkType, x => x.Count, cancellationToken);

		return Enum.GetValues<LoginNetworkType>()
			.Select(t => new NetworkBreakdownEntry(t, counts.GetValueOrDefault(t)))
			.ToList();
	}

	public async Task<List<YearlySummaryEntry>> GetYearlySummariesAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.LoginHistorySummaries
			.OrderByDescending(s => s.SchoolYear)
			.ThenByDescending(s => s.TotalLogins)
			.Select(s => new YearlySummaryEntry(
				db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
				s.SchoolYear,
				s.Site != null ? s.Site.SiteName : null,
				s.TotalLogins,
				s.SchoolNetworkLogins,
				s.HomeNetworkLogins,
				s.MobileNetworkLogins,
				s.UniqueSites,
				s.FirstLoginUtc,
				s.LastLoginUtc))
			.ToListAsync(cancellationToken);
	}

	public async Task<List<UserLoginEntry>> GetUserLoginHistoryAsync(string userId, int take = 10, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.LoginHistories
			.Where(h => h.UserId == userId)
			.OrderByDescending(h => h.LoginUtc)
			.Take(take)
			.Select(h => new UserLoginEntry(h.LoginUtc, h.Site != null ? h.Site.SiteName : null, h.IpAddress))
			.ToListAsync(cancellationToken);
	}
}

using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;
using SiteEngine.Identity;

namespace WebApp.Services;

// Login analytics: called once per successful sign-in from
// PasswordlessSignInService, plus a not-yet-scheduled yearly cleanup that
// rolls LoginHistory up into LoginHistorySummary.
public class LoginTrackingService(IDbContextFactory<AppDbContext> dbFactory, SiteRoleResolver siteRoleResolver)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
	private readonly SiteRoleResolver _siteRoleResolver = siteRoleResolver;

	// Updates the ApplicationUser's login counters and appends a LoginHistory
	// row. Called after SignInAsync succeeds — never on a failed attempt.
	public async Task RecordLoginAsync(
		string userId,
		Guid? siteId,
		string? ipAddress,
		CancellationToken cancellationToken = default)
	{
		// Global Admins (SuperAdmin on the Portal) sign into Divisions/Units for
		// maintenance and support, not as members — none of that should count as
		// site activity, so it's excluded here before anything is recorded.
		var portalRole = await _siteRoleResolver.ResolveAsync(userId, SeedData.DefaultPortalSiteId, cancellationToken);
		if (portalRole == SiteRole.SuperAdmin)
			return;

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
			?? throw new InvalidOperationException($"User '{userId}' not found.");

		var now = DateTimeOffset.UtcNow;

		if (user.FirstLoginUtc is null)
			user.FirstLoginUtc = now;

		user.LastLoginUtc = now;
		user.LastLoginSiteId = siteId;
		user.LoginCount++;

		db.LoginHistories.Add(new LoginHistory
		{
			UserId = userId,
			LoginUtc = now,
			SiteId = siteId,
			IpAddress = ipAddress ?? string.Empty
		});

		await db.SaveChangesAsync(cancellationToken);
	}

	// Placeholder yearly maintenance — not wired to a scheduler yet. Intended
	// to run once at the end of each school year (see SchoolYear.Current()):
	// roll that year's LoginHistory into LoginHistorySummary, then prune
	// LoginHistory rows older than 3 school years. User cleanup is
	// deliberately out of scope (CLAUDE.md doesn't call for it here).
	public async Task RunYearlyCleanupAsync(string schoolYear, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var (yearStart, yearEnd) = SchoolYearRange(schoolYear);

		var yearRows = await db.LoginHistories
			.Where(h => h.LoginUtc >= yearStart && h.LoginUtc < yearEnd)
			.ToListAsync(cancellationToken);

		var byUser = yearRows.GroupBy(h => h.UserId);

		foreach (var group in byUser)
		{
			var bySite = group
				.GroupBy(h => h.SiteId)
				.OrderByDescending(g => g.Count())
				.ToList();

			var primarySiteId = bySite.FirstOrDefault()?.Key;

			var summary = await db.LoginHistorySummaries.FirstOrDefaultAsync(
				s => s.UserId == group.Key && s.SchoolYear == schoolYear,
				cancellationToken);

			if (summary is null)
			{
				summary = new LoginHistorySummary
				{
					UserId = group.Key,
					SchoolYear = schoolYear
				};
				db.LoginHistorySummaries.Add(summary);
			}

			summary.SiteId = primarySiteId;
			summary.TotalLogins = group.Count();
			summary.SchoolNetworkLogins = group.Count(h => h.NetworkType == LoginNetworkType.School);
			summary.HomeNetworkLogins = group.Count(h => h.NetworkType == LoginNetworkType.Home);
			summary.MobileNetworkLogins = group.Count(h => h.NetworkType == LoginNetworkType.Mobile);
			summary.UniqueSites = bySite.Count(g => g.Key is not null);
			summary.FirstLoginUtc = group.Min(h => h.LoginUtc);
			summary.LastLoginUtc = group.Max(h => h.LoginUtc);
		}

		await db.SaveChangesAsync(cancellationToken);

		var cutoff = new DateTimeOffset(yearStart.Year - 3, 7, 1, 0, 0, 0, TimeSpan.Zero);
		await db.LoginHistories
			.Where(h => h.LoginUtc < cutoff)
			.ExecuteDeleteAsync(cancellationToken);
	}

	// School years run July 1 – June 30 (SchoolYear.cs). Parses "2026-2027" back
	// into that range so cleanup can filter LoginHistory by it.
	private static (DateTimeOffset start, DateTimeOffset end) SchoolYearRange(string schoolYear)
	{
		var startYear = int.Parse(schoolYear.Split('-')[0]);
		var start = new DateTimeOffset(startYear, 7, 1, 0, 0, 0, TimeSpan.Zero);
		var end = start.AddYears(1);
		return (start, end);
	}
}

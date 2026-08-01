using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Identity;

namespace WebApp.Services;

public class ProfileService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<SiteUser?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.SiteUsers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId, cancellationToken);
	}

	// Required fields today: FirstName, DisplayName. More may be added later.
	public static bool IsComplete(SiteUser? siteUser) =>
		siteUser is not null &&
		!string.IsNullOrWhiteSpace(siteUser.FirstName) &&
		!string.IsNullOrWhiteSpace(siteUser.DisplayName);

	public async Task UpdateAsync(
		string identityUserId,
		string firstName,
		string lastName,
		string displayName,
		CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var siteUser = await db.SiteUsers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId, cancellationToken)
			?? throw new InvalidOperationException($"No SiteUser found for identity user '{identityUserId}'.");

		siteUser.FirstName = firstName.Trim();
		siteUser.LastName = lastName?.Trim() ?? string.Empty;
		siteUser.DisplayName = displayName.Trim();
		siteUser.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);
	}
}

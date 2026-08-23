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

	// Required field today: DisplayName. FirstName/LastName remain on SiteUser
	// (SetupService still sets FirstName for the wizard-created admin) but the
	// self-service profile UI no longer collects them separately.
	public static bool IsComplete(SiteUser? siteUser) =>
		siteUser is not null && !string.IsNullOrWhiteSpace(siteUser.DisplayName);

	public async Task UpdateAsync(
		string identityUserId,
		string displayName,
		CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var siteUser = await db.SiteUsers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId, cancellationToken)
			?? throw new InvalidOperationException($"No SiteUser found for identity user '{identityUserId}'.");

		siteUser.DisplayName = displayName.Trim();
		siteUser.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);
	}
}

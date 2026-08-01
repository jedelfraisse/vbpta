using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Enums;
using SiteEngine.Identity;

namespace WebApp.Services;

// Read-only lookup of an existing school-year membership record for a Division
// or Local Unit site. It does not create membership — that's out of scope
// until the GiveBacks CSV import tool (CLAUDE.md #8) exists to actually grant
// it. Once a SiteUserRole/CustomRole "Member" row exists for a user, this is
// what lets the login flow recognize it automatically.
public class MembershipLookupService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<SiteUserRole?> FindCurrentMembershipAsync(
		Guid siteId,
		Guid siteUserId,
		string schoolYear,
		CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.SiteUserRoles
			.Include(r => r.CustomRole)
			.FirstOrDefaultAsync(r =>
				r.SiteId == siteId &&
				r.SiteUserId == siteUserId &&
				r.SchoolYear == schoolYear &&
				(r.Role == SiteRole.Member || (r.CustomRole != null && r.CustomRole.Name == "Member")),
				cancellationToken);
	}
}

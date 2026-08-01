using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Enums;

namespace WebApp.Services;

// Resolves which SiteRole an authenticated user should be treated as for a
// given site, for nav/menu purposes. A user can hold several SiteUserRole
// rows for the same site (e.g. SiteAdmin + SuperAdmin) — the
// highest-privilege one wins, since SiteRole's declared enum order (Viewer
// ... SuperAdmin) IS its privilege order. An authenticated user with no
// matching role at all still resolves to Viewer, never null — null stays
// reserved for "not authenticated," decided by the caller.
public class SiteRoleResolver(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<SiteRole> ResolveAsync(string identityUserId, Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var siteUserId = await db.SiteUsers
			.Where(u => u.IdentityUserId == identityUserId)
			.Select(u => (Guid?)u.Id)
			.FirstOrDefaultAsync(cancellationToken);

		if (siteUserId is null)
			return SiteRole.Viewer;

		var roles = await db.SiteUserRoles
			.Where(r => r.SiteUserId == siteUserId && r.SiteId == siteId && r.Role != null)
			.Select(r => r.Role!.Value)
			.ToListAsync(cancellationToken);

		return roles.Count > 0 ? roles.Max() : SiteRole.Viewer;
	}

	// Dashboard/admin surfaces gate sections on privilege, not exact match — a
	// DivisionAdmin (or SuperAdmin) should see everything a SiteAdmin sees. A
	// user's overall standing may come from a role held on the Portal site
	// itself (portalRole) or from a role held on any Division/Unit they belong
	// to (membershipRoles), so the effective role for gating is the max of both.
	public static SiteRole HighestRole(SiteRole portalRole, IEnumerable<SiteRole?> membershipRoles) =>
		membershipRoles.Select(r => r ?? SiteRole.Viewer).Append(portalRole).Max();
}

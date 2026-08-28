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

	// A Local Unit's own roles always count. A DivisionAdmin role held on its
	// PARENT Division also counts — SiteRole.DivisionAdmin's own enum comment
	// ("Division-level admin (Division + Units)") already promises this scope,
	// so someone administering a Division shouldn't have to be separately
	// enrolled as a member of every one of its Local Units just to be
	// recognized there. Deliberately narrow: only DivisionAdmin (or higher —
	// there's nothing above it but SuperAdmin) held on the parent cascades
	// down; a plain Member/Officer/SiteAdmin on the Division does NOT, since
	// SiteAdmin-of-the-Division and admin-of-its-Units are meant to stay
	// distinct roles. SuperAdmin is unaffected by any of this — callers that
	// need "is this user a global admin" already resolve that separately
	// against the Portal site's own id (see SiteLayoutBase.
	// RefreshMembersOnlyGateAsync, GlobalAdminLayout, etc.), since that's
	// where a SuperAdmin's role is actually assigned, not on every site they
	// might need to reach.
	public async Task<SiteRole> ResolveAsync(string identityUserId, Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var siteUserId = await db.SiteUsers
			.Where(u => u.IdentityUserId == identityUserId)
			.Select(u => (Guid?)u.Id)
			.FirstOrDefaultAsync(cancellationToken);

		if (siteUserId is null)
			return SiteRole.Viewer;

		var parentSiteId = await db.Sites
			.Where(s => s.Id == siteId)
			.Select(s => s.ParentSiteId)
			.FirstOrDefaultAsync(cancellationToken);

		var relevantSiteIds = parentSiteId is Guid parentId
			? new[] { siteId, parentId }
			: new[] { siteId };

		var roles = await db.SiteUserRoles
			.Where(r => r.SiteUserId == siteUserId && relevantSiteIds.Contains(r.SiteId) && r.Role != null)
			.Select(r => new { r.SiteId, Role = r.Role!.Value })
			.ToListAsync(cancellationToken);

		var effectiveRoles = roles
			.Where(r => r.SiteId == siteId || r.Role >= SiteRole.DivisionAdmin)
			.Select(r => r.Role)
			.ToList();

		return effectiveRoles.Count > 0 ? effectiveRoles.Max() : SiteRole.Viewer;
	}

	// Dashboard/admin surfaces gate sections on privilege, not exact match — a
	// DivisionAdmin (or SuperAdmin) should see everything a SiteAdmin sees. A
	// user's overall standing may come from a role held on the Portal site
	// itself (portalRole) or from a role held on any Division/Unit they belong
	// to (membershipRoles), so the effective role for gating is the max of both.
	public static SiteRole HighestRole(SiteRole portalRole, IEnumerable<SiteRole?> membershipRoles) =>
		membershipRoles.Select(r => r ?? SiteRole.Viewer).Append(portalRole).Max();
}

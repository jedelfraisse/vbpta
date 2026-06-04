using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Identity;

namespace SiteEngine.Services;

public class SiteUserService(IDbContextFactory<AppDbContext> dbContextFactory) : ISiteUserService
{
	public async Task<IEnumerable<SiteRole>> GetUserRolesAtSiteAsync(string userId, Guid siteId)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		return await dbContext.SiteUserRoles
			.Where(x => x.UserId == userId && x.SiteId == siteId)
			.Select(x => x.Role)
			.ToListAsync();
	}

	public async Task<bool> UserHasRoleAsync(string userId, Guid siteId, SiteRole role)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		return await dbContext.SiteUserRoles
			.AnyAsync(x => x.UserId == userId && x.SiteId == siteId && x.Role == role);
	}

	public async Task<SiteUserRole> AssignRoleAsync(string userId, Guid siteId, SiteRole role)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		var existing = await dbContext.SiteUserRoles
			.FirstOrDefaultAsync(x => x.UserId == userId && x.SiteId == siteId && x.Role == role);

		if (existing != null)
			return existing;

		var assignment = new SiteUserRole
		{
			UserId = userId,
			SiteId = siteId,
			Role = role,
			CreatedAt = DateTime.UtcNow
		};

		dbContext.SiteUserRoles.Add(assignment);
		await dbContext.SaveChangesAsync();
		return assignment;
	}

	public async Task<bool> RemoveRoleAsync(string userId, Guid siteId, SiteRole role)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		var assignment = await dbContext.SiteUserRoles
			.FirstOrDefaultAsync(x => x.UserId == userId && x.SiteId == siteId && x.Role == role);

		if (assignment == null)
			return false;

		dbContext.SiteUserRoles.Remove(assignment);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public async Task<IEnumerable<SiteUser>> GetUsersWithRoleAsync(Guid siteId, SiteRole role)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		return await dbContext.SiteUserRoles
			.Where(x => x.SiteId == siteId && x.Role == role)
			.Select(x => x.User)
			.ToListAsync();
	}

	public async Task<string?> GetSiteAdminEmailAsync(Guid siteId)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		return await dbContext.SiteUserRoles
			.Where(x => x.SiteId == siteId && x.Role == SiteRole.Admin)
			.Select(x => x.User.Email)
			.FirstOrDefaultAsync();
	}
}

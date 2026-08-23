using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Identity;

namespace WebApp.Services;

// Ban and delete are deliberately separate lifecycles — see BannedEmail.
// Delete removes the account (Identity + SiteUser + roles + login history,
// all cascading via FK — see AppDbContext's OnDelete(DeleteBehavior.Cascade)
// on each). If that email signs in again later, RequestCodeAsync just
// creates a fresh, unprivileged account for it, same as any first-time
// visitor — that's expected, not a bug, for ordinary account cleanup. Ban
// blocks that re-creation entirely, independent of whether an account
// currently exists.
public class UserModerationService(IDbContextFactory<AppDbContext> dbFactory, UserManager<ApplicationUser> userManager)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
	private readonly UserManager<ApplicationUser> _userManager = userManager;

	public async Task<bool> IsBannedAsync(string email, CancellationToken cancellationToken = default)
	{
		var normalizedEmail = Normalize(email);

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.BannedEmails.AnyAsync(b => b.Email == normalizedEmail, cancellationToken);
	}

	public async Task<List<BannedEmail>> GetBannedEmailsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.BannedEmails
			.OrderByDescending(b => b.BannedAtUtc)
			.ToListAsync(cancellationToken);
	}

	public async Task BanEmailAsync(string email, string? bannedByUserId, string? reason = null, CancellationToken cancellationToken = default)
	{
		var normalizedEmail = Normalize(email);

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (await db.BannedEmails.AnyAsync(b => b.Email == normalizedEmail, cancellationToken))
			return;

		db.BannedEmails.Add(new BannedEmail
		{
			Email = normalizedEmail,
			BannedAtUtc = DateTimeOffset.UtcNow,
			BannedByUserId = bannedByUserId,
			Reason = reason
		});

		await db.SaveChangesAsync(cancellationToken);
	}

	public async Task UnbanEmailAsync(string email, CancellationToken cancellationToken = default)
	{
		var normalizedEmail = Normalize(email);

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var entry = await db.BannedEmails.FirstOrDefaultAsync(b => b.Email == normalizedEmail, cancellationToken);
		if (entry is null)
			return;

		db.BannedEmails.Remove(entry);
		await db.SaveChangesAsync(cancellationToken);
	}

	// Returns false if the user was already gone (nothing to delete) or
	// Identity's delete failed for some other reason — either way the
	// caller has nothing more to do.
	public async Task<bool> DeleteUserAsync(string identityUserId)
	{
		var user = await _userManager.FindByIdAsync(identityUserId);
		if (user is null)
			return false;

		var result = await _userManager.DeleteAsync(user);
		return result.Succeeded;
	}

	private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}

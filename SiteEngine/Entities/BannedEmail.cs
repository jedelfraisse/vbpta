namespace SiteEngine.Entities;

// Deliberately independent of ApplicationUser/SiteUser — a ban is keyed by
// email, not by an account row. RequestCodeAsync auto-creates an account for
// any email that doesn't have one yet, so a ban tied to the account would be
// erased the moment that account was deleted (or would never apply at all to
// an email that hasn't signed up yet). Keeping it a standalone list lets it
// outlive — and precede — the account it's blocking.
public class BannedEmail
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// Always normalized (trimmed, lowercased) — see UserModerationService.
	public string Email { get; set; } = string.Empty;

	public DateTimeOffset BannedAtUtc { get; set; }

	// FK to AspNetUsers (ApplicationUser) for the admin who issued the ban.
	// Nullable: not enforced, just an attribution trail.
	public string? BannedByUserId { get; set; }

	public string? Reason { get; set; }
}

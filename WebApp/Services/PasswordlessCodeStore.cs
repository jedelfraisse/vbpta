namespace WebApp.Services;

// Explicit, single-use, time-limited sign-in codes for the passwordless login
// flow. Deliberately not using Identity's built-in email token provider here:
// that provider returns the SAME code for repeated requests within its own
// internal time window (by design, for its own use case) and has no fixed,
// reportable expiry — neither fits "clicking send again gives a fresh code
// that's good for exactly 20 minutes, and the email says so."
public sealed class PasswordlessCodeStore
{
	public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(20);

	private readonly object _gate = new();
	private readonly Dictionary<string, (string Code, DateTimeOffset ExpiresAtUtc)> _codes =
		new(StringComparer.OrdinalIgnoreCase);

	// Always overwrites any pending code for this email, so requesting again
	// immediately invalidates whatever was sent before.
	public string GenerateCode(string email)
	{
		var code = Random.Shared.Next(100000, 999999).ToString();

		lock (_gate)
		{
			_codes[email] = (code, DateTimeOffset.UtcNow.Add(Lifetime));
		}

		return code;
	}

	// Does not consume on failure, so a mistyped code can still be retried
	// until it actually expires. Call Consume() explicitly once sign-in succeeds.
	public bool Validate(string email, string code)
	{
		lock (_gate)
		{
			if (!_codes.TryGetValue(email, out var entry))
				return false;

			if (entry.ExpiresAtUtc < DateTimeOffset.UtcNow)
			{
				_codes.Remove(email);
				return false;
			}

			return entry.Code == code;
		}
	}

	public void Consume(string email)
	{
		lock (_gate)
		{
			_codes.Remove(email);
		}
	}
}

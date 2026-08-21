namespace WebApp.Services;

// Small in-memory record of system-level facts that describe *this running
// process*, not portal content — so they don't belong in PortalConfig/the
// database. Populated once by Program.cs at startup, and again whenever an
// admin sends a test email from Global Admin > System & Developer > System
// Settings > Email Server. Resets on every restart; that's intentional; both
// facts are inherently scoped to "since this process last started".
public sealed class SystemDiagnosticsState
{
	public DateTimeOffset? LastStartupValidationUtc { get; private set; }
	public DateTimeOffset? LastEmailTestUtc { get; private set; }
	public bool? LastEmailTestSucceeded { get; private set; }

	public void RecordStartupValidation() => LastStartupValidationUtc = DateTimeOffset.UtcNow;

	public void RecordEmailTest(bool succeeded)
	{
		LastEmailTestUtc = DateTimeOffset.UtcNow;
		LastEmailTestSucceeded = succeeded;
	}
}

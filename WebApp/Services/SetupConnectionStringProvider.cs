namespace WebApp.Services;

// Single in-memory source of truth for "what's the SQL connection string right now".
// Loaded once from appsettings.json at process start, then updated synchronously the
// instant SetupService.RunMigrationsAsync proves a new one actually works. Every
// AppDbContext (factory-created or DI-scoped) is configured by reading this value —
// never by re-parsing appsettings.json or waiting on IConfiguration's file-watcher reload.
public sealed class SetupConnectionStringProvider
{
	private readonly object _gate = new();
	private string? _connectionString;

	public SetupConnectionStringProvider(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString("DefaultConnection");
	}

	public string? Current
	{
		get { lock (_gate) return _connectionString; }
	}

	public bool IsConfigured => !string.IsNullOrWhiteSpace(Current);

	public void Set(string connectionString)
	{
		lock (_gate)
		{
			_connectionString = connectionString;
		}
	}

	// Clears the in-memory connection string so IsConfigured goes back to false.
	// Used at startup when the configured connection string turns out to point at a
	// database that no longer exists or has no schema (deleted DB, renamed DB, etc.) —
	// without this, the provider would keep reporting "configured" against a target
	// that was proven unusable, and the app would never fall back into setup mode.
	public void Reset()
	{
		lock (_gate)
		{
			_connectionString = null;
		}
	}
}

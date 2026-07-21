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
}

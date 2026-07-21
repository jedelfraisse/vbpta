using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;

namespace WebApp.Services;

// Do NOT replace this with builder.Services.AddDbContextFactory<AppDbContext>(...).
// That extension registers DbContextOptions<AppDbContext> as a SINGLETON, built exactly
// once by invoking the options delegate at first resolution. During setup, the first
// resolution happens on the very first page load (SetupStateService gets injected in
// Routes.razor) — before any connection string exists. Every CreateDbContext() call
// afterward would then reuse those same frozen, unconfigured options for the lifetime
// of the whole process, no matter how many times SetupConnectionStringProvider.Set()
// runs later. This factory instead builds a brand-new DbContextOptions from whatever
// the connection string currently is, on every single call — no caching, ever.
public sealed class LiveConnectionDbContextFactory : IDbContextFactory<AppDbContext>
{
	private readonly SetupConnectionStringProvider _connectionProvider;

	public LiveConnectionDbContextFactory(SetupConnectionStringProvider connectionProvider)
	{
		_connectionProvider = connectionProvider;
	}

	public AppDbContext CreateDbContext()
	{
		if (!_connectionProvider.IsConfigured)
			throw new InvalidOperationException(
				"Cannot create AppDbContext: no connection string has been configured yet.");

		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlServer(_connectionProvider.Current)
			.Options;

		return new AppDbContext(options);
	}

	public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
		=> Task.FromResult(CreateDbContext());
}

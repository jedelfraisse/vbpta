using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SiteEngine.Data;

namespace VbptaWeb.Tests.Support;

internal sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
	private readonly DbContextOptions<AppDbContext> _options;

	public TestDbContextFactory(DbContextOptions<AppDbContext> options)
	{
		_options = options;
	}

	public AppDbContext CreateDbContext()
	{
		return new AppDbContext(_options);
	}

	public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new AppDbContext(_options));
	}
}

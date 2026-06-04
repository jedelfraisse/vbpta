using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SiteEngine.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
		optionsBuilder.UseSqlServer("Server=localhost;Database=vbpta;User Id=vbpta;Password=vbpta;TrustServerCertificate=True;MultipleActiveResultSets=True");
		return new AppDbContext(optionsBuilder.Options);
	}
}

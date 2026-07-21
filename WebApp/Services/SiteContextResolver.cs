namespace WebApp.Services;

public class SiteContextResolver
{
	private readonly SetupStateService _setup;
	private readonly IServiceProvider _services;

	public SiteContextResolver(SetupStateService setup, IServiceProvider services)
	{
		_setup = setup;
		_services = services;
	}

	public object GetContext()
	{
		var status = _setup.GetStatus();

		if (!status.IsFullyConfigured)
			return _services.GetRequiredService<SetupSiteContext>();

		return _services.GetRequiredService<RuntimeSiteContext>();
	}
}

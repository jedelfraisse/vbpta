using SiteEngine.Entities;

namespace WebApp.Services;

public class SetupSiteContext
{
	public bool IsSetupMode => true;
	public Site? CurrentSite => null;
}

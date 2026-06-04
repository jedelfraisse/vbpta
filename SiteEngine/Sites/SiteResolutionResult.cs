using SiteEngine.Config;
using SiteEngine.Entities;

namespace SiteEngine.Sites;

public sealed record SiteResolutionResult(Site Site, SiteConfig SiteConfig, bool IsAdminContext);

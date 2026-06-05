using SiteEngine.Entities;

namespace SiteEngine.Services;

public interface ISiteContentService
{
    Task<SitePage> GetPageAsync(string pageId);
}

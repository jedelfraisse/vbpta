using SiteEngine.Entities;

namespace SiteEngine.Services;

public class SiteContentService : ISiteContentService
{
    public async Task<SitePage> GetPageAsync(string pageId)
    {
        // FUTURE: Load from DB using EF Core
        // For now, return a static example page
        return new SitePage
        {
            PageId = pageId,
            Text = """
                {smallbox Color="black" title="Welcome!"}
                
                We have cookies.
                {/smallbox}

                {smallbox events}

                {fullbox boardlist}
                """
        };
    }
}

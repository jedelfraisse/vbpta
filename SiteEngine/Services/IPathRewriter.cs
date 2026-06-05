namespace SiteEngine.Services;

public interface IPathRewriter
{
    string Rewrite(string html, int siteId);
}

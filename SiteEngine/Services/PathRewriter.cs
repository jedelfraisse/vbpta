using System.Text.RegularExpressions;

namespace SiteEngine.Services;

public class PathRewriter : IPathRewriter
{
    public string Rewrite(string html, int siteId)
    {
        return Regex.Replace(
            html,
            "(src|href)=[\"']\\/(?!site-data\\/)",
            $"$1=\"/site-data/{siteId}/"
        );
    }
}

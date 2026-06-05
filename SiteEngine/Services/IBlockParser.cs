using SiteEngine.Entities;

namespace SiteEngine.Services;

public interface IBlockParser
{
    List<BlockResult> Parse(string text);
}

namespace SiteEngine.Sites;

public interface ISiteResolver
{
	Task<SiteResolutionResult?> ResolveAsync(string host, CancellationToken cancellationToken = default);
	void InvalidateHost(string host);
}

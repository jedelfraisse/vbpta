using Markdig;

namespace SiteEngine.Services;

public interface IMarkdownPipelineFactory
{
    MarkdownPipeline CreatePipeline();
}

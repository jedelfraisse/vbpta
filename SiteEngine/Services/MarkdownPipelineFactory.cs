using Markdig;

namespace SiteEngine.Services;

public class MarkdownPipelineFactory : IMarkdownPipelineFactory
{
    public MarkdownPipeline CreatePipeline()
    {
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }
}

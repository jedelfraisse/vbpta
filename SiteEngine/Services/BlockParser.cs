using System.Text.RegularExpressions;
using Markdig;
using SiteEngine.Entities;

namespace SiteEngine.Services;

public class BlockParser : IBlockParser
{
    private static readonly Regex BlockStart = new(
        @"\{(?<size>smallbox|fullbox)(\s+(?<helper>\w+))?(?<params>[^}]*)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BlockEnd = new(
        @"\{\/(?<size>smallbox|fullbox)\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ParamRegex = new(
        @"(?<key>\w+)=""(?<value>[^""]+)""",
        RegexOptions.Compiled);

    public List<BlockResult> Parse(string text)
    {
        var results = new List<BlockResult>();
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        int index = 0;

        while (index < text.Length)
        {
            var startMatch = BlockStart.Match(text, index);
            if (!startMatch.Success)
                break;

            var size = startMatch.Groups["size"].Value.ToLower();
            var helper = startMatch.Groups["helper"].Success
                ? startMatch.Groups["helper"].Value.ToLower()
                : null;

            var paramText = startMatch.Groups["params"].Value;
            var parameters = ExtractParameters(paramText);

            // Title is just a parameter
            parameters.TryGetValue("title", out var title);

            var blockSize = size == "smallbox" ? BlockSize.Small : BlockSize.Full;

            int contentStart = startMatch.Index + startMatch.Length;

            // Helper blocks have no closing tag
            var endMatch = BlockEnd.Match(text, contentStart);
            bool isMarkdownBlock = endMatch.Success;

            if (!isMarkdownBlock)
            {
                results.Add(new BlockResult
                {
                    Type = BlockType.Helper,
                    Size = blockSize,
                    HelperName = helper,
                    Title = title,
                    Parameters = parameters
                });

                index = startMatch.Index + startMatch.Length;
                continue;
            }

            // Markdown block
            string markdown = text.Substring(contentStart, endMatch.Index - contentStart);
            string html = Markdown.ToHtml(markdown, pipeline);

            results.Add(new BlockResult
            {
                Type = BlockType.Markdown,
                Size = blockSize,
                Html = html,
                Title = title,
                Parameters = parameters
            });

            index = endMatch.Index + endMatch.Length;
        }

        return results;
    }

    private Dictionary<string, string> ExtractParameters(string paramText)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in ParamRegex.Matches(paramText))
        {
            dict[m.Groups["key"].Value] = m.Groups["value"].Value;
        }

        return dict;
    }
}

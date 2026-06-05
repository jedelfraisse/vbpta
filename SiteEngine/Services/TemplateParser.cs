using System.Text.RegularExpressions;

namespace SiteEngine.Services;

public class TemplateParser : ITemplateParser
{
    public string Parse(string html)
    {
        var pattern = @"\{\{card\s+(?<type>\w+)(?<attrs>[^}]*)\}\}";

        return Regex.Replace(html, pattern, match =>
        {
            var type = match.Groups["type"].Value.ToLower();
            var attrs = ParseAttributes(match.Groups["attrs"].Value);

            return RenderCard(type, attrs);
        });
    }

    private Dictionary<string, string> ParseAttributes(string raw)
    {
        var dict = new Dictionary<string, string>();
        var attrPattern = @"(\w+)=""([^""]+)""";

        foreach (Match m in Regex.Matches(raw, attrPattern))
            dict[m.Groups[1].Value] = m.Groups[2].Value;

        return dict;
    }

    private string RenderCard(string type, Dictionary<string, string> attrs)
    {
        var bg = attrs.TryGetValue("bgcolor", out var c) ? c : "white";

        return type switch
        {
            "events" => $"<div class=\"city-card-half\" style=\"background:{bg};\">{{EVENTS}}</div>",
            "board"  => $"<div class=\"city-card\" style=\"background:{bg};\">{{BOARD}}</div>",
            _        => $"<!-- Unknown card type: {type} -->"
        };
    }
}

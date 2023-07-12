using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorBrain.Client.Shared;

public static partial class GeometryHelper
{
    [GeneratedRegex(@"\`{3}(json|application\/geo\+json)?\n((.|\n)*)\n\`{3}", RegexOptions.Multiline)]
    private static partial Regex GeometryExpression();
    
    public static (IReadOnlyList<JsonDocument>, string replacementContent) GetGeometry(string content)
    {
        var contentBuffer = new StringBuilder(content);
        var results = new List<JsonDocument>();
        var matches = GeometryExpression().Matches(content);
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                if (match.Groups[1].Value == "application/geo+json")
                {
                    contentBuffer.Replace(match.Value, "\nGenerating a map for you...\n");
                }
                
                JsonDocument? json;
                try
                {
                    json = JsonDocument.Parse(match.Groups[2].Value);
                }
                catch
                {
                    continue;
                }
                if (json.RootElement.TryGetProperty("type", out var type))
                {
                    switch (type.GetString())
                    {
                        case "FeatureCollection":
                            contentBuffer.Replace(match.Value, "\n> Generated a map for you.\n");
                            results.Add(json);
                        break;
                    }
                }
            }
        }
        return (results, contentBuffer.ToString());
    }
    
}
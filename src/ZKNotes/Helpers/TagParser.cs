using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZKNotes.Helpers;

/// <summary>
/// Extracts inline #hashtags from note content.
/// </summary>
public static partial class TagParser
{
    // Matches #tag (word characters, hyphens, underscores, at least 1 char)
    // Excludes things like #123 (pure numbers) and Markdown headings (# followed by space)
    [GeneratedRegex(@"(?:^|(?<=\s))#([a-zA-Z_][\w-]*)", RegexOptions.Compiled)]
    private static partial Regex TagPattern();

    /// <summary>
    /// Extracts all unique hashtags from content (lowercased, without the # prefix).
    /// </summary>
    public static List<string> ExtractTags(string content)
    {
        if (string.IsNullOrEmpty(content))
            return [];

        return TagPattern()
            .Matches(content)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}

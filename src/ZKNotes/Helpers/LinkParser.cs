using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ZKNotes.Helpers;

/// <summary>
/// Extracts [[wiki-style links]] from note content.
/// </summary>
public static partial class LinkParser
{
    // Matches [[anything inside double brackets]]
    [GeneratedRegex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex LinkPattern();

    /// <summary>
    /// Extracts all linked note references from content.
    /// Returns the inner text of each [[link]].
    /// </summary>
    public static List<string> ExtractLinks(string content)
    {
        if (string.IsNullOrEmpty(content))
            return [];

        var links = new List<string>();
        foreach (Match match in LinkPattern().Matches(content))
        {
            var linkText = match.Groups[1].Value.Trim();
            if (linkText.Length > 0)
                links.Add(linkText);
        }

        return links;
    }

    /// <summary>
    /// Replaces [[link]] syntax with a clickable representation for display.
    /// </summary>
    public static string ReplaceLinksForHtml(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        return LinkPattern().Replace(content, m =>
        {
            var linkText = m.Groups[1].Value.Trim();
            return $"<a href=\"zk://note/{System.Net.WebUtility.UrlEncode(linkText)}\" class=\"note-link\">{System.Net.WebUtility.HtmlEncode(linkText)}</a>";
        });
    }
}

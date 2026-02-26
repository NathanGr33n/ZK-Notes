using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ZKNotes.Helpers;

/// <summary>
/// Rewrites inline #tags in markdown text.
/// Uses the same tag token rules as TagParser (start/whitespace + # + [a-zA-Z_][\w-]*).
/// </summary>
public static partial class TagRewriter
{
    [GeneratedRegex(@"(?:^|(?<=\s))#(?<tag>[a-zA-Z_][\w-]*)", RegexOptions.Compiled)]
    private static partial Regex TagTokenPattern();

    [GeneratedRegex(@"^[a-zA-Z_][\w-]*$", RegexOptions.Compiled)]
    private static partial Regex ValidTagPattern();

    public static bool IsValidTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        var t = Normalize(tag);
        return ValidTagPattern().IsMatch(t);
    }

    public static string Normalize(string tag)
    {
        var t = tag.Trim();
        if (t.StartsWith('#'))
            t = t.TrimStart('#');

        return t.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Renames all occurrences of #fromTag to #toTag.
    /// </summary>
    public static string RenameTag(string content, string fromTag, string toTag)
    {
        content ??= string.Empty;

        var from = Normalize(fromTag);
        var to = Normalize(toTag);

        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            return content;

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return content;

        var matches = TagTokenPattern().Matches(content);
        if (matches.Count == 0)
            return content;

        var sb = new StringBuilder(content.Length);
        var lastIndex = 0;

        foreach (Match m in matches)
        {
            if (!m.Success)
                continue;

            var tag = m.Groups["tag"].Value;
            if (!string.Equals(tag, from, StringComparison.OrdinalIgnoreCase))
                continue;

            // Replace only the tag text, not the leading whitespace.
            var hashIndex = m.Index;
            while (hashIndex < content.Length && content[hashIndex] != '#')
                hashIndex++;

            if (hashIndex >= content.Length)
                continue;

            sb.Append(content, lastIndex, hashIndex - lastIndex);
            sb.Append('#');
            sb.Append(to);

            lastIndex = hashIndex + 1 + tag.Length;
        }

        sb.Append(content, lastIndex, content.Length - lastIndex);
        return sb.ToString();
    }
}

using Markdig;

namespace ZKNotes.Helpers;

/// <summary>
/// Converts Markdown content to styled HTML for the preview pane.
/// </summary>
public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private const string HtmlTemplate = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8"/>
        <style>
            /* NOTE: this template is used with string.Format, so literal braces must be escaped as {{ and }} */
            :root {{
                --bg: transparent;
                --surface: #171A22;
                --surface2: #1E2230;
                --surface3: #24293A;
                --border: #2B3240;
                --text: #EDEFF5;
                --muted: #A9B0BE;
                --accent: #9B59B6;
                --accentHover: #7D3C98;
            }}
            body {{
                font-family: 'Inter', 'Segoe UI Variable Text', 'Segoe UI', sans-serif;
                font-size: 14px;
                line-height: 1.65;
                color: var(--text);
                background: var(--bg);
                padding: 16px;
                margin: 0;
            }}
            h1, h2, h3, h4 {{
                color: var(--text);
                margin-top: 1.15em;
            }}
            p, li {{ color: var(--text); }}
            a.note-link {{ color: var(--accent); text-decoration: none; font-weight: 600; }}
            a.note-link:hover {{ text-decoration: underline; color: var(--accentHover); }}
            a {{ color: var(--accent); }}
            a:hover {{ color: var(--accentHover); }}
            code {{
                background: var(--surface3);
                border: 1px solid var(--border);
                padding: 2px 6px;
                border-radius: 4px;
                font-size: 13px;
            }}
            pre {{
                background: var(--surface2);
                border: 1px solid var(--border);
                padding: 12px;
                border-radius: 8px;
                overflow-x: auto;
            }}
            pre code {{ background: none; border: none; padding: 0; }}
            blockquote {{
                border-left: 3px solid var(--border);
                margin-left: 0;
                padding-left: 12px;
                color: var(--muted);
            }}
            hr {{ border: none; border-top: 1px solid var(--border); }}
            img {{ max-width: 100%; }}
        </style>
        </head>
        <body>{0}</body>
        </html>
        """;

    /// <summary>
    /// Renders Markdown content to a full HTML page with dark theme styling.
    /// Processes [[links]] before Markdown conversion.
    /// </summary>
    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Format(HtmlTemplate, "<p style='color:var(--muted)'>No content</p>");

        // Replace [[links]] with HTML anchors before Markdown processing
        var processed = LinkParser.ReplaceLinksForHtml(markdown);
        var bodyHtml = Markdown.ToHtml(processed, Pipeline);

        return string.Format(HtmlTemplate, bodyHtml);
    }
}

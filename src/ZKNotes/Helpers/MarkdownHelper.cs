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
            body {{
                font-family: 'Segoe UI', sans-serif;
                font-size: 14px;
                line-height: 1.6;
                color: #e0e0e0;
                background: #1e1e1e;
                padding: 16px;
                margin: 0;
            }}
            h1, h2, h3, h4 {{ color: #dcdcdc; margin-top: 1em; }}
            a.note-link {{ color: #6cb6ff; text-decoration: none; font-weight: 600; }}
            a.note-link:hover {{ text-decoration: underline; }}
            a {{ color: #6cb6ff; }}
            code {{
                background: #2d2d2d;
                padding: 2px 6px;
                border-radius: 3px;
                font-size: 13px;
            }}
            pre {{
                background: #2d2d2d;
                padding: 12px;
                border-radius: 6px;
                overflow-x: auto;
            }}
            pre code {{ background: none; padding: 0; }}
            blockquote {{
                border-left: 3px solid #444;
                margin-left: 0;
                padding-left: 12px;
                color: #999;
            }}
            hr {{ border: none; border-top: 1px solid #333; }}
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
            return string.Format(HtmlTemplate, "<p style='color:#666'>No content</p>");

        // Replace [[links]] with HTML anchors before Markdown processing
        var processed = LinkParser.ReplaceLinksForHtml(markdown);
        var bodyHtml = Markdown.ToHtml(processed, Pipeline);

        return string.Format(HtmlTemplate, bodyHtml);
    }
}

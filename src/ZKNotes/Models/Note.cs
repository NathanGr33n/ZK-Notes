using System;
using System.Collections.Generic;

namespace ZKNotes.Models;

/// <summary>
/// Represents a single Zettelkasten note with metadata and content.
/// </summary>
public sealed class Note
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public NoteType Type { get; set; } = NoteType.Standard;
    public string? Template { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<string> Links { get; set; } = [];
    public List<string> Attachments { get; set; } = [];
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime LastEdit { get; set; } = DateTime.Now;
    public DateTime? LastReviewed { get; set; }

    /// <summary>
    /// Returns a short snippet of the content for previews.
    /// </summary>
    public string Snippet => Content.Length > 120
        ? Content[..120].TrimEnd() + "…"
        : Content;
}

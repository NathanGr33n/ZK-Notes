using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ZKNotes.Models;

/// <summary>
/// YAML frontmatter metadata serialized at the top of each .md file.
/// </summary>
public sealed class NoteMetadata
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    [YamlMember(Alias = "created")]
    public DateTime Created { get; set; }

    [YamlMember(Alias = "last_edit")]
    public DateTime LastEdit { get; set; }

    [YamlMember(Alias = "last_reviewed")]
    public DateTime? LastReviewed { get; set; }

    [YamlMember(Alias = "links")]
    public List<string> Links { get; set; } = [];

    [YamlMember(Alias = "attachments")]
    public List<string> Attachments { get; set; } = [];
}

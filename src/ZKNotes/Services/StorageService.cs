using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ZKNotes.Models;

namespace ZKNotes.Services;

/// <summary>
/// Handles reading, writing, and deleting note .md files with YAML frontmatter.
/// Thread-safe via a SemaphoreSlim for concurrent access.
/// </summary>
public sealed class StorageService
{
    private const string FrontmatterDelimiter = "---";
    private const string IdPrefix = "ZK-";
    private const string CounterFileName = ".zk-counter";

    private readonly string _notesDir;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly ISerializer _yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public StorageService(string notesDirectory)
    {
        _notesDir = notesDirectory;
        Directory.CreateDirectory(_notesDir);
    }

    public string NotesDirectory => _notesDir;

    /// <summary>
    /// Generates the next unique note ID (ZK-0001, ZK-0002, ...).
    /// Uses a counter file for persistence.
    /// </summary>
    public async Task<string> GenerateNextIdAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var counterPath = Path.Combine(_notesDir, CounterFileName);
            int counter = 0;

            if (File.Exists(counterPath))
            {
                var text = await File.ReadAllTextAsync(counterPath).ConfigureAwait(false);
                int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out counter);
            }

            counter++;
            await File.WriteAllTextAsync(counterPath, counter.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);

            return $"{IdPrefix}{counter:D4}";
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Saves a note to disk as a .md file with YAML frontmatter.
    /// </summary>
    public async Task SaveNoteAsync(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (string.IsNullOrWhiteSpace(note.Id))
            throw new ArgumentException("Note must have an ID.", nameof(note));

        note.LastEdit = DateTime.Now;

        var metadata = new NoteMetadata
        {
            Id = note.Id,
            Title = note.Title,
            Type = note.Type.ToString().ToLowerInvariant(),
            Template = note.Template,
            Tags = note.Tags,
            Created = note.Created,
            LastEdit = note.LastEdit,
            LastReviewed = note.LastReviewed,
            Links = note.Links,
            Attachments = note.Attachments
        };

        var sb = new StringBuilder();
        sb.AppendLine(FrontmatterDelimiter);
        sb.Append(_yamlSerializer.Serialize(metadata).TrimEnd());
        sb.AppendLine();
        sb.AppendLine(FrontmatterDelimiter);
        sb.Append(note.Content);

        var filePath = GetFilePath(note.Id);

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Loads a single note by ID.
    /// </summary>
    public async Task<Note?> LoadNoteAsync(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            return null;

        var text = await File.ReadAllTextAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
        return ParseNoteFile(text, id);
    }

    /// <summary>
    /// Loads all notes from the notes directory.
    /// </summary>
    public async Task<List<Note>> LoadAllNotesAsync()
    {
        var notes = new List<Note>();
        if (!Directory.Exists(_notesDir))
            return notes;

        var files = Directory.GetFiles(_notesDir, "*.md", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var text = await File.ReadAllTextAsync(file, Encoding.UTF8).ConfigureAwait(false);
                var id = Path.GetFileNameWithoutExtension(file);
                var note = ParseNoteFile(text, id);
                if (note is not null)
                    notes.Add(note);
            }
            catch
            {
                // Skip malformed files
            }
        }

        return notes;
    }

    /// <summary>
    /// Deletes a note file from disk.
    /// </summary>
    public Task DeleteNoteAsync(string id)
    {
        var filePath = GetFilePath(id);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a note file exists.
    /// </summary>
    public bool NoteExists(string id) => File.Exists(GetFilePath(id));

    private string GetFilePath(string id)
    {
        // Sanitize ID for use as filename
        var safe = string.Concat(id.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_notesDir, safe + ".md");
    }

    private Note? ParseNoteFile(string fileContent, string fallbackId)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
            return null;

        var lines = fileContent.Split('\n');
        int frontmatterStart = -1;
        int frontmatterEnd = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == FrontmatterDelimiter)
            {
                if (frontmatterStart < 0)
                    frontmatterStart = i;
                else
                {
                    frontmatterEnd = i;
                    break;
                }
            }
        }

        NoteMetadata? metadata = null;
        string content;

        if (frontmatterStart >= 0 && frontmatterEnd > frontmatterStart)
        {
            var yamlLines = lines[(frontmatterStart + 1)..frontmatterEnd];
            var yaml = string.Join('\n', yamlLines);

            try
            {
                metadata = _yamlDeserializer.Deserialize<NoteMetadata>(yaml);
            }
            catch
            {
                // Fall through with null metadata
            }

            content = string.Join('\n', lines[(frontmatterEnd + 1)..]).TrimStart('\r', '\n');
        }
        else
        {
            content = fileContent;
        }

        metadata ??= new NoteMetadata { Id = fallbackId };

        return new Note
        {
            Id = string.IsNullOrEmpty(metadata.Id) ? fallbackId : metadata.Id,
            Title = metadata.Title,
            Content = content,
            Type = ParseNoteType(metadata.Type),
            Template = metadata.Template,
            Tags = metadata.Tags ?? [],
            Links = metadata.Links ?? [],
            Attachments = metadata.Attachments ?? [],
            Created = metadata.Created == default ? DateTime.Now : metadata.Created,
            LastEdit = metadata.LastEdit == default ? DateTime.Now : metadata.LastEdit,
            LastReviewed = metadata.LastReviewed
        };
    }

    private static NoteType ParseNoteType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return NoteType.Standard;

        return type.Trim().ToLowerInvariant() switch
        {
            "fleeting" => NoteType.Fleeting,
            "journal" => NoteType.Journal,
            _ => NoteType.Standard
        };
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ZKNotes.Models;

namespace ZKNotes.Services;

public sealed class TemplateDefinition
{
    public required string Name { get; init; }
    public NoteType DefaultType { get; init; } = NoteType.Standard;
    public string? TitleTemplate { get; init; }
    public required string BodyTemplate { get; init; }
}

/// <summary>
/// Loads markdown note templates from a templates folder (defaults to &lt;NotesDirectory&gt;\_templates).
/// Templates are plain .md files. File name (without extension) is the template name.
///
/// Templates may optionally include YAML frontmatter:
/// ---
/// type: journal|fleeting|standard
/// title: "Journal {{date}}"
/// ---
/// (body markdown)
/// </summary>
public sealed class TemplateService : IDisposable
{
    private const string FrontmatterDelimiter = "---";

    private readonly string _templatesDir;
    private readonly FileSystemWatcher _watcher;
    private int _changeRaised;

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public TemplateService(string notesDirectory, string templatesSubfolder)
    {
        if (string.IsNullOrWhiteSpace(notesDirectory))
            throw new ArgumentException("Notes directory is required", nameof(notesDirectory));

        templatesSubfolder = string.IsNullOrWhiteSpace(templatesSubfolder) ? "_templates" : templatesSubfolder.Trim();
        _templatesDir = Path.Combine(notesDirectory, templatesSubfolder);
        Directory.CreateDirectory(_templatesDir);

        _watcher = new FileSystemWatcher(_templatesDir, "*.md")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Created += (_, _) => RaiseTemplatesChanged();
        _watcher.Changed += (_, _) => RaiseTemplatesChanged();
        _watcher.Deleted += (_, _) => RaiseTemplatesChanged();
        _watcher.Renamed += (_, _) => RaiseTemplatesChanged();
        _watcher.EnableRaisingEvents = true;
    }

    public string TemplatesDirectory => _templatesDir;

    public event EventHandler? TemplatesChanged;

    public IReadOnlyList<string> ListTemplateNames()
    {
        if (!Directory.Exists(_templatesDir))
            return [];

        return Directory.GetFiles(_templatesDir, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n)
            .ToList();
    }

    public bool TemplateExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var path = GetTemplatePath(name);
        return File.Exists(path);
    }

    public TemplateDefinition LoadTemplateDefinition(string name)
    {
        var raw = LoadTemplate(name);
        var (metadata, body) = SplitFrontmatter(raw);

        var defaultType = ParseNoteType(metadata?.Type);

        return new TemplateDefinition
        {
            Name = name,
            DefaultType = defaultType,
            TitleTemplate = string.IsNullOrWhiteSpace(metadata?.Title) ? null : metadata!.Title,
            BodyTemplate = body
        };
    }

    public string LoadTemplate(string name)
    {
        var path = GetTemplatePath(name);
        if (!File.Exists(path))
            return string.Empty;

        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    public string ApplyVariables(string template, IReadOnlyDictionary<string, string> variables)
    {
        template ??= string.Empty;
        if (variables is null || variables.Count == 0)
            return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace("{{" + key + "}}", value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public static IReadOnlyDictionary<string, string> BaseVariables(string id, DateTime now)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = id,
            ["date"] = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["time"] = now.ToString("HH:mm", CultureInfo.InvariantCulture),
            ["datetime"] = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            ["year"] = now.ToString("yyyy", CultureInfo.InvariantCulture),
            ["month"] = now.ToString("MM", CultureInfo.InvariantCulture),
            ["day"] = now.ToString("dd", CultureInfo.InvariantCulture)
        };
    }

    public static IReadOnlyDictionary<string, string> DefaultVariables(string id, string title, DateTime now)
    {
        var vars = new Dictionary<string, string>(BaseVariables(id, now), StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = title
        };

        return vars;
    }

    private string GetTemplatePath(string name)
    {
        // Basic sanitation (avoid path traversal)
        var safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        safe = safe.Replace("..", string.Empty, StringComparison.Ordinal);
        return Path.Combine(_templatesDir, safe + ".md");
    }

    private void RaiseTemplatesChanged()
    {
        // FileSystemWatcher tends to fire multiple times per save. Collapse bursts.
        if (Interlocked.Exchange(ref _changeRaised, 1) == 1)
            return;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(200);
            Interlocked.Exchange(ref _changeRaised, 0);
            TemplatesChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private (TemplateMetadata? metadata, string body) SplitFrontmatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (null, string.Empty);

        var lines = content.Split('\n');
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

        if (frontmatterStart == 0 && frontmatterEnd > frontmatterStart)
        {
            var yamlLines = lines[(frontmatterStart + 1)..frontmatterEnd];
            var yaml = string.Join('\n', yamlLines);

            TemplateMetadata? meta = null;
            try
            {
                meta = _yamlDeserializer.Deserialize<TemplateMetadata>(yaml);
            }
            catch
            {
                meta = null;
            }

            var body = string.Join('\n', lines[(frontmatterEnd + 1)..]).TrimStart('\r', '\n');
            return (meta, body);
        }

        return (null, content);
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

    public void Dispose()
    {
        _watcher.Dispose();
    }

    private sealed class TemplateMetadata
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
    }
}

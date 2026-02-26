using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ZKNotes.Services;

/// <summary>
/// Loads markdown note templates from a templates folder (defaults to &lt;NotesDirectory&gt;\_templates).
/// Templates are plain .md files. File name (without extension) is the template name.
/// </summary>
public sealed class TemplateService
{
    private readonly string _templatesDir;

    public TemplateService(string notesDirectory, string templatesSubfolder)
    {
        if (string.IsNullOrWhiteSpace(notesDirectory))
            throw new ArgumentException("Notes directory is required", nameof(notesDirectory));

        templatesSubfolder = string.IsNullOrWhiteSpace(templatesSubfolder) ? "_templates" : templatesSubfolder.Trim();
        _templatesDir = Path.Combine(notesDirectory, templatesSubfolder);
        Directory.CreateDirectory(_templatesDir);
    }

    public string TemplatesDirectory => _templatesDir;

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

    public string LoadTemplate(string name)
    {
        var path = GetTemplatePath(name);
        if (!File.Exists(path))
            return string.Empty;

        return File.ReadAllText(path);
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

    public static IReadOnlyDictionary<string, string> DefaultVariables(string id, string title, DateTime now)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = id,
            ["title"] = title,
            ["date"] = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["time"] = now.ToString("HH:mm", CultureInfo.InvariantCulture),
            ["datetime"] = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        };
    }

    private string GetTemplatePath(string name)
    {
        // Basic sanitation (avoid path traversal)
        var safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        safe = safe.Replace("..", string.Empty, StringComparison.Ordinal);
        return Path.Combine(_templatesDir, safe + ".md");
    }
}

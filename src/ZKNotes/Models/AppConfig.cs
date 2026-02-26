using System;
using System.IO;
using System.Text.Json;

namespace ZKNotes.Models;

/// <summary>
/// Persistent application configuration stored as JSON.
/// </summary>
public sealed class AppConfig
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZKNotes");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public string NotesDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ZKNotes");

    public string AttachmentsSubfolder { get; set; } = "_attachments";
    public string TemplatesSubfolder { get; set; } = "_templates";

    public static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}

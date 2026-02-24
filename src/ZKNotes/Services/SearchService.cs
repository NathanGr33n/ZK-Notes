using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ZKNotes.Models;

namespace ZKNotes.Services;

/// <summary>
/// Full-text search index using SQLite FTS5.
/// Supports full-text search, tag filtering, and ranked results.
/// </summary>
public sealed class SearchService : IDisposable
{
    private readonly SqliteConnection _db;

    public SearchService(string notesDirectory)
    {
        var dbPath = Path.Combine(notesDirectory, ".zk-search.db");
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS notes_fts USING fts5(
                id UNINDEXED,
                title,
                content,
                tags
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Rebuilds the entire search index from a list of notes.
    /// </summary>
    public async Task RebuildIndexAsync(List<Note> notes)
    {
        await Task.Run(() =>
        {
            using var transaction = _db.BeginTransaction();
            using var deleteCmd = _db.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM notes_fts;";
            deleteCmd.ExecuteNonQuery();

            foreach (var note in notes)
            {
                InsertNote(note);
            }

            transaction.Commit();
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds or updates a single note in the index.
    /// </summary>
    public void IndexNote(Note note)
    {
        RemoveFromIndex(note.Id);
        InsertNote(note);
    }

    /// <summary>
    /// Removes a note from the index.
    /// </summary>
    public void RemoveFromIndex(string noteId)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM notes_fts WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", noteId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Searches for notes matching the query. Returns note IDs ranked by relevance.
    /// </summary>
    public List<string> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var results = new List<string>();

        // Sanitize query for FTS5: escape special chars and add prefix matching
        var sanitized = SanitizeFtsQuery(query);

        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT id FROM notes_fts
            WHERE notes_fts MATCH @query
            ORDER BY rank
            LIMIT 50;
            """;
        cmd.Parameters.AddWithValue("@query", sanitized);

        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }
        }
        catch (SqliteException)
        {
            // Malformed query — return empty results
        }

        return results;
    }

    /// <summary>
    /// Searches for notes with a specific tag.
    /// </summary>
    public List<string> SearchByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return [];

        var results = new List<string>();
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT id FROM notes_fts
            WHERE tags MATCH @tag
            ORDER BY rank
            LIMIT 50;
            """;
        cmd.Parameters.AddWithValue("@tag", $"\"{tag.ToLowerInvariant()}\"");

        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }
        }
        catch (SqliteException)
        {
            // Malformed query
        }

        return results;
    }

    private void InsertNote(Note note)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notes_fts (id, title, content, tags)
            VALUES (@id, @title, @content, @tags);
            """;
        cmd.Parameters.AddWithValue("@id", note.Id);
        cmd.Parameters.AddWithValue("@title", note.Title);
        cmd.Parameters.AddWithValue("@content", note.Content);
        cmd.Parameters.AddWithValue("@tags", string.Join(" ", note.Tags));
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sanitizes user input for FTS5 MATCH queries.
    /// Adds prefix matching (*) for partial word search.
    /// </summary>
    private static string SanitizeFtsQuery(string query)
    {
        // Remove FTS5 special operators to prevent injection
        var cleaned = query
            .Replace("\"", "")
            .Replace("*", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(":", "")
            .Replace("^", "")
            .Trim();

        if (string.IsNullOrEmpty(cleaned))
            return "\"\"";

        // Split into terms and add prefix matching
        var terms = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", terms.Select(t => $"\"{t}\"*"));
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

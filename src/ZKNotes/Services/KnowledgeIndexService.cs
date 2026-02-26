using System;
using System.Collections.Generic;
using System.Linq;
using ZKNotes.Helpers;
using ZKNotes.Models;

namespace ZKNotes.Services;

/// <summary>
/// In-memory knowledge index derived from loaded notes.
/// Provides backlinks, outbound link resolution, and orphan detection.
/// </summary>
public sealed class KnowledgeIndexService
{
    private readonly object _gate = new();

    private Dictionary<string, Note> _notesById = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<string>> _idsByTitle = new(StringComparer.OrdinalIgnoreCase);

    // noteId -> outbound link targets (resolved to IDs where possible)
    private Dictionary<string, HashSet<string>> _outboundById = new(StringComparer.OrdinalIgnoreCase);

    // noteId -> inbound link sources (IDs of notes that link to this note)
    private Dictionary<string, HashSet<string>> _inboundById = new(StringComparer.OrdinalIgnoreCase);

    // noteId -> outbound link tokens (raw targets from [[...]]; includes unresolved title refs)
    private Dictionary<string, HashSet<string>> _outboundTokensById = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Updates a single note in the index incrementally without rebuilding.
    /// </summary>
    public void UpdateNote(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);
        
        lock (_gate)
        {
            // Remove old links first
            RemoveNoteInternal(note.Id);
            
            // Add updated note
            AddNoteInternal(note);
        }
    }

    /// <summary>
    /// Removes a note from the index.
    /// </summary>
    public void RemoveNote(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return;
            
        lock (_gate)
        {
            RemoveNoteInternal(noteId);
        }
    }

    /// <summary>
    /// Rebuilds the entire index from a collection of notes.
    /// </summary>
    public void Rebuild(IEnumerable<Note> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        lock (_gate)
        {
            _notesById = notes
                .Where(n => !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _idsByTitle = _notesById.Values
                .Where(n => !string.IsNullOrWhiteSpace(n.Title))
                .GroupBy(n => n.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(n => n.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            _outboundById = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _inboundById = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _outboundTokensById = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            var idSet = new HashSet<string>(_notesById.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var note in _notesById.Values)
            {
                var tokens = LinkParser.ExtractLinks(note.Content)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                _outboundTokensById[note.Id] = tokens;

                var resolvedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var token in tokens)
                {
                    if (TryResolveToId(token, idSet, out var targetId))
                        resolvedTargets.Add(targetId);
                }

                _outboundById[note.Id] = resolvedTargets;

                foreach (var targetId in resolvedTargets)
                {
                    if (!_inboundById.TryGetValue(targetId, out var inbound))
                    {
                        inbound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        _inboundById[targetId] = inbound;
                    }

                    inbound.Add(note.Id);
                }
            }

            // Ensure keys exist for all notes (so callers don't need null checks)
            foreach (var id in _notesById.Keys)
            {
                _outboundById.TryAdd(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                _inboundById.TryAdd(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                _outboundTokensById.TryAdd(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
        }
    }

    public IReadOnlyList<Note> GetBacklinks(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return [];

        lock (_gate)
        {
            if (!_inboundById.TryGetValue(noteId, out var inboundIds) || inboundIds.Count == 0)
                return [];

            return inboundIds
                .Select(id => _notesById.TryGetValue(id, out var n) ? n : null)
                .Where(n => n is not null)
                .OrderByDescending(n => n!.LastEdit)
                .Cast<Note>()
                .ToList();
        }
    }

    public IReadOnlyList<Note> GetOrphanNotes()
    {
        lock (_gate)
        {
            return _notesById.Values
                .Where(n =>
                {
                    var inbound = _inboundById.TryGetValue(n.Id, out var inb) ? inb.Count : 0;
                    var outboundResolved = _outboundById.TryGetValue(n.Id, out var outb) ? outb.Count : 0;
                    var outboundTokens = _outboundTokensById.TryGetValue(n.Id, out var tok) ? tok.Count : 0;

                    // Orphan = no inbound links, and no outbound links (count outbound tokens even if unresolved)
                    return inbound == 0 && outboundResolved == 0 && outboundTokens == 0;
                })
                .OrderByDescending(n => n.LastEdit)
                .ToList();
        }
    }

    public IReadOnlyCollection<string> GetOutboundLinkIds(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return Array.Empty<string>();

        lock (_gate)
        {
            return _outboundById.TryGetValue(noteId, out var set)
                ? set.ToList()
                : Array.Empty<string>();
        }
    }

    private bool TryResolveToId(string token, HashSet<string> idSet, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Direct ID link
        if (idSet.Contains(token))
        {
            id = token;
            return true;
        }

        // Legacy title link: resolve only if title maps to exactly 1 note
        if (_idsByTitle.TryGetValue(token.Trim(), out var ids) && ids.Count == 1)
        {
            id = ids[0];
            return true;
        }

        return false;
    }

    private void AddNoteInternal(Note note)
    {
        if (string.IsNullOrWhiteSpace(note.Id))
            return;

        // Add to notes dictionary
        _notesById[note.Id] = note;

        // Update title index
        if (!string.IsNullOrWhiteSpace(note.Title))
        {
            var titleKey = note.Title.Trim();
            if (!_idsByTitle.TryGetValue(titleKey, out var ids))
            {
                ids = new List<string>();
                _idsByTitle[titleKey] = ids;
            }
            if (!ids.Contains(note.Id, StringComparer.OrdinalIgnoreCase))
            {
                ids.Add(note.Id);
            }
        }

        // Extract links
        var tokens = LinkParser.ExtractLinks(note.Content)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _outboundTokensById[note.Id] = tokens;

        // Resolve links
        var idSet = new HashSet<string>(_notesById.Keys, StringComparer.OrdinalIgnoreCase);
        var resolvedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var token in tokens)
        {
            if (TryResolveToId(token, idSet, out var targetId))
                resolvedTargets.Add(targetId);
        }

        _outboundById[note.Id] = resolvedTargets;

        // Update inbound links
        foreach (var targetId in resolvedTargets)
        {
            if (!_inboundById.TryGetValue(targetId, out var inbound))
            {
                inbound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _inboundById[targetId] = inbound;
            }
            inbound.Add(note.Id);
        }

        // Ensure empty sets exist
        _outboundById.TryAdd(note.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        _inboundById.TryAdd(note.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        _outboundTokensById.TryAdd(note.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private void RemoveNoteInternal(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
            return;

        // Remove from title index
        if (_notesById.TryGetValue(noteId, out var note) && !string.IsNullOrWhiteSpace(note.Title))
        {
            var titleKey = note.Title.Trim();
            if (_idsByTitle.TryGetValue(titleKey, out var ids))
            {
                ids.RemoveAll(id => id.Equals(noteId, StringComparison.OrdinalIgnoreCase));
                if (ids.Count == 0)
                {
                    _idsByTitle.Remove(titleKey);
                }
            }
        }

        // Remove outbound links
        if (_outboundById.TryGetValue(noteId, out var outbound))
        {
            foreach (var targetId in outbound)
            {
                if (_inboundById.TryGetValue(targetId, out var inbound))
                {
                    inbound.Remove(noteId);
                }
            }
        }

        // Remove inbound links (notes that pointed to this note)
        if (_inboundById.TryGetValue(noteId, out var inboundLinks))
        {
            foreach (var sourceId in inboundLinks.ToList())
            {
                if (_outboundById.TryGetValue(sourceId, out var sourceOutbound))
                {
                    sourceOutbound.Remove(noteId);
                }
            }
        }

        // Remove from all dictionaries
        _notesById.Remove(noteId);
        _outboundById.Remove(noteId);
        _inboundById.Remove(noteId);
        _outboundTokensById.Remove(noteId);
    }
}

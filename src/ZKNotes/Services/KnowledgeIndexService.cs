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
}

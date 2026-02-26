using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.ViewModels;

public sealed class LinkSuggestion
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the note editor with Markdown editing and preview.
/// Includes debounced auto-save and link/tag suggestion support.
/// </summary>
public partial class NoteEditorViewModel : ObservableObject
{
    private readonly StorageService _storage;
    private readonly MainViewModel _main;
    private CancellationTokenSource? _saveCts;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _tagsDisplay = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LinkSuggestion> _linkSuggestions = [];

    [ObservableProperty]
    private LinkSuggestion? _selectedLinkSuggestion;

    [ObservableProperty]
    private bool _showLinkSuggestions;

    [ObservableProperty]
    private ObservableCollection<string> _tagSuggestions = [];

    [ObservableProperty]
    private string? _selectedTagSuggestion;

    [ObservableProperty]
    private bool _showTagSuggestions;

    private Note? _currentNote;

    public NoteEditorViewModel(StorageService storage, MainViewModel main)
    {
        _storage = storage;
        _main = main;
    }

    public void LoadNote(Note note)
    {
        _currentNote = note;
        Title = note.Title;
        Content = note.Content;
        TagsDisplay = note.Tags.Count > 0 ? string.Join(", ", note.Tags.Select(t => $"#{t}")) : "";
    }

    partial void OnContentChanged(string value)
    {
        DebounceSave();
    }

    partial void OnTitleChanged(string value)
    {
        DebounceSave();
    }

    [RelayCommand]
    private async Task SaveNowAsync()
    {
        if (_currentNote is null)
            return;

        _currentNote.Title = Title;
        _currentNote.Content = Content;
        _currentNote.LastEdit = DateTime.Now;

        await _main.OnNoteSavedAsync(_currentNote).ConfigureAwait(false);

        App.Current.Dispatcher.Invoke(() =>
        {
            TagsDisplay = _currentNote.Tags.Count > 0
                ? string.Join(", ", _currentNote.Tags.Select(t => $"#{t}"))
                : "";
        });
    }

    /// <summary>
    /// Updates link suggestions based on partial [[input.
    /// Suggestions return stable IDs and will be inserted as [[id|Title]].
    /// </summary>
    public void UpdateLinkSuggestions(string partialText)
    {
        partialText ??= string.Empty;

        if (_currentNote is null)
        {
            ShowLinkSuggestions = false;
            return;
        }

        // When the user types just "[[", show a curated list:
        // 1) Recently opened notes
        // 2) Closely related notes (shared tags)
        // 3) Fallback: recently edited notes
        if (string.IsNullOrWhiteSpace(partialText))
        {
            var notesById = _main.Notes
                .Where(n => !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var currentTags = _currentNote.Tags ?? [];
            var recentIds = _main.GetRecentNoteIds(20);

            var recentNotes = recentIds
                .Select(id => notesById.TryGetValue(id, out var n) ? n : null)
                .Where(n => n is not null && !string.Equals(n!.Id, _currentNote.Id, StringComparison.OrdinalIgnoreCase))
                .Cast<Note>();

            var relatedNotes = _main.Notes
                .Where(n => !string.Equals(n.Id, _currentNote.Id, StringComparison.OrdinalIgnoreCase))
                .Select(n => new
                {
                    Note = n,
                    SharedTags = (currentTags.Count > 0 && n.Tags is { Count: > 0 })
                        ? n.Tags.Intersect(currentTags, StringComparer.OrdinalIgnoreCase).Count()
                        : 0
                })
                .Where(x => x.SharedTags > 0)
                .OrderByDescending(x => x.SharedTags)
                .ThenByDescending(x => x.Note.LastEdit)
                .Select(x => x.Note);

            var fallbackRecentEdits = _main.Notes
                .Where(n => !string.Equals(n.Id, _currentNote.Id, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.LastEdit);

            var combined = recentNotes
                .Concat(relatedNotes)
                .Concat(fallbackRecentEdits)
                .DistinctBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .Select(n => new LinkSuggestion { Id = n.Id, Title = n.Title })
                .ToList();

            LinkSuggestions = new ObservableCollection<LinkSuggestion>(combined);
            SelectedLinkSuggestion = LinkSuggestions.FirstOrDefault();
            ShowLinkSuggestions = LinkSuggestions.Count > 0;
            return;
        }

        // Non-empty query: score by ID/title match, then boost recents + shared tags.
        var recentBoostIds = _main.GetRecentNoteIds(20);
        var tags = _currentNote.Tags ?? [];

        var candidates = _main.Notes
            .Where(n => !string.Equals(n.Id, _currentNote.Id, StringComparison.OrdinalIgnoreCase))
            .Select(n =>
            {
                var score = 0;

                if (n.Id.StartsWith(partialText, StringComparison.OrdinalIgnoreCase)) score += 80;
                else if (n.Id.Contains(partialText, StringComparison.OrdinalIgnoreCase)) score += 50;

                if (n.Title.StartsWith(partialText, StringComparison.OrdinalIgnoreCase)) score += 70;
                else if (n.Title.Contains(partialText, StringComparison.OrdinalIgnoreCase)) score += 40;

                if (recentBoostIds.Contains(n.Id, StringComparer.OrdinalIgnoreCase))
                    score += 40;

                if (tags.Count > 0 && n.Tags is { Count: > 0 })
                {
                    var shared = n.Tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Count();
                    score += shared * 10;
                }

                return new { Note = n, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Note.LastEdit)
            .Take(12)
            .Select(x => new LinkSuggestion { Id = x.Note.Id, Title = x.Note.Title })
            .ToList();

        LinkSuggestions = new ObservableCollection<LinkSuggestion>(candidates);
        SelectedLinkSuggestion = LinkSuggestions.FirstOrDefault();
        ShowLinkSuggestions = LinkSuggestions.Count > 0;
    }

    /// <summary>
    /// Updates tag suggestions based on partial #input.
    /// </summary>
    public void UpdateTagSuggestions(string partialText)
    {
        partialText ??= string.Empty;

        var recent = _main.GetRecentTags(25);

        IEnumerable<string> tagStream;
        if (string.IsNullOrWhiteSpace(partialText))
        {
            tagStream = recent;
        }
        else
        {
            var fromRecent = recent.Where(t => t.StartsWith(partialText, StringComparison.OrdinalIgnoreCase));
            var all = _main.Notes.SelectMany(n => n.Tags).Distinct(StringComparer.OrdinalIgnoreCase);
            var fromAll = all.Where(t => t.StartsWith(partialText, StringComparison.OrdinalIgnoreCase));

            tagStream = fromRecent.Concat(fromAll);
        }

        var suggestions = tagStream
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        TagSuggestions = new ObservableCollection<string>(suggestions);
        SelectedTagSuggestion = TagSuggestions.FirstOrDefault();
        ShowTagSuggestions = TagSuggestions.Count > 0;
    }

    public void HideSuggestions()
    {
        ShowLinkSuggestions = false;
        ShowTagSuggestions = false;
    }

    /// <summary>
    /// Debounced auto-save: waits 1.5 seconds after last edit before saving.
    /// </summary>
    private void DebounceSave()
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1500, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                    await SaveNowAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on rapid typing
            }
        }, token);
    }
}

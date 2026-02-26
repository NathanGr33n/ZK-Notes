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

        var currentTags = _currentNote.Tags ?? [];
        var recentIds = _main.GetRecentNoteIds(12);

        var candidates = _main.Notes
            .Where(n => n.Id != _currentNote.Id)
            .Select(n =>
            {
                var score = 0;

                if (!string.IsNullOrWhiteSpace(partialText))
                {
                    if (n.Id.StartsWith(partialText, StringComparison.OrdinalIgnoreCase)) score += 80;
                    else if (n.Id.Contains(partialText, StringComparison.OrdinalIgnoreCase)) score += 50;

                    if (n.Title.StartsWith(partialText, StringComparison.OrdinalIgnoreCase)) score += 70;
                    else if (n.Title.Contains(partialText, StringComparison.OrdinalIgnoreCase)) score += 40;
                }

                if (recentIds.Contains(n.Id, StringComparer.OrdinalIgnoreCase))
                    score += 40;

                if (currentTags.Count > 0 && n.Tags is { Count: > 0 })
                {
                    var shared = n.Tags.Intersect(currentTags, StringComparer.OrdinalIgnoreCase).Count();
                    score += shared * 10;
                }

                return new { Note = n, Score = score };
            })
            .Where(x => string.IsNullOrWhiteSpace(partialText) || x.Score > 0)
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

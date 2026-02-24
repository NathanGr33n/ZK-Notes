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

/// <summary>
/// ViewModel for the note editor with Markdown editing and preview.
/// Includes debounced auto-save and link suggestion support.
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
    private ObservableCollection<string> _linkSuggestions = [];

    [ObservableProperty]
    private bool _showLinkSuggestions;

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
    /// </summary>
    public void UpdateLinkSuggestions(string partialText)
    {
        if (string.IsNullOrWhiteSpace(partialText))
        {
            ShowLinkSuggestions = false;
            return;
        }

        var suggestions = _main.Notes
            .Where(n => n.Id != _currentNote?.Id)
            .Where(n => n.Title.Contains(partialText, StringComparison.OrdinalIgnoreCase)
                        || n.Id.Contains(partialText, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .Select(n => n.Title)
            .ToList();

        LinkSuggestions = new ObservableCollection<string>(suggestions);
        ShowLinkSuggestions = suggestions.Count > 0;
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

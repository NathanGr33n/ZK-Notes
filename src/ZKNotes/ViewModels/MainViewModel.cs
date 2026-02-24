using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Helpers;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.ViewModels;

public enum NavigationPage
{
    Notes,
    Graph,
    Search,
    Review
}

/// <summary>
/// Root ViewModel managing navigation, note collection, and cross-cutting concerns.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly StorageService _storage;
    private readonly SearchService _search;

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Notes;

    [ObservableProperty]
    private ObservableCollection<Note> _notes = [];

    [ObservableProperty]
    private Note? _selectedNote;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _statusText = "Ready";

    public NoteEditorViewModel Editor { get; }
    public SearchViewModel Search { get; }
    public GraphViewModel Graph { get; }
    public ReviewViewModel Review { get; }

    public MainViewModel(StorageService storage, SearchService search)
    {
        _storage = storage;
        _search = search;

        Editor = new NoteEditorViewModel(storage, this);
        Search = new SearchViewModel(search, this);
        Graph = new GraphViewModel(this);
        Review = new ReviewViewModel(storage, this);
    }

    public async Task InitializeAsync()
    {
        StatusText = "Loading notes…";
        var notes = await _storage.LoadAllNotesAsync().ConfigureAwait(false);

        App.Current.Dispatcher.Invoke(() =>
        {
            Notes = new ObservableCollection<Note>(notes.OrderByDescending(n => n.LastEdit));
            StatusText = $"{Notes.Count} notes loaded";
        });

        await _search.RebuildIndexAsync(notes).ConfigureAwait(false);
    }

    [RelayCommand]
    private void NavigateTo(NavigationPage page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private async Task CreateNoteAsync()
    {
        var id = await _storage.GenerateNextIdAsync().ConfigureAwait(false);
        var note = new Note
        {
            Id = id,
            Title = "Untitled Note",
            Content = string.Empty,
            Created = DateTime.Now,
            LastEdit = DateTime.Now
        };

        App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Insert(0, note);
            SelectedNote = note;
            Editor.LoadNote(note);
            IsEditing = true;
            CurrentPage = NavigationPage.Notes;
        });
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (SelectedNote is null)
            return;

        var note = SelectedNote;
        await _storage.DeleteNoteAsync(note.Id).ConfigureAwait(false);
        _search.RemoveFromIndex(note.Id);

        App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Remove(note);
            SelectedNote = Notes.FirstOrDefault();
            if (SelectedNote is not null)
                Editor.LoadNote(SelectedNote);
            else
                IsEditing = false;

            StatusText = $"Deleted {note.Title}";
        });
    }

    /// <summary>
    /// Called after a note is saved to update the collection and search index.
    /// </summary>
    public async Task OnNoteSavedAsync(Note note)
    {
        // Parse tags and links from content
        note.Tags = TagParser.ExtractTags(note.Content);
        note.Links = LinkParser.ExtractLinks(note.Content);

        await _storage.SaveNoteAsync(note).ConfigureAwait(false);
        _search.IndexNote(note);

        App.Current.Dispatcher.Invoke(() =>
        {
            StatusText = $"Saved {note.Title}";
        });
    }

    /// <summary>
    /// Creates a note from the Quick Capture popup.
    /// </summary>
    public async Task<string> CreateNoteFromCaptureAsync(string title, string content)
    {
        var id = await _storage.GenerateNextIdAsync().ConfigureAwait(false);
        var note = new Note
        {
            Id = id,
            Title = title,
            Content = content,
            Tags = TagParser.ExtractTags(content),
            Links = LinkParser.ExtractLinks(content),
            Created = DateTime.Now,
            LastEdit = DateTime.Now
        };

        await _storage.SaveNoteAsync(note).ConfigureAwait(false);
        _search.IndexNote(note);

        App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Insert(0, note);
            SelectedNote = note;
            Editor.LoadNote(note);
            IsEditing = true;
            CurrentPage = NavigationPage.Notes;
            StatusText = $"Created {note.Title}";
        });

        return id;
    }

    public void SelectNote(Note note)
    {
        SelectedNote = note;
        Editor.LoadNote(note);
        IsEditing = true;
        CurrentPage = NavigationPage.Notes;
    }
}

using System;
using System.Collections.Generic;
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
    Review,
    Tags,
    Insights
}

/// <summary>
/// Root ViewModel managing navigation, note collection, and cross-cutting concerns.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private const int MaxRecentNotes = 25;
    private const int MaxRecentTags = 50;

    private readonly object _recentGate = new();
    private readonly LinkedList<string> _recentNoteIds = new();
    private readonly LinkedList<string> _recentTags = new();

    private readonly StorageService _storage;
    private readonly SearchService _search;
    private readonly TemplateService _templates;
    private readonly KnowledgeIndexService _index;

    [ObservableProperty]
    private NavigationPage _currentPage = NavigationPage.Notes;

    [ObservableProperty]
    private ObservableCollection<Note> _notes = [];

    [ObservableProperty]
    private Note? _selectedNote;

    [ObservableProperty]
    private ObservableCollection<Note> _backlinks = [];

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private ObservableCollection<string> _templatesList = [];

    [ObservableProperty]
    private string? _selectedTemplate;

    public NoteEditorViewModel Editor { get; }
    public SearchViewModel Search { get; }
    public GraphViewModel Graph { get; }
    public ReviewViewModel Review { get; }
    public TagManagementViewModel TagManagement { get; }
    public InsightsViewModel Insights { get; }

    public MainViewModel(StorageService storage, SearchService search, TemplateService templates, KnowledgeIndexService index)
    {
        _storage = storage;
        _search = search;
        _templates = templates;
        _index = index;

        Editor = new NoteEditorViewModel(storage, this);
        Search = new SearchViewModel(search, this);
        Graph = new GraphViewModel(this);
        Review = new ReviewViewModel(storage, this);
        TagManagement = new TagManagementViewModel(this);
        Insights = new InsightsViewModel(index, this);
    }

    public async Task InitializeAsync()
    {
        StatusText = "Loading notes…";
        var notes = await _storage.LoadAllNotesAsync().ConfigureAwait(false);

        // Build link/backlink index from disk-loaded notes (works even if frontmatter links are stale)
        _index.Rebuild(notes);

        var templateNames = _templates.ListTemplateNames();

        App.Current.Dispatcher.Invoke(() =>
        {
            Notes = new ObservableCollection<Note>(notes.OrderByDescending(n => n.LastEdit));
            TemplatesList = new ObservableCollection<string>(templateNames);
            SelectedTemplate = TemplatesList.FirstOrDefault();

            StatusText = $"{Notes.Count} notes loaded";
            RefreshBacklinks();
            Insights.RefreshOrphans();
            TagManagement.Refresh();
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
        await CreateNoteInternalAsync(
            title: "Untitled Note",
            content: string.Empty,
            type: NoteType.Standard,
            template: null,
            linkToCurrent: false)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CreateFleetingNoteAsync()
    {
        await CreateNoteInternalAsync(
            title: $"Fleeting {DateTime.Now:yyyy-MM-dd HHmm}",
            content: string.Empty,
            type: NoteType.Fleeting,
            template: null,
            linkToCurrent: true)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CreateJournalNoteAsync()
    {
        var templateName = "journal";
        var title = $"Journal {DateTime.Now:yyyy-MM-dd}";

        var content = _templates.TemplateExists(templateName)
            ? _templates.LoadTemplate(templateName)
            : "# {{title}}\n\n";

        await CreateNoteInternalAsync(
            title: title,
            content: content,
            type: NoteType.Journal,
            template: _templates.TemplateExists(templateName) ? templateName : null,
            linkToCurrent: false)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CreateFromTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedTemplate))
        {
            StatusText = "Select a template";
            return;
        }

        var templateName = SelectedTemplate;
        var title = templateName;
        var content = _templates.LoadTemplate(templateName);

        await CreateNoteInternalAsync(
            title: title,
            content: content,
            type: NoteType.Standard,
            template: templateName,
            linkToCurrent: false)
            .ConfigureAwait(false);
    }

    private async Task CreateNoteInternalAsync(string title, string content, NoteType type, string? template, bool linkToCurrent)
    {
        var id = await _storage.GenerateNextIdAsync().ConfigureAwait(false);
        var now = DateTime.Now;

        var templateVars = TemplateService.DefaultVariables(id, title, now);
        var resolvedContent = _templates.ApplyVariables(content ?? string.Empty, templateVars);

        if (linkToCurrent)
        {
            var current = App.Current.Dispatcher.Invoke(() => SelectedNote);
            if (current is not null)
            {
                resolvedContent = $"[[{current.Id}|{current.Title}]]\n\n" + resolvedContent;
            }
        }

        var note = new Note
        {
            Id = id,
            Title = title,
            Content = resolvedContent,
            Type = type,
            Template = template,
            Created = now,
            LastEdit = now,
            Tags = TagParser.ExtractTags(resolvedContent),
            Links = LinkParser.ExtractLinks(resolvedContent)
        };

        await _storage.SaveNoteAsync(note).ConfigureAwait(false);
        _search.IndexNote(note);

        var notesSnapshot = App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Insert(0, note);
            SelectedNote = note;
            Editor.LoadNote(note);
            IsEditing = true;
            CurrentPage = NavigationPage.Notes;
            StatusText = $"Created {note.Title}";

            return Notes.ToList();
        });

        _index.Rebuild(notesSnapshot);

        App.Current.Dispatcher.Invoke(() =>
        {
            RefreshBacklinks();
            Insights.RefreshOrphans();
            Search.RefreshTags();
            TagManagement.Refresh();
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

        var notesSnapshot = App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Remove(note);
            SelectedNote = Notes.FirstOrDefault();
            if (SelectedNote is not null)
                Editor.LoadNote(SelectedNote);
            else
                IsEditing = false;

            StatusText = $"Deleted {note.Title}";

            return Notes.ToList();
        });

        _index.Rebuild(notesSnapshot);

        App.Current.Dispatcher.Invoke(() =>
        {
            RefreshBacklinks();
            Insights.RefreshOrphans();
        });
    }

    /// <summary>
    /// Called after a note is saved to update the collection and search index.
    /// </summary>
    public async Task OnNoteSavedAsync(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        // Parse tags and links from content
        note.Tags = TagParser.ExtractTags(note.Content);
        note.Links = LinkParser.ExtractLinks(note.Content);

        TrackRecentTags(note.Tags);

        await _storage.SaveNoteAsync(note).ConfigureAwait(false);
        _search.IndexNote(note);

        // Rebuild knowledge index from a UI-thread snapshot of notes (ObservableCollection is not thread-safe)
        var notesSnapshot = App.Current.Dispatcher.Invoke(() => Notes.ToList());
        _index.Rebuild(notesSnapshot);

        App.Current.Dispatcher.Invoke(() =>
        {
            StatusText = $"Saved {note.Title}";
            RefreshBacklinks();
            Insights.RefreshOrphans();
            Search.RefreshTags();
        });
    }

    /// <summary>
    /// Saves and re-indexes many notes in a single pass.
    /// Used for bulk operations (tag rename/merge, templates, migrations, etc.).
    /// </summary>
    public async Task SaveNotesBatchAsync(System.Collections.Generic.IReadOnlyList<Note> notes, string statusText)
    {
        if (notes is null || notes.Count == 0)
            return;

        foreach (var note in notes)
        {
            // Parse tags and links from content
            note.Tags = TagParser.ExtractTags(note.Content);
            note.Links = LinkParser.ExtractLinks(note.Content);

            TrackRecentTags(note.Tags);

            await _storage.SaveNoteAsync(note).ConfigureAwait(false);
            _search.IndexNote(note);
        }

        // Rebuild knowledge index once for the full batch
        var notesSnapshot = App.Current.Dispatcher.Invoke(() => Notes.ToList());
        _index.Rebuild(notesSnapshot);

        App.Current.Dispatcher.Invoke(() =>
        {
            StatusText = statusText;
            RefreshBacklinks();
            Insights.RefreshOrphans();
            Search.RefreshTags();
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

        var notesSnapshot = App.Current.Dispatcher.Invoke(() =>
        {
            Notes.Insert(0, note);
            SelectedNote = note;
            Editor.LoadNote(note);
            IsEditing = true;
            CurrentPage = NavigationPage.Notes;
            StatusText = $"Created {note.Title}";

            return Notes.ToList();
        });

        _index.Rebuild(notesSnapshot);

        App.Current.Dispatcher.Invoke(() =>
        {
            RefreshBacklinks();
            Insights.RefreshOrphans();
        });

        return id;
    }

    partial void OnSelectedNoteChanged(Note? value)
    {
        RefreshBacklinks();
    }

    private void RefreshBacklinks()
    {
        if (SelectedNote is null)
        {
            Backlinks = [];
            return;
        }

        var backlinks = _index.GetBacklinks(SelectedNote.Id);
        Backlinks = new ObservableCollection<Note>(backlinks);
    }

    public void SelectNote(Note note)
    {
        TrackRecentNoteId(note.Id);

        SelectedNote = note;
        Editor.LoadNote(note);
        IsEditing = true;
        CurrentPage = NavigationPage.Notes;
    }

    public IReadOnlyList<string> GetRecentNoteIds(int max)
    {
        lock (_recentGate)
        {
            return _recentNoteIds.Take(max).ToList();
        }
    }

    public IReadOnlyList<string> GetRecentTags(int max)
    {
        lock (_recentGate)
        {
            return _recentTags.Take(max).ToList();
        }
    }

    private void TrackRecentNoteId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        lock (_recentGate)
        {
            var existing = _recentNoteIds.Find(id);
            if (existing is not null)
                _recentNoteIds.Remove(existing);

            _recentNoteIds.AddFirst(id);
            while (_recentNoteIds.Count > MaxRecentNotes)
                _recentNoteIds.RemoveLast();
        }
    }

    private void TrackRecentTags(IEnumerable<string> tags)
    {
        lock (_recentGate)
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                var normalized = tag.Trim().ToLowerInvariant();

                var existing = _recentTags.Find(normalized);
                if (existing is not null)
                    _recentTags.Remove(existing);

                _recentTags.AddFirst(normalized);
            }

            while (_recentTags.Count > MaxRecentTags)
                _recentTags.RemoveLast();
        }
    }
}

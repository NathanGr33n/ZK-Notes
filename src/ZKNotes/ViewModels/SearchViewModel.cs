using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.ViewModels;

/// <summary>
/// ViewModel for full-text search and tag filtering.
/// </summary>
public partial class SearchViewModel : ObservableObject
{
    private readonly SearchService _search;
    private readonly MainViewModel _main;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Note> _results = [];

    [ObservableProperty]
    private ObservableCollection<string> _allTags = [];

    [ObservableProperty]
    private string? _selectedTag;

    public SearchViewModel(SearchService search, MainViewModel main)
    {
        _search = search;
        _main = main;
    }

    partial void OnQueryChanged(string value)
    {
        PerformSearch();
    }

    [RelayCommand]
    private void PerformSearch()
    {
        var ids = string.IsNullOrWhiteSpace(Query)
            ? []
            : _search.Search(Query);

        var notes = _main.Notes
            .Where(n => ids.Contains(n.Id))
            .ToList();

        Results = new ObservableCollection<Note>(notes);
    }

    [RelayCommand]
    private void SearchByTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        SelectedTag = tag;
        var ids = _search.SearchByTag(tag);
        var notes = _main.Notes
            .Where(n => ids.Contains(n.Id))
            .ToList();

        Results = new ObservableCollection<Note>(notes);
    }

    [RelayCommand]
    private void OpenNote(Note? note)
    {
        if (note is not null)
            _main.SelectNote(note);
    }

    /// <summary>
    /// Refreshes the list of all tags from loaded notes.
    /// </summary>
    public void RefreshTags()
    {
        var tags = _main.Notes
            .SelectMany(n => n.Tags)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        AllTags = new ObservableCollection<string>(tags);
    }
}

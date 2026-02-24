using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.ViewModels;

/// <summary>
/// ViewModel for the weekly review panel.
/// Surfaces notes that haven't been reviewed recently.
/// </summary>
public partial class ReviewViewModel : ObservableObject
{
    private readonly StorageService _storage;
    private readonly MainViewModel _main;
    private const int ReviewIntervalDays = 7;

    [ObservableProperty]
    private ObservableCollection<Note> _reviewNotes = [];

    [ObservableProperty]
    private string _reviewStatus = string.Empty;

    public ReviewViewModel(StorageService storage, MainViewModel main)
    {
        _storage = storage;
        _main = main;
    }

    /// <summary>
    /// Refreshes the list of notes due for review.
    /// Notes are due if they were never reviewed or last reviewed > 7 days ago.
    /// </summary>
    [RelayCommand]
    public void RefreshReviewList()
    {
        var cutoff = DateTime.Now.AddDays(-ReviewIntervalDays);

        var dueNotes = _main.Notes
            .Where(n => n.LastReviewed is null || n.LastReviewed < cutoff)
            .OrderBy(n => n.LastReviewed ?? DateTime.MinValue)
            .Take(20)
            .ToList();

        ReviewNotes = new ObservableCollection<Note>(dueNotes);
        ReviewStatus = dueNotes.Count > 0
            ? $"{dueNotes.Count} notes due for review"
            : "All caught up! No notes due for review.";
    }

    [RelayCommand]
    private async Task MarkReviewedAsync(Note? note)
    {
        if (note is null) return;

        note.LastReviewed = DateTime.Now;
        await _storage.SaveNoteAsync(note).ConfigureAwait(false);

        App.Current.Dispatcher.Invoke(() =>
        {
            ReviewNotes.Remove(note);
            ReviewStatus = ReviewNotes.Count > 0
                ? $"{ReviewNotes.Count} notes due for review"
                : "All caught up! No notes due for review.";
        });
    }

    [RelayCommand]
    private void OpenNote(Note? note)
    {
        if (note is not null)
            _main.SelectNote(note);
    }
}

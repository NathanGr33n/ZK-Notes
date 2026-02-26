using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Models;
using ZKNotes.Services;

namespace ZKNotes.ViewModels;

/// <summary>
/// ViewModel for knowledge-base insights.
/// Currently surfaces orphaned notes (no inbound or outbound links).
/// </summary>
public partial class InsightsViewModel : ObservableObject
{
    private readonly KnowledgeIndexService _index;
    private readonly MainViewModel _main;

    [ObservableProperty]
    private ObservableCollection<Note> _orphanNotes = [];

    [ObservableProperty]
    private string _orphanStatus = string.Empty;

    public InsightsViewModel(KnowledgeIndexService index, MainViewModel main)
    {
        _index = index;
        _main = main;
    }

    [RelayCommand]
    public void RefreshOrphans()
    {
        var orphans = _index.GetOrphanNotes().ToList();
        OrphanNotes = new ObservableCollection<Note>(orphans);
        OrphanStatus = orphans.Count == 0
            ? "No orphan notes"
            : $"{orphans.Count} orphan notes";
    }

    [RelayCommand]
    private void OpenNote(Note? note)
    {
        if (note is null)
            return;

        _main.SelectNote(note);
        _main.NavigateToCommand.Execute(NavigationPage.Notes);
    }
}

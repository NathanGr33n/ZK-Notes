using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZKNotes.Helpers;
using ZKNotes.Models;

namespace ZKNotes.ViewModels;

public sealed class TagUsage
{
    public string Tag { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>
/// Tag management: lists tags, supports renaming and merging tags.
/// </summary>
public partial class TagManagementViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    [ObservableProperty]
    private ObservableCollection<TagUsage> _tags = [];

    [ObservableProperty]
    private TagUsage? _selectedTag;

    [ObservableProperty]
    private ObservableCollection<Note> _notesWithTag = [];

    [ObservableProperty]
    private ObservableCollection<TagUsage> _mergeTargets = [];

    [ObservableProperty]
    private TagUsage? _selectedMergeTarget;

    [ObservableProperty]
    private string _renameTo = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public TagManagementViewModel(MainViewModel main)
    {
        _main = main;
    }

    partial void OnSelectedTagChanged(TagUsage? value)
    {
        RefreshSelectedTagDetails();

        RenameTo = value?.Tag ?? string.Empty;
        SelectedMergeTarget = null;

        MergeTargets = new ObservableCollection<TagUsage>(
            Tags.Where(t => !string.Equals(t.Tag, value?.Tag, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    public void Refresh()
    {
        var tags = _main.Notes
            .SelectMany(n => n.Tags)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TagUsage { Tag = g.Key.ToLowerInvariant(), Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag)
            .ToList();

        Tags = new ObservableCollection<TagUsage>(tags);

        // If selected tag disappeared (merge/rename), clear selection.
        if (SelectedTag is not null && !Tags.Any(t => string.Equals(t.Tag, SelectedTag.Tag, StringComparison.OrdinalIgnoreCase)))
            SelectedTag = null;

        RefreshSelectedTagDetails();

        StatusText = $"{Tags.Count} tags";
    }

    private void RefreshSelectedTagDetails()
    {
        if (SelectedTag is null)
        {
            NotesWithTag = [];
            MergeTargets = new ObservableCollection<TagUsage>(Tags);
            return;
        }

        var tag = SelectedTag.Tag;
        var notes = _main.Notes
            .Where(n => n.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(n => n.LastEdit)
            .ToList();

        NotesWithTag = new ObservableCollection<Note>(notes);

        MergeTargets = new ObservableCollection<TagUsage>(
            Tags.Where(t => !string.Equals(t.Tag, SelectedTag.Tag, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private void OpenNote(Note? note)
    {
        if (note is null) return;
        _main.SelectNote(note);
        _main.NavigateToCommand.Execute(NavigationPage.Notes);
    }

    [RelayCommand]
    private async Task RenameSelectedTagAsync()
    {
        if (SelectedTag is null)
        {
            StatusText = "Select a tag to rename";
            return;
        }

        var from = TagRewriter.Normalize(SelectedTag.Tag);
        var to = TagRewriter.Normalize(RenameTo);

        if (!TagRewriter.IsValidTag(to))
        {
            StatusText = "Invalid tag name";
            return;
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            StatusText = "No changes";
            return;
        }

        // If the destination tag already exists, this is effectively a merge.
        if (Tags.Any(t => string.Equals(t.Tag, to, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "Destination tag already exists (use Merge instead)";
            return;
        }

        var affected = _main.Notes
            .Where(n => n.Tags.Contains(from, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var note in affected)
        {
            note.Content = TagRewriter.RenameTag(note.Content, from, to);
        }

        await _main.SaveNotesBatchAsync(affected, $"Renamed #{from} → #{to} ({affected.Count} notes)")
            .ConfigureAwait(false);

        Refresh();
        SelectedTag = Tags.FirstOrDefault(t => string.Equals(t.Tag, to, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private async Task MergeSelectedTagAsync()
    {
        if (SelectedTag is null)
        {
            StatusText = "Select a tag to merge";
            return;
        }

        if (SelectedMergeTarget is null)
        {
            StatusText = "Select a merge target";
            return;
        }

        var from = TagRewriter.Normalize(SelectedTag.Tag);
        var to = TagRewriter.Normalize(SelectedMergeTarget.Tag);

        if (!TagRewriter.IsValidTag(to))
        {
            StatusText = "Invalid target tag";
            return;
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            StatusText = "No changes";
            return;
        }

        var affected = _main.Notes
            .Where(n => n.Tags.Contains(from, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var note in affected)
        {
            note.Content = TagRewriter.RenameTag(note.Content, from, to);
        }

        await _main.SaveNotesBatchAsync(affected, $"Merged #{from} → #{to} ({affected.Count} notes)")
            .ConfigureAwait(false);

        Refresh();
        SelectedTag = Tags.FirstOrDefault(t => string.Equals(t.Tag, to, StringComparison.OrdinalIgnoreCase));
    }
}

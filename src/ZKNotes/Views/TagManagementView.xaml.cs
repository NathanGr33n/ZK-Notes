using System.Windows;
using System.Windows.Controls;
using ZKNotes.Models;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class TagManagementView : UserControl
{
    public TagManagementView()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is MainViewModel vm)
            vm.TagManagement.RefreshCommand.Execute(null);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.TagManagement.RefreshCommand.Execute(null);
    }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.TagManagement.RenameSelectedTagCommand.Execute(null);
    }

    private void Merge_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.TagManagement.MergeSelectedTagCommand.Execute(null);
    }

    private void OpenNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Note note } && DataContext is MainViewModel vm)
            vm.TagManagement.OpenNoteCommand.Execute(note);
    }
}

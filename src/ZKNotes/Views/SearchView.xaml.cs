using System.Windows;
using System.Windows.Controls;
using ZKNotes.Models;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is MainViewModel vm)
        {
            vm.Search.RefreshTags();
        }
    }

    private void Tag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && DataContext is MainViewModel vm)
        {
            vm.Search.SearchByTagCommand.Execute(tag);
        }
    }

    private void Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: Note note } && DataContext is MainViewModel vm)
        {
            vm.Search.OpenNoteCommand.Execute(note);
        }
    }
}

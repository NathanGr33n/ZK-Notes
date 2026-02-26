using System.Windows;
using System.Windows.Controls;
using ZKNotes.Models;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class InsightsView : UserControl
{
    public InsightsView()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is MainViewModel vm)
        {
            vm.Insights.RefreshOrphansCommand.Execute(null);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Insights.RefreshOrphansCommand.Execute(null);
    }

    private void OpenNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Note note } && DataContext is MainViewModel vm)
            vm.Insights.OpenNoteCommand.Execute(note);
    }
}

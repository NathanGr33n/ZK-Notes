using System.Windows;
using System.Windows.Controls;
using ZKNotes.Models;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class ReviewView : UserControl
{
    public ReviewView()
    {
        InitializeComponent();
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is MainViewModel vm)
        {
            vm.Review.RefreshReviewList();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Review.RefreshReviewListCommand.Execute(null);
    }

    private void OpenNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Note note } && DataContext is MainViewModel vm)
            vm.Review.OpenNoteCommand.Execute(note);
    }

    private void MarkReviewed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Note note } && DataContext is MainViewModel vm)
            vm.Review.MarkReviewedCommand.Execute(note);
    }
}

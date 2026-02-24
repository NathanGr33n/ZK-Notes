using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZKNotes.Helpers;
using ZKNotes.Models;
using ZKNotes.ViewModels;
using ZKNotes.Views;

namespace ZKNotes;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Register Ctrl+Shift+N for quick capture
        var quickCapture = new RoutedCommand();
        quickCapture.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift));
        CommandBindings.Add(new CommandBinding(quickCapture, QuickCapture_Executed));
    }

    private async void QuickCapture_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var popup = new QuickCaptureWindow { Owner = this };
        popup.ShowDialog();

        if (!popup.WasSaved)
            return;

        // Create and save the new note
        var id = await vm.CreateNoteFromCaptureAsync(popup.NoteTitle, popup.NoteContent);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string pageStr &&
            Enum.TryParse<NavigationPage>(pageStr, out var page) &&
            DataContext is MainViewModel vm)
        {
            vm.NavigateToCommand.Execute(page);
        }
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class NoteListView : UserControl
{
    private bool _webViewReady;

    public NoteListView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await PreviewWebView.EnsureCoreWebView2Async();
            _webViewReady = true;
            UpdatePreview();
        }
        catch
        {
            // WebView2 runtime not available — preview will be disabled
        }

        if (DataContext is MainViewModel vm)
        {
            vm.Editor.PropertyChanged += Editor_PropertyChanged;
        }
    }

    private void Editor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NoteEditorViewModel.PreviewHtml))
        {
            Dispatcher.Invoke(UpdatePreview);
        }
    }

    private void UpdatePreview()
    {
        if (!_webViewReady || DataContext is not MainViewModel vm)
            return;

        PreviewWebView.NavigateToString(vm.Editor.PreviewHtml);
    }

    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedNote is not null)
        {
            vm.SelectNote(vm.SelectedNote);
        }
    }
}

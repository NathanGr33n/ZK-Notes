using System.Windows;
using System.Windows.Input;

namespace ZKNotes.Views;

public partial class QuickCaptureWindow : Window
{
    public string NoteTitle { get; private set; } = string.Empty;
    public string NoteContent { get; private set; } = string.Empty;
    public bool WasSaved { get; private set; }

    public QuickCaptureWindow()
    {
        InitializeComponent();
        TitleBox.Focus();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        NoteTitle = TitleBox.Text.Trim();
        NoteContent = ContentBox.Text;

        if (string.IsNullOrWhiteSpace(NoteTitle))
        {
            MessageBox.Show("Please enter a title.", "Quick Capture",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        WasSaved = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

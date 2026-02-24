using System.Windows.Controls;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class NoteListView : UserControl
{
    public NoteListView()
    {
        InitializeComponent();
    }

    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedNote is not null)
        {
            vm.SelectNote(vm.SelectedNote);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ZKNotes.Models;
using ZKNotes.ViewModels;

namespace ZKNotes.Views;

public partial class NoteListView : UserControl
{
    private enum SuggestionMode
    {
        None,
        Link,
        Tag
    }

    private SuggestionMode _suggestionMode;
    private int _tokenStartOffset;

    public NoteListView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Hook AvalonEdit events once XAML is loaded.
        if (ContentEditor is null)
            return;

        ContentEditor.TextArea.TextEntered += ContentEditor_TextEntered;
        ContentEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        ContentEditor.TextArea.PreviewKeyDown += ContentEditor_PreviewKeyDown;
        ContentEditor.TextArea.LostKeyboardFocus += ContentEditor_LostKeyboardFocus;

        LinkSuggestionsList.MouseDoubleClick += (_, _) => AcceptSelectedSuggestion();
        TagSuggestionsList.MouseDoubleClick += (_, _) => AcceptSelectedSuggestion();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ContentEditor is null)
            return;

        ContentEditor.TextArea.TextEntered -= ContentEditor_TextEntered;
        ContentEditor.TextArea.Caret.PositionChanged -= Caret_PositionChanged;
        ContentEditor.TextArea.PreviewKeyDown -= ContentEditor_PreviewKeyDown;
        ContentEditor.TextArea.LostKeyboardFocus -= ContentEditor_LostKeyboardFocus;
    }

    private void ContentEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        (DataContext as MainViewModel)?.Editor.HideSuggestions();
        _suggestionMode = SuggestionMode.None;
    }

    private void ContentEditor_TextEntered(object sender, TextCompositionEventArgs e)
    {
        UpdateSuggestions();
    }

    private void Caret_PositionChanged(object? sender, EventArgs e)
    {
        // Keep suggestions in sync when the caret moves (mouse clicks, arrow keys, etc.)
        UpdateSuggestions();
    }

    private void ContentEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        if (!vm.Editor.ShowLinkSuggestions && !vm.Editor.ShowTagSuggestions)
            return;

        if (e.Key == Key.Escape)
        {
            vm.Editor.HideSuggestions();
            _suggestionMode = SuggestionMode.None;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            MoveSelection(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            if (AcceptSelectedSuggestion())
                e.Handled = true;
        }
    }

    private void MoveSelection(int delta)
    {
        var list = _suggestionMode switch
        {
            SuggestionMode.Link => LinkSuggestionsList,
            SuggestionMode.Tag => TagSuggestionsList,
            _ => null
        };

        if (list is null || list.Items.Count == 0)
            return;

        var idx = list.SelectedIndex;
        if (idx < 0) idx = 0;

        idx = Math.Clamp(idx + delta, 0, list.Items.Count - 1);
        list.SelectedIndex = idx;
        list.ScrollIntoView(list.SelectedItem);
    }

    private bool AcceptSelectedSuggestion()
    {
        if (ContentEditor?.Document is null)
            return false;

        if (DataContext is not MainViewModel vm)
            return false;

        var caretOffset = ContentEditor.CaretOffset;
        if (_tokenStartOffset < 0 || _tokenStartOffset > caretOffset)
            return false;

        switch (_suggestionMode)
        {
            case SuggestionMode.Link:
            {
                var suggestion = vm.Editor.SelectedLinkSuggestion;
                if (suggestion is null)
                    return false;

                // Preserve a typed alias if the user is in [[target|alias]] form.
                var innerStart = _tokenStartOffset + 2;
                var innerLength = Math.Max(0, caretOffset - innerStart);
                var inner = innerLength > 0 ? ContentEditor.Document.GetText(innerStart, innerLength) : string.Empty;
                var pipe = inner.IndexOf('|', StringComparison.Ordinal);
                var alias = pipe >= 0 ? inner[(pipe + 1)..] : string.Empty;
                var display = string.IsNullOrWhiteSpace(alias) ? suggestion.Title : alias.Trim();

                var insert = $"[[{suggestion.Id}|{display}]]";
                ContentEditor.Document.Replace(_tokenStartOffset, caretOffset - _tokenStartOffset, insert);
                ContentEditor.CaretOffset = _tokenStartOffset + insert.Length;
                break;
            }
            case SuggestionMode.Tag:
            {
                var tag = vm.Editor.SelectedTagSuggestion;
                if (string.IsNullOrWhiteSpace(tag))
                    return false;

                var insert = $"#{tag}";
                ContentEditor.Document.Replace(_tokenStartOffset, caretOffset - _tokenStartOffset, insert);
                ContentEditor.CaretOffset = _tokenStartOffset + insert.Length;
                break;
            }
            default:
                return false;
        }

        vm.Editor.HideSuggestions();
        _suggestionMode = SuggestionMode.None;
        return true;
    }

    private void UpdateSuggestions()
    {
        if (ContentEditor?.Document is null)
            return;

        if (DataContext is not MainViewModel vm || vm.SelectedNote is null)
            return;

        // Prefer link context when nested situations exist
        if (TryGetLinkContext(ContentEditor, out var startOffset, out var partial))
        {
            _suggestionMode = SuggestionMode.Link;
            _tokenStartOffset = startOffset;

            vm.Editor.ShowTagSuggestions = false;
            vm.Editor.UpdateLinkSuggestions(partial);

            if (vm.Editor.ShowLinkSuggestions)
                PositionPopup(LinkSuggestionsPopup);

            return;
        }

        if (TryGetTagContext(ContentEditor, out startOffset, out partial))
        {
            _suggestionMode = SuggestionMode.Tag;
            _tokenStartOffset = startOffset;

            vm.Editor.ShowLinkSuggestions = false;
            vm.Editor.UpdateTagSuggestions(partial);

            if (vm.Editor.ShowTagSuggestions)
                PositionPopup(TagSuggestionsPopup);

            return;
        }

        vm.Editor.HideSuggestions();
        _suggestionMode = SuggestionMode.None;
    }

    private static bool TryGetLinkContext(TextEditor editor, out int tokenStartOffset, out string partial)
    {
        tokenStartOffset = -1;
        partial = string.Empty;

        var doc = editor.Document;
        var caretOffset = editor.CaretOffset;
        if (doc is null || caretOffset <= 0)
            return false;

        var before = doc.GetText(0, caretOffset);
        var open = before.LastIndexOf("[[", StringComparison.Ordinal);
        if (open < 0)
            return false;

        // Don't offer suggestions across newlines
        var segment = before.AsSpan(open, caretOffset - open);
        if (segment.Contains('\n') || segment.Contains('\r'))
            return false;

        // If there's a close before the caret, we're not inside a link
        if (before.IndexOf("]]", open + 2, StringComparison.Ordinal) >= 0)
            return false;

        var inner = before[(open + 2)..];

        // Support [[target]] and [[target|alias]].
        // Suggestions should keep working even after the user types '|', so we base matching
        // on the target portion only.
        var pipe = inner.IndexOf('|', StringComparison.Ordinal);
        var target = pipe >= 0 ? inner[..pipe] : inner;

        tokenStartOffset = open;
        partial = target.Trim();
        return true;
    }

    private static bool TryGetTagContext(TextEditor editor, out int tokenStartOffset, out string partial)
    {
        tokenStartOffset = -1;
        partial = string.Empty;

        var doc = editor.Document;
        var caretOffset = editor.CaretOffset;
        if (doc is null || caretOffset <= 0)
            return false;

        var start = caretOffset - 1;
        while (start >= 0)
        {
            var c = doc.GetCharAt(start);
            if (char.IsWhiteSpace(c))
                break;
            start--;
        }
        start++;

        if (start < 0 || start >= doc.TextLength)
            return false;

        if (doc.GetCharAt(start) != '#')
            return false;

        tokenStartOffset = start;
        partial = doc.GetText(start + 1, caretOffset - (start + 1));
        return true;
    }

    private void PositionPopup(System.Windows.Controls.Primitives.Popup popup)
    {
        if (ContentEditor is null)
            return;

        // Ensure visual lines are created so caret calculations work.
        ContentEditor.TextArea.TextView.EnsureVisualLines();

        var caretRect = ContentEditor.TextArea.Caret.CalculateCaretRectangle();
        var pt = ContentEditor.TextArea.TransformToAncestor(ContentEditor)
            .Transform(new Point(caretRect.X, caretRect.Bottom));

        popup.HorizontalOffset = pt.X;
        popup.VerticalOffset = pt.Y + 8;
    }

    private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedNote is not null)
        {
            vm.SelectNote(vm.SelectedNote);
        }
    }

    private void Backlinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: Note note } && DataContext is MainViewModel vm)
        {
            vm.SelectNote(note);
        }
    }
}

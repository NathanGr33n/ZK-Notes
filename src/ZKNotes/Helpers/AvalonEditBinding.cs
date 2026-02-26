using System;
using System.Windows;
using ICSharpCode.AvalonEdit;

namespace ZKNotes.Helpers;

/// <summary>
/// Enables binding AvalonEdit TextEditor.Text (which is not a dependency property).
/// </summary>
public static class AvalonEditBinding
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(AvalonEditBinding),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating",
        typeof(bool),
        typeof(AvalonEditBinding),
        new PropertyMetadata(false));

    private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextEditor editor)
            return;

        editor.TextChanged -= Editor_TextChanged;
        editor.TextChanged += Editor_TextChanged;

        if (GetIsUpdating(editor))
            return;

        var newText = e.NewValue as string ?? string.Empty;
        if (!string.Equals(editor.Text, newText, StringComparison.Ordinal))
        {
            SetIsUpdating(editor, true);
            editor.Text = newText;
            SetIsUpdating(editor, false);
        }
    }

    private static void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (sender is not TextEditor editor)
            return;

        if (GetIsUpdating(editor))
            return;

        SetIsUpdating(editor, true);
        SetText(editor, editor.Text);
        SetIsUpdating(editor, false);
    }
}

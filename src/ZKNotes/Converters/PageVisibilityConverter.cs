using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ZKNotes.ViewModels;

namespace ZKNotes.Converters;

/// <summary>
/// Converts a NavigationPage enum to Visibility.
/// ConverterParameter is the page name string to compare against.
/// </summary>
public sealed class PageVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NavigationPage currentPage &&
            parameter is string pageName &&
            Enum.TryParse<NavigationPage>(pageName, out var targetPage))
        {
            return currentPage == targetPage ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

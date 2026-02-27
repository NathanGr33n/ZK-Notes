using System;
using System.Windows;
using System.Windows.Media;
using ZKNotes.Models;
using ThemeMode = ZKNotes.Models.ThemeMode;

namespace ZKNotes.Services;

/// <summary>
/// Service for managing application theme switching.
/// </summary>
public class ThemeService
{
    private readonly AppConfig _config;
    private readonly LoggerService _logger;
    private ThemeMode _currentTheme;

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    public event EventHandler<ThemeMode>? ThemeChanged;

    /// <summary>
    /// Gets the current active theme.
    /// </summary>
    public ThemeMode CurrentTheme => _currentTheme;

    public ThemeService(AppConfig config, LoggerService logger)
    {
        _config = config;
        _logger = logger;
        _currentTheme = config.Theme;
    }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    public void ApplyTheme(ThemeMode theme)
    {
        _logger.Information("Applying theme: {Theme}", theme);

        try
        {
            var resources = Application.Current.Resources;

            if (theme == ThemeMode.Dark)
            {
                ApplyDarkTheme(resources);
            }
            else
            {
                ApplyLightTheme(resources);
            }

            _currentTheme = theme;
            _config.Theme = theme;
            _config.Save();

            ThemeChanged?.Invoke(this, theme);
            _logger.Information("Theme applied successfully: {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply theme: {Theme}", theme);
        }
    }

    /// <summary>
    /// Toggles between dark and light themes.
    /// </summary>
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        ApplyTheme(newTheme);
    }

    private void ApplyDarkTheme(ResourceDictionary resources)
    {
        // Background colors
        UpdateColor(resources, "ColorBackground", "#0F1115");
        UpdateColor(resources, "ColorSidebar", "#12141A");
        UpdateColor(resources, "ColorSurface", "#171A22");
        UpdateColor(resources, "ColorSurfaceHover", "#1E2230");
        UpdateColor(resources, "ColorSurfacePressed", "#24293A");
        UpdateColor(resources, "ColorSelected", "#241B2C");
        UpdateColor(resources, "ColorBorder", "#2B3240");

        // Foreground colors
        UpdateColor(resources, "ColorForeground", "#EDEFF5");
        UpdateColor(resources, "ColorForegroundMuted", "#A9B0BE");

        // Accent colors (same for both themes)
        UpdateColor(resources, "ColorAccent", "#9B59B6");
        UpdateColor(resources, "ColorAccentHover", "#7D3C98");
        UpdateColor(resources, "ColorDanger", "#E74C3C");
    }

    private void ApplyLightTheme(ResourceDictionary resources)
    {
        // Background colors
        UpdateColor(resources, "ColorBackground", "#FAFBFC");
        UpdateColor(resources, "ColorSidebar", "#F5F6F8");
        UpdateColor(resources, "ColorSurface", "#FFFFFF");
        UpdateColor(resources, "ColorSurfaceHover", "#F0F1F3");
        UpdateColor(resources, "ColorSurfacePressed", "#E8E9EB");
        UpdateColor(resources, "ColorSelected", "#EDE7F6");
        UpdateColor(resources, "ColorBorder", "#D1D5DB");

        // Foreground colors
        UpdateColor(resources, "ColorForeground", "#1A1D23");
        UpdateColor(resources, "ColorForegroundMuted", "#6B7280");

        // Accent colors (same for both themes)
        UpdateColor(resources, "ColorAccent", "#9B59B6");
        UpdateColor(resources, "ColorAccentHover", "#7D3C98");
        UpdateColor(resources, "ColorDanger", "#E74C3C");
    }

    private void UpdateColor(ResourceDictionary resources, string key, string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        resources[key] = color;

        // Also update the corresponding brush
        var brushKey = key.Replace("Color", "") + "Brush";
        if (resources.Contains(brushKey))
        {
            var brush = resources[brushKey] as SolidColorBrush;
            if (brush != null)
            {
                brush.Color = color;
            }
        }
    }
}

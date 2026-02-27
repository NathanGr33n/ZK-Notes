using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ZKNotes.Models;
using ZKNotes.Services;
using ZKNotes.ViewModels;

namespace ZKNotes;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private LoggerService? _logger;

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => _serviceProvider!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var config = AppConfig.Load();

        // Initialize logging
        var logsDir = Path.Combine(config.NotesDirectory, "_logs");
        _logger = new LoggerService(logsDir);
        _logger.Information("ZKNotes application starting");
        _logger.Information("Notes directory: {NotesDirectory}", config.NotesDirectory);

        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddSingleton(_logger);
        services.AddSingleton(new StorageService(config.NotesDirectory, _logger));
        services.AddSingleton(sp => new SearchService(config.NotesDirectory, sp.GetRequiredService<LoggerService>()));
        services.AddSingleton(sp => new TemplateService(config.NotesDirectory, config.TemplatesSubfolder));
        services.AddSingleton(sp => new BackupService(config.NotesDirectory, sp.GetRequiredService<LoggerService>()));
        services.AddSingleton<KnowledgeIndexService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<MainViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // Apply saved theme
        var themeService = _serviceProvider.GetRequiredService<ThemeService>();
        themeService.ApplyTheme(config.Theme);

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        try
        {
            await mainVm.InitializeAsync();
            _logger.Information("Application initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Failed to initialize application");
            MessageBox.Show($"Error loading notes: {ex.Message}\n\nPlease check the logs in {logsDir}",
                "ZK-Notes", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Information("Application shutting down");
        _serviceProvider?.Dispose();
        _logger?.Dispose();
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.Fatal(ex, "Unhandled exception in application domain");
            MessageBox.Show($"A fatal error occurred: {ex.Message}\n\nThe application will now close.",
                "ZK-Notes - Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error(e.Exception, "Unhandled exception in dispatcher");
        
        MessageBox.Show($"An error occurred: {e.Exception.Message}\n\nPlease try again.",
            "ZK-Notes - Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        
        // Mark as handled to prevent application crash
        e.Handled = true;
    }
}


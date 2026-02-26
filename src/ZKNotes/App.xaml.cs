using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ZKNotes.Models;
using ZKNotes.Services;
using ZKNotes.ViewModels;

namespace ZKNotes;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => _serviceProvider!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = AppConfig.Load();

        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddSingleton(new StorageService(config.NotesDirectory));
        services.AddSingleton(sp => new SearchService(config.NotesDirectory));
        services.AddSingleton<KnowledgeIndexService>();
        services.AddSingleton<MainViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();

        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();

        try
        {
            await mainVm.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading notes: {ex.Message}", "ZK-Notes",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}


using Microsoft.Extensions.DependencyInjection;
using Soundboard.Services;
using Soundboard.Services.Interfaces;
using Soundboard.ViewModels;
using System.Windows;

namespace Soundboard;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
        // Global exception handlers
        DispatcherUnhandledException += (s, e) =>
        {
            System.Windows.MessageBox.Show($"Unhandled exception:\n\n{e.Exception}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            System.Windows.MessageBox.Show($"Fatal exception:\n\n{e.ExceptionObject}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize services:\n\n{ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IDeviceEnumerator, DeviceEnumerator>();
        services.AddSingleton<ISoundLibrary, SoundLibrary>();
        services.AddSingleton<IAudioEngine, AudioEngine>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
    }

    public T GetService<T>() where T : class
    {
        return _serviceProvider!.GetRequiredService<T>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            // Initialize services that need early startup
            var configService = _serviceProvider!.GetRequiredService<IConfigService>();
            await configService.LoadAsync();

            // Create and show the main window
            var mainWindow = new Views.MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Startup failed:\n\n{ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}

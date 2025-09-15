using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkUsage.Services;

namespace NetworkUsage
{
    /// <summary>
    /// Main application class with dependency injection and service configuration
    /// Configures all services and initializes the application according to constitutional requirements
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        private IServiceProvider? _serviceProvider;
        private ILogger<App>? _logger;

        /// <summary>
        /// Application startup with dependency injection configuration
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Initialize logging first
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                
                var startupLogger = loggerFactory.CreateLogger<App>();
                startupLogger.LogInformation("Network Usage Monitor starting up...");

                // Build configuration
                var configuration = BuildConfiguration();
                
                // Build host with services
                _host = BuildHost(configuration);
                
                // Start the host
                await _host.StartAsync();
                
                // Get service provider and logger
                _serviceProvider = _host.Services;
                _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
                
                _logger.LogInformation("Application host started successfully");
                
                // Initialize and show main window
                await InitializeMainWindowAsync();
                
                _logger.LogInformation("Network Usage Monitor startup completed");
            }
            catch (Exception ex)
            {
                var errorLogger = _logger ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<App>();
                errorLogger.LogCritical(ex, "Fatal error during application startup");
                
                MessageBox.Show(
                    $"Fatal error during startup:\n{ex.Message}", 
                    "Network Usage Monitor - Startup Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                
                Shutdown(1);
            }
        }

        /// <summary>
        /// Application shutdown with proper cleanup
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("Network Usage Monitor shutting down...");
                
                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }
                
                _logger?.LogInformation("Application shutdown completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during application shutdown");
            }
            finally
            {
                base.OnExit(e);
            }
        }

        #region Configuration and Service Setup

        /// <summary>
        /// Build application configuration from files and environment
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", 
                    optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("NETUSAGE_");

            return builder.Build();
        }

        /// <summary>
        /// Build host with dependency injection container
        /// </summary>
        private static IHost BuildHost(IConfiguration configuration)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services, configuration);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                    
                    // Configure log levels
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddFilter("Microsoft", LogLevel.Warning);
                    logging.AddFilter("System", LogLevel.Warning);
                })
                .UseConsoleLifetime()
                .Build();
        }

        /// <summary>
        /// Configure all services in the dependency injection container
        /// Constitutional requirement: Library-first architecture with proper DI
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Core services (implementing the interfaces)
            services.AddSingleton<NetworkMonitorService>();
            services.AddSingleton<TaskbarIntegrationService>();
            services.AddSingleton<UIComponentsService>();
            
            // Register interfaces (for testing and future extensibility)
            services.AddSingleton<INetworkMonitor>(provider => provider.GetRequiredService<NetworkMonitorService>());
            services.AddSingleton<ITaskbarIntegration>(provider => provider.GetRequiredService<TaskbarIntegrationService>());
            services.AddSingleton<IUIComponents>(provider => provider.GetRequiredService<UIComponentsService>());
            
            // Main window (with DI support)
            services.AddTransient<MainWindow>();
            
            // Configuration
            services.Configure<DisplayConfiguration>(configuration.GetSection("Display"));
            
            // CLI services (for potential CLI mode)
            services.AddTransient<CLI.NetworkMonitorCLI>();
            services.AddTransient<CLI.TaskbarCLI>();
            services.AddTransient<CLI.UIComponentsCLI>();
            
            // Background services for monitoring
            services.AddHostedService<NetworkMonitoringBackgroundService>();
        }

        /// <summary>
        /// Initialize and show the main window
        /// </summary>
        private async Task InitializeMainWindowAsync()
        {
            try
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider not initialized");
                }

                _logger?.LogInformation("Creating main window...");

                // Create main window with DI
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                
                // Check if we should start minimized to tray
                var args = Environment.GetCommandLineArgs();
                bool startMinimized = args.Contains("--minimized") || args.Contains("--tray");
                
                if (startMinimized)
                {
                    _logger?.LogInformation("Starting minimized to system tray");
                    
                    // Don't show main window, just ensure tray icon is visible
                    var taskbarIntegration = _serviceProvider.GetRequiredService<TaskbarIntegrationService>();
                    await taskbarIntegration.ShowAsync();
                }
                else
                {
                    _logger?.LogInformation("Showing main window");
                    
                    // Show main window normally
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize main window");
                throw;
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handle unhandled exceptions
        /// </summary>
        private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                _logger?.LogCritical(e.Exception, "Unhandled exception occurred");
                
                MessageBox.Show(
                    $"An unexpected error occurred:\n{e.Exception.Message}\n\nThe application will attempt to continue.",
                    "Network Usage Monitor - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                e.Handled = true; // Prevent application crash
            }
            catch (Exception ex)
            {
                // Last resort error handling
                MessageBox.Show(
                    $"Critical error in error handler:\n{ex.Message}",
                    "Network Usage Monitor - Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle unhandled domain exceptions
        /// </summary>
        private void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    _logger?.LogCritical(exception, "Unhandled domain exception occurred");
                    
                    if (e.IsTerminating)
                    {
                        MessageBox.Show(
                            $"A fatal error occurred and the application must close:\n{exception.Message}",
                            "Network Usage Monitor - Fatal Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch
            {
                // Silent handling to prevent recursive errors
            }
        }

        #endregion

        #region CLI Mode Support

        /// <summary>
        /// Check if application should run in CLI mode
        /// </summary>
        private static bool IsCliMode(string[] args)
        {
            return args.Length > 0 && (
                args.Contains("--cli") ||
                args.Contains("network-monitor") ||
                args.Contains("taskbar") ||
                args.Contains("ui"));
        }

        /// <summary>
        /// Run in CLI mode if requested
        /// </summary>
        private static async Task<int> RunCliModeAsync(string[] args)
        {
            try
            {
                // Simple CLI mode for testing services
                Console.WriteLine("Network Usage Monitor CLI Mode");
                Console.WriteLine("Use --help for available commands");
                
                // This would be expanded to support full CLI operations
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CLI Error: {ex.Message}");
                return 1;
            }
        }

        #endregion

        #region Static Entry Point

        /// <summary>
        /// Application entry point with CLI mode support
        /// </summary>
        [STAThread]
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // Check for CLI mode
                if (IsCliMode(args))
                {
                    return await RunCliModeAsync(args);
                }

                // Run WPF application mode
                var app = new App();
                
                // Set up global exception handlers
                app.DispatcherUnhandledException += app.OnUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += app.OnUnhandledDomainException;
                
                return app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal startup error: {ex.Message}");
                return 1;
            }
        }

        #endregion
    }

    /// <summary>
    /// Background service for continuous network monitoring
    /// Ensures monitoring continues even when main window is hidden
    /// </summary>
    public class NetworkMonitoringBackgroundService : BackgroundService
    {
        private readonly NetworkMonitorService _networkMonitor;
        private readonly ILogger<NetworkMonitoringBackgroundService> _logger;

        public NetworkMonitoringBackgroundService(
            NetworkMonitorService networkMonitor,
            ILogger<NetworkMonitoringBackgroundService> logger)
        {
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Network monitoring background service started");
                
                // Keep the service running until cancellation
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Monitor service health
                    if (!_networkMonitor.IsMonitoring)
                    {
                        _logger.LogDebug("Network monitoring is not active, waiting...");
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Network monitoring background service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in network monitoring background service");
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;
using NetworkUsage.Services;

namespace NetworkUsage
{
    /// <summary>
    /// Main window for Network Usage Monitor application
    /// Integrates NetworkMonitor, TaskbarIntegration, and UIComponents services
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private readonly NetworkMonitorService _networkMonitor;
        private readonly TaskbarIntegrationService _taskbarIntegration;
        private readonly UIComponentsService _uiComponents;
        private readonly DisplayConfiguration _displayConfiguration;
        private DispatcherTimer? _updateTimer;
        private bool _isInitialized = false;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetRequiredService<ILogger<MainWindow>>();
            
            // Get services from DI container
            _networkMonitor = _serviceProvider.GetRequiredService<NetworkMonitorService>();
            _taskbarIntegration = _serviceProvider.GetRequiredService<TaskbarIntegrationService>();
            _uiComponents = _serviceProvider.GetRequiredService<UIComponentsService>();
            
            // Initialize configuration with defaults
            _displayConfiguration = new DisplayConfiguration();
            
            InitializeComponent();
            
            _logger.LogInformation("MainWindow initialized");
        }

        protected override async void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            if (!_isInitialized)
            {
                await InitializeServicesAsync();
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            await ShutdownServicesAsync();
            base.OnClosed(e);
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                _logger.LogInformation("Initializing application services...");

                // Initialize UI components
                await _uiComponents.InitializeAsync();
                
                // Set up event handlers
                SetupEventHandlers();
                
                // Apply initial theme
                await ApplyThemeFromConfiguration();
                
                // Initialize taskbar integration
                if (_displayConfiguration.ShowInSystemTray)
                {
                    await _taskbarIntegration.ShowAsync();
                }
                
                // Load and display available adapters
                await RefreshAdapterListAsync();
                
                // Start network monitoring
                await StartNetworkMonitoringAsync();
                
                // Set up UI update timer
                SetupUpdateTimer();
                
                _isInitialized = true;
                UpdateStatusBar("Application initialized successfully");
                
                _logger.LogInformation("Application services initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize application services");
                await _uiComponents.ShowErrorAsync("Failed to initialize application", ex);
            }
        }

        private void SetupEventHandlers()
        {
            // Network monitor events
            _networkMonitor.TrafficDataUpdated += OnTrafficDataUpdated;
            _networkMonitor.ActiveAdapterChanged += OnActiveAdapterChanged;
            
            // Taskbar integration events
            _taskbarIntegration.IconClicked += OnTrayIconClicked;
            _taskbarIntegration.IconHovered += OnTrayIconHovered;
            
            // UI components events
            _uiComponents.UserInteraction += OnUserInteraction;
            _uiComponents.StatisticsWindowClosed += OnStatisticsWindowClosed;
            
            // Main window UI events
            RefreshAdaptersButton.Click += OnRefreshAdaptersClicked;
            SetActiveAdapterButton.Click += OnSetActiveAdapterClicked;
            ApplyFormatButton.Click += OnApplyFormatClicked;
            
            // Settings change events
            UpdateIntervalComboBox.SelectionChanged += OnUpdateIntervalChanged;
            ThemeComboBox.SelectionChanged += OnThemeChanged;
            ShowInSystemTrayCheckBox.Checked += OnSystemTraySettingChanged;
            ShowInSystemTrayCheckBox.Unchecked += OnSystemTraySettingChanged;
        }

        private void SetupUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = _displayConfiguration.UpdateInterval
            };
            
            _updateTimer.Tick += async (sender, e) => await UpdateUIAsync();
            _updateTimer.Start();
        }

        private async void OnTrafficDataUpdated(object? sender, NetworkTrafficData trafficData)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateSpeedDisplays(trafficData);
                    UpdateLastUpdateTime();
                });

                if (_taskbarIntegration.IsVisible)
                {
                    await _taskbarIntegration.UpdateDisplayAsync(trafficData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling traffic data update");
            }
        }

        private async void OnActiveAdapterChanged(object? sender, NetworkAdapter adapter)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ActiveAdapterText.Text = $"{adapter.Name} ({adapter.Type})";
                    AdapterStatusText.Text = $"Status: {adapter.GetStatusDescription()}";
                    UpdateStatusBar($"Active adapter: {adapter.Name}");
                });

                _logger.LogInformation($"Active adapter changed to: {adapter.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling adapter change");
            }
        }

        private async void OnTrayIconClicked(object? sender, TrayIconClickEventArgs e)
        {
            try
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (IsVisible)
                    {
                        Hide();
                    }
                    else
                    {
                        Show();
                        Activate();
                    }
                }
                
                _logger.LogDebug($"Tray icon clicked: {e.Button}, {e.ClickCount} clicks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tray icon click");
            }
        }

        private void OnTrayIconHovered(object? sender, TrayIconHoverEventArgs e)
        {
            _logger.LogDebug($"Tray icon hovered: IsEntering={e.IsEntering}");
        }

        private void OnUserInteraction(object? sender, UIInteractionEventArgs e)
        {
            _logger.LogDebug($"User interaction: {e.InteractionType} on {e.ElementName}");
        }

        private void OnStatisticsWindowClosed(object? sender, StatisticsWindowEventArgs e)
        {
            _logger.LogInformation($"Statistics window closed: {e.WindowAction}");
        }

        private async void OnRefreshAdaptersClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("Refreshing adapters...");
                await RefreshAdapterListAsync();
                UpdateStatusBar("Adapters refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing adapters");
                await _uiComponents.ShowErrorAsync("Failed to refresh adapters", ex);
            }
        }

        private async void OnSetActiveAdapterClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = AdapterListView.SelectedItem;
                if (selectedItem is NetworkAdapter adapter)
                {
                    UpdateStatusBar($"Setting active adapter to {adapter.Name}...");
                    await _networkMonitor.SetActiveAdapterAsync(adapter.Id);
                    UpdateStatusBar($"Active adapter set to {adapter.Name}");
                }
                else
                {
                    await _uiComponents.ShowErrorAsync("Please select an adapter from the list first", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active adapter");
                await _uiComponents.ShowErrorAsync("Failed to set active adapter", ex);
            }
        }

        private async void OnApplyFormatClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var format = TooltipFormatTextBox.Text;
                if (string.IsNullOrWhiteSpace(format))
                {
                    await _uiComponents.ShowErrorAsync("Tooltip format cannot be empty", null);
                    return;
                }

                UpdateStatusBar("Applying new tooltip format...");
                await _taskbarIntegration.SetDisplayFormatAsync(format);
                UpdateStatusBar("Tooltip format applied");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying tooltip format");
                await _uiComponents.ShowErrorAsync("Invalid tooltip format", ex);
            }
        }

        private async void OnUpdateIntervalChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return; // Skip during initialization
                
                if (UpdateIntervalComboBox.SelectedItem is ComboBoxItem item && 
                    item.Tag is string tagValue && 
                    int.TryParse(tagValue, out var intervalMs))
                {
                    var interval = TimeSpan.FromMilliseconds(intervalMs);
                    _displayConfiguration.UpdateInterval = interval;
                    
                    await _networkMonitor.SetUpdateIntervalAsync(interval);
                    await _uiComponents.SetUIUpdateIntervalAsync(interval);
                    
                    if (_updateTimer != null)
                    {
                        _updateTimer.Interval = interval;
                    }
                    
                    UpdateStatusBar($"Update interval set to {intervalMs}ms");
                    _logger.LogInformation($"Update interval changed to {intervalMs}ms");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing update interval");
            }
        }

        private async void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return; // Skip during initialization
                
                if (ThemeComboBox.SelectedItem is ComboBoxItem item && 
                    item.Tag is string tagValue)
                {
                    var theme = EnumExtensions.ParseWindowsTheme(tagValue);
                    _displayConfiguration.CurrentTheme = theme;
                    
                    await _taskbarIntegration.ApplyThemeAsync(theme);
                    await _uiComponents.ApplyThemeAsync(theme);
                    
                    UpdateStatusBar($"Theme changed to {theme}");
                    _logger.LogInformation($"Theme changed to {theme}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing theme");
            }
        }

        private async void OnSystemTraySettingChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return; // Skip during initialization
                
                var showInTray = ShowInSystemTrayCheckBox.IsChecked == true;
                _displayConfiguration.ShowInSystemTray = showInTray;
                
                if (showInTray && !_taskbarIntegration.IsVisible)
                {
                    await _taskbarIntegration.ShowAsync();
                    UpdateStatusBar("System tray icon shown");
                }
                else if (!showInTray && _taskbarIntegration.IsVisible)
                {
                    await _taskbarIntegration.HideAsync();
                    UpdateStatusBar("System tray icon hidden");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing system tray setting");
            }
        }

        private async Task StartNetworkMonitoringAsync()
        {
            try
            {
                if (!_networkMonitor.IsMonitoring)
                {
                    await _networkMonitor.StartMonitoringAsync();
                    UpdateStatusBar("Network monitoring started");
                    
                    Dispatcher.Invoke(() =>
                    {
                        MonitoringStatusText.Text = "Monitoring: Active";
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start network monitoring");
                await _uiComponents.ShowErrorAsync("Failed to start network monitoring", ex);
            }
        }

        private async Task RefreshAdapterListAsync()
        {
            try
            {
                var adapters = await _networkMonitor.GetAvailableAdaptersAsync();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    AdapterListView.ItemsSource = adapters.ToList();
                    AdapterCountText.Text = $"Adapters: {adapters.Count()}";
                    
                    var activeAdapter = adapters.FirstOrDefault(a => a.IsActive);
                    if (activeAdapter != null)
                    {
                        ActiveAdapterText.Text = $"{activeAdapter.Name} ({activeAdapter.Type})";
                        AdapterStatusText.Text = $"Status: {activeAdapter.GetStatusDescription()}";
                    }
                });

                await _uiComponents.UpdateAdapterListAsync(adapters);
                
                _logger.LogDebug($"Adapter list refreshed: {adapters.Count()} adapters found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh adapter list");
                await _uiComponents.ShowErrorAsync("Failed to refresh network adapters", ex);
            }
        }

        private async Task ApplyThemeFromConfiguration()
        {
            try
            {
                var theme = _displayConfiguration.CurrentTheme;
                
                await _taskbarIntegration.ApplyThemeAsync(theme);
                await _uiComponents.ApplyThemeAsync(theme);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (ComboBoxItem item in ThemeComboBox.Items)
                    {
                        if (item.Tag?.ToString()?.Equals(theme.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                        {
                            ThemeComboBox.SelectedItem = item;
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme from configuration");
            }
        }

        private void UpdateSpeedDisplays(NetworkTrafficData trafficData)
        {
            try
            {
                var downloadSpeed = SpeedReading.FromBytesPerSecond(trafficData.ReceiveSpeed);
                var uploadSpeed = SpeedReading.FromBytesPerSecond(trafficData.SendSpeed);
                
                DownloadSpeedText.Text = downloadSpeed.FormattedString;
                UploadSpeedText.Text = uploadSpeed.FormattedString;
                
                var totalDownload = SpeedReading.FromBytesPerSecond(trafficData.BytesReceived);
                var totalUpload = SpeedReading.FromBytesPerSecond(trafficData.BytesSent);
                
                DownloadTotalText.Text = $"Total: {totalDownload.FormattedString.Replace("/s", "")}";
                UploadTotalText.Text = $"Total: {totalUpload.FormattedString.Replace("/s", "")}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating speed displays");
            }
        }

        private void UpdateLastUpdateTime()
        {
            LastUpdateText.Text = $"Last update: {DateTime.Now:HH:mm:ss}";
        }

        private void UpdateStatusBar(string message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                StatusBarText.Text = message;
            });
        }

        private async Task UpdateUIAsync()
        {
            try
            {
                var avgNetworkPerf = _networkMonitor.GetAveragePerformanceMs();
                var avgTaskbarPerf = _taskbarIntegration.GetAveragePerformanceMs();
                var avgUIPerf = _uiComponents.GetAveragePerformanceMs();
                
                var overallAvg = (avgNetworkPerf + avgTaskbarPerf + avgUIPerf) / 3;
                
                await Dispatcher.InvokeAsync(() =>
                {
                    AverageResponseTimeText.Text = $"{overallAvg:F1}ms";
                    MonitoringStatusText.Text = _networkMonitor.IsMonitoring ? 
                        "Monitoring: Active" : "Monitoring: Stopped";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during UI update");
            }
        }

        private async Task ShutdownServicesAsync()
        {
            try
            {
                _logger.LogInformation("Shutting down application services...");

                _updateTimer?.Stop();
                
                await _uiComponents.ShutdownAsync();
                await _taskbarIntegration.HideAsync();
                await _networkMonitor.StopMonitoringAsync();
                
                _logger.LogInformation("Application services shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during service shutdown");
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Minimized && _displayConfiguration.ShowInSystemTray)
                {
                    Hide();
                    
                    if (!_taskbarIntegration.IsVisible)
                    {
                        _ = _taskbarIntegration.ShowAsync();
                    }
                }
                
                base.OnStateChanged(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling window state change");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_displayConfiguration.ShowInSystemTray)
                {
                    e.Cancel = true;
                    Hide();
                    UpdateStatusBar("Application minimized to system tray");
                }
                else
                {
                    base.OnClosing(e);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling window closing");
                base.OnClosing(e);
            }
        }
    }
}
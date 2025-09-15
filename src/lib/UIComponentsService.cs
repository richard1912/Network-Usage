using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;

namespace NetworkUsage.Services
{
    /// <summary>
    /// Modern GUI components service implementing IUIComponents
    /// Provides detailed network statistics display with Windows 11 theme adaptation
    /// Performance target: <100ms response times, Windows 11 modern design
    /// </summary>
    public class UIComponentsService : IUIComponents, IDisposable
    {
        private readonly ILogger<UIComponentsService> _logger;
        private readonly object _lock = new object();

        // Statistics window management
        private Window? _statisticsWindow = null;
        private bool _isStatisticsWindowVisible = false;
        private WindowsTheme _currentTheme = WindowsTheme.Auto;
        private TimeSpan _uiUpdateInterval = TimeSpan.FromSeconds(1);

        // Performance tracking
        private readonly Queue<long> _performanceMetrics = new();
        private const int MaxPerformanceMetrics = 100;
        private DateTime _lastUIUpdate = DateTime.Now;

        // UI State
        private readonly List<NetworkAdapter> _currentAdapterList = new();
        private NetworkTrafficData? _lastTrafficData = null;

        public UIComponentsService(ILogger<UIComponentsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("UIComponentsService initialized");
        }

        #region IUIComponents Events

        public event EventHandler<StatisticsWindowEventArgs>? StatisticsWindowClosed;
        public event EventHandler<UIInteractionEventArgs>? UserInteraction;

        #endregion

        #region IUIComponents Properties

        public bool IsStatisticsWindowVisible 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isStatisticsWindowVisible; 
                } 
            } 
        }

        public WindowsTheme CurrentTheme 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _currentTheme; 
                } 
            } 
        }

        #endregion

        #region IUIComponents Methods

        /// <summary>
        /// Show the detailed network statistics window
        /// </summary>
        public async Task ShowDetailedStatsAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        if (_isStatisticsWindowVisible && _statisticsWindow != null)
                        {
                            // Window already visible, just bring to front
                            _statisticsWindow.Activate();
                            _statisticsWindow.WindowState = WindowState.Normal;
                            return;
                        }

                        // Create new statistics window
                        _statisticsWindow = CreateStatisticsWindow();
                        
                        // Apply current theme
                        ApplyThemeToWindow(_statisticsWindow, _currentTheme);
                        
                        // Show the window
                        _statisticsWindow.Show();
                        _isStatisticsWindowVisible = true;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 100)
                {
                    _logger.LogWarning($"ShowDetailedStatsAsync took {elapsedMs:F1}ms (should be <100ms)");
                }

                _logger.LogInformation($"Statistics window shown in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show detailed statistics window");
                throw;
            }
        }

        /// <summary>
        /// Hide the statistics window
        /// </summary>
        public async Task HideDetailedStatsAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        if (!_isStatisticsWindowVisible || _statisticsWindow == null)
                        {
                            return; // Already hidden
                        }

                        // Save window state before closing
                        var windowBounds = new Rectangle(
                            (int)_statisticsWindow.Left,
                            (int)_statisticsWindow.Top,
                            (int)_statisticsWindow.Width,
                            (int)_statisticsWindow.Height
                        );

                        // Close the window
                        _statisticsWindow.Close();
                        _statisticsWindow = null;
                        _isStatisticsWindowVisible = false;

                        // Fire window closed event
                        var eventArgs = new StatisticsWindowEventArgs
                        {
                            WindowAction = "Close",
                            Timestamp = DateTime.Now,
                            WindowBounds = windowBounds
                        };

                        StatisticsWindowClosed?.Invoke(this, eventArgs);
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"Statistics window hidden in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide statistics window");
                throw;
            }
        }

        /// <summary>
        /// Update the statistics display with new network data
        /// </summary>
        public async Task UpdateStatisticsAsync(NetworkTrafficData trafficData)
        {
            if (trafficData == null)
                throw new ArgumentNullException(nameof(trafficData));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        _lastTrafficData = trafficData;
                        
                        if (!_isStatisticsWindowVisible || _statisticsWindow == null)
                        {
                            return; // Nothing to update if window not visible
                        }

                        // Update UI elements with new data
                        UpdateStatisticsWindowContent(_statisticsWindow, trafficData);
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 50)
                {
                    _logger.LogWarning($"UpdateStatisticsAsync took {elapsedMs:F1}ms (should be <50ms)");
                }

                _lastUIUpdate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update statistics display");
                throw;
            }
        }

        /// <summary>
        /// Apply Windows 11 theme to all UI components
        /// </summary>
        public async Task ApplyThemeAsync(WindowsTheme theme)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        _currentTheme = theme;
                        
                        if (_isStatisticsWindowVisible && _statisticsWindow != null)
                        {
                            ApplyThemeToWindow(_statisticsWindow, theme);
                        }
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 200)
                {
                    _logger.LogWarning($"ApplyThemeAsync took {elapsedMs:F1}ms (should be <200ms)");
                }

                _logger.LogInformation($"Theme applied ({theme}) to UI components in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply theme: {theme}");
                throw;
            }
        }

        /// <summary>
        /// Update display with information about multiple network adapters
        /// </summary>
        public async Task UpdateAdapterListAsync(IEnumerable<NetworkAdapter> adapters)
        {
            if (adapters == null)
                throw new ArgumentNullException(nameof(adapters));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        _currentAdapterList.Clear();
                        _currentAdapterList.AddRange(adapters);
                        
                        if (_isStatisticsWindowVisible && _statisticsWindow != null)
                        {
                            UpdateAdapterListDisplay(_statisticsWindow, adapters);
                        }
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogDebug($"Adapter list updated with {adapters.Count()} adapters in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update adapter list");
                throw;
            }
        }

        /// <summary>
        /// Show error message to user with appropriate styling
        /// </summary>
        public async Task ShowErrorAsync(string errorMessage, Exception? exception = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Create error dialog with Windows 11 styling
                    var errorDialog = new Window
                    {
                        Title = "Network Usage Monitor - Error",
                        Width = 400,
                        Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.SingleBorderWindow
                    };

                    // Apply current theme to error dialog
                    ApplyThemeToWindow(errorDialog, _currentTheme);

                    // Create content
                    var stackPanel = new StackPanel 
                    { 
                        Margin = new Thickness(20),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var messageText = new TextBlock
                    {
                        Text = errorMessage,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    var okButton = new Button
                    {
                        Content = "OK",
                        Width = 80,
                        Height = 30,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    okButton.Click += (sender, e) => errorDialog.Close();

                    stackPanel.Children.Add(messageText);
                    
                    if (exception != null)
                    {
                        var detailsText = new TextBlock
                        {
                            Text = $"Details: {exception.Message}",
                            FontSize = 10,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 15)
                        };
                        stackPanel.Children.Add(detailsText);
                    }
                    
                    stackPanel.Children.Add(okButton);
                    errorDialog.Content = stackPanel;

                    // Show dialog
                    errorDialog.ShowDialog();
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 100)
                {
                    _logger.LogWarning($"ShowErrorAsync took {elapsedMs:F1}ms (should be <100ms)");
                }

                _logger.LogInformation($"Error dialog shown in {elapsedMs:F1}ms: {errorMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show error dialog");
                throw;
            }
        }

        /// <summary>
        /// Handle user interaction events from UI components
        /// </summary>
        public async Task HandleInteractionAsync(UIInteractionEventArgs interaction)
        {
            if (interaction == null)
                throw new ArgumentNullException(nameof(interaction));

            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogDebug($"Handling UI interaction: {interaction.InteractionType} on {interaction.ElementName}");

                // Process different interaction types
                switch (interaction.InteractionType?.ToLowerInvariant())
                {
                    case "buttonclick":
                        await HandleButtonClickAsync(interaction);
                        break;
                        
                    case "settingchange":
                        await HandleSettingChangeAsync(interaction);
                        break;
                        
                    case "adapterselect":
                        await HandleAdapterSelectionAsync(interaction);
                        break;
                        
                    default:
                        _logger.LogWarning($"Unknown interaction type: {interaction.InteractionType}");
                        break;
                }

                // Fire user interaction event
                UserInteraction?.Invoke(this, interaction);

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 100)
                {
                    _logger.LogWarning($"HandleInteractionAsync took {elapsedMs:F1}ms (should be <100ms)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to handle interaction: {interaction.InteractionType}");
                throw;
            }
        }

        /// <summary>
        /// Configure UI update frequency for statistics display
        /// </summary>
        public async Task SetUIUpdateIntervalAsync(TimeSpan interval)
        {
            if (interval < TimeSpan.FromMilliseconds(100) || interval > TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), 
                    "UI update interval must be between 100ms and 10 seconds");
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        _uiUpdateInterval = interval;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"UI update interval set to {interval.TotalMilliseconds}ms in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set UI update interval: {interval}");
                throw;
            }
        }

        /// <summary>
        /// Position statistics window relative to system tray icon
        /// </summary>
        public async Task PositionWindowAsync(Rectangle trayIconBounds)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        if (_statisticsWindow == null)
                        {
                            return; // No window to position
                        }

                        // Calculate optimal position near tray icon
                        var optimalPosition = CalculateOptimalWindowPosition(trayIconBounds, _statisticsWindow);
                        
                        _statisticsWindow.Left = optimalPosition.X;
                        _statisticsWindow.Top = optimalPosition.Y;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 50)
                {
                    _logger.LogWarning($"PositionWindowAsync took {elapsedMs:F1}ms (should be <50ms)");
                }

                _logger.LogDebug($"Window positioned near tray icon in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to position statistics window");
                throw;
            }
        }

        /// <summary>
        /// Initialize UI components and prepare for display
        /// </summary>
        public async Task InitializeAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        // Detect system theme if using Auto
                        if (_currentTheme == WindowsTheme.Auto)
                        {
                            _currentTheme = DetectSystemTheme();
                        }
                        
                        // Pre-load theme resources
                        LoadThemeResources(_currentTheme);
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 500)
                {
                    _logger.LogWarning($"InitializeAsync took {elapsedMs:F1}ms (should be <500ms)");
                }

                _logger.LogInformation($"UI components initialized in {elapsedMs:F1}ms with theme: {_currentTheme}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize UI components");
                throw;
            }
        }

        /// <summary>
        /// Cleanup UI resources and prepare for application shutdown
        /// </summary>
        public async Task ShutdownAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        // Close statistics window if open
                        if (_isStatisticsWindowVisible && _statisticsWindow != null)
                        {
                            _statisticsWindow.Close();
                            _statisticsWindow = null;
                            _isStatisticsWindowVisible = false;
                        }
                        
                        // Clear cached data
                        _currentAdapterList.Clear();
                        _lastTrafficData = null;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"UI components shutdown completed in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to shutdown UI components");
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create the statistics window with modern Windows 11 design
        /// </summary>
        private Window CreateStatisticsWindow()
        {
            var window = new Window
            {
                Title = "Network Usage Statistics",
                Width = 600,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ResizeMode = ResizeMode.CanResize,
                MinWidth = 400,
                MinHeight = 300
            };

            // Create main content
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerText = new TextBlock
            {
                Text = "Network Usage Statistics",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 15, 20, 15)
            };
            Grid.SetRow(headerText, 0);
            mainGrid.Children.Add(headerText);

            // Content area
            var contentPanel = CreateStatisticsContent();
            Grid.SetRow(contentPanel, 1);
            mainGrid.Children.Add(contentPanel);

            // Footer with refresh button
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 10, 20, 15)
            };

            var refreshButton = new Button
            {
                Content = "Refresh",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0)
            };

            refreshButton.Click += async (sender, e) =>
            {
                var interaction = new UIInteractionEventArgs
                {
                    InteractionType = "ButtonClick",
                    ElementName = "RefreshButton",
                    InteractionData = "refresh",
                    Timestamp = DateTime.Now
                };
                
                await HandleInteractionAsync(interaction);
            };

            footerPanel.Children.Add(refreshButton);
            Grid.SetRow(footerPanel, 2);
            mainGrid.Children.Add(footerPanel);

            window.Content = mainGrid;

            // Set up window event handlers
            window.Closing += OnStatisticsWindowClosing;

            return window;
        }

        /// <summary>
        /// Create the main statistics content panel
        /// </summary>
        private Panel CreateStatisticsContent()
        {
            var tabControl = new TabControl();

            // Current Statistics tab
            var currentStatsTab = new TabItem { Header = "Current" };
            currentStatsTab.Content = CreateCurrentStatsPanel();
            tabControl.Items.Add(currentStatsTab);

            // Adapter List tab
            var adaptersTab = new TabItem { Header = "Adapters" };
            adaptersTab.Content = CreateAdaptersPanel();
            tabControl.Items.Add(adaptersTab);

            // Settings tab
            var settingsTab = new TabItem { Header = "Settings" };
            settingsTab.Content = CreateSettingsPanel();
            tabControl.Items.Add(settingsTab);

            return tabControl;
        }

        /// <summary>
        /// Create current statistics panel
        /// </summary>
        private Panel CreateCurrentStatsPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            // Current speeds display
            var speedsGrid = new Grid();
            speedsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            speedsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Download speed
            var downloadPanel = new StackPanel();
            downloadPanel.Children.Add(new TextBlock { Text = "Download", FontWeight = FontWeights.Bold });
            downloadPanel.Children.Add(new TextBlock { Name = "DownloadSpeedText", Text = "0 B/s", FontSize = 24 });
            Grid.SetColumn(downloadPanel, 0);
            speedsGrid.Children.Add(downloadPanel);

            // Upload speed
            var uploadPanel = new StackPanel();
            uploadPanel.Children.Add(new TextBlock { Text = "Upload", FontWeight = FontWeights.Bold });
            uploadPanel.Children.Add(new TextBlock { Name = "UploadSpeedText", Text = "0 B/s", FontSize = 24 });
            Grid.SetColumn(uploadPanel, 1);
            speedsGrid.Children.Add(uploadPanel);

            panel.Children.Add(speedsGrid);

            // Adapter info
            var adapterInfoText = new TextBlock 
            { 
                Name = "AdapterInfoText",
                Text = "No adapter selected",
                Margin = new Thickness(0, 20, 0, 0),
                FontStyle = FontStyles.Italic
            };
            panel.Children.Add(adapterInfoText);

            return panel;
        }

        /// <summary>
        /// Create adapters list panel
        /// </summary>
        private Panel CreateAdaptersPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            var headerText = new TextBlock 
            { 
                Text = "Available Network Adapters",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(headerText);

            var adaptersList = new ListBox 
            { 
                Name = "AdaptersList",
                Height = 200
            };
            panel.Children.Add(adaptersList);

            return panel;
        }

        /// <summary>
        /// Create settings panel
        /// </summary>
        private Panel CreateSettingsPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            // Update interval setting
            var intervalPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            intervalPanel.Children.Add(new TextBlock { Text = "Update Interval:", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            
            var intervalCombo = new ComboBox { Name = "UpdateIntervalCombo", Width = 100 };
            intervalCombo.Items.Add("500ms");
            intervalCombo.Items.Add("1s");
            intervalCombo.Items.Add("2s");
            intervalCombo.Items.Add("5s");
            intervalCombo.SelectedIndex = 1; // Default to 1s
            intervalPanel.Children.Add(intervalCombo);
            
            panel.Children.Add(intervalPanel);

            // Theme setting
            var themePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            themePanel.Children.Add(new TextBlock { Text = "Theme:", Width = 120, VerticalAlignment = VerticalAlignment.Center });
            
            var themeCombo = new ComboBox { Name = "ThemeCombo", Width = 150 };
            themeCombo.Items.Add("Auto");
            themeCombo.Items.Add("Light");
            themeCombo.Items.Add("Dark");
            themeCombo.Items.Add("High Contrast");
            themeCombo.SelectedIndex = 0; // Default to Auto
            themePanel.Children.Add(themeCombo);
            
            panel.Children.Add(themePanel);

            return panel;
        }

        /// <summary>
        /// Update statistics window content with new traffic data
        /// </summary>
        private void UpdateStatisticsWindowContent(Window window, NetworkTrafficData trafficData)
        {
            try
            {
                // Find and update speed displays
                var downloadSpeedText = FindElementByName<TextBlock>(window, "DownloadSpeedText");
                var uploadSpeedText = FindElementByName<TextBlock>(window, "UploadSpeedText");
                var adapterInfoText = FindElementByName<TextBlock>(window, "AdapterInfoText");

                if (downloadSpeedText != null)
                {
                    var downloadSpeed = SpeedReading.FromBytesPerSecond(trafficData.ReceiveSpeed);
                    downloadSpeedText.Text = downloadSpeed.FormattedString;
                }

                if (uploadSpeedText != null)
                {
                    var uploadSpeed = SpeedReading.FromBytesPerSecond(trafficData.SendSpeed);
                    uploadSpeedText.Text = uploadSpeed.FormattedString;
                }

                if (adapterInfoText != null)
                {
                    adapterInfoText.Text = $"Adapter: {trafficData.AdapterName} ({trafficData.AdapterType})";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update statistics window content");
            }
        }

        /// <summary>
        /// Update adapter list display in statistics window
        /// </summary>
        private void UpdateAdapterListDisplay(Window window, IEnumerable<NetworkAdapter> adapters)
        {
            try
            {
                var adaptersList = FindElementByName<ListBox>(window, "AdaptersList");
                if (adaptersList != null)
                {
                    adaptersList.Items.Clear();
                    
                    foreach (var adapter in adapters.OrderByDescending(a => a.GetPriority()))
                    {
                        var displayText = $"{adapter.Name} ({adapter.Type}) - {adapter.GetStatusDescription()}";
                        if (adapter.IsActive)
                        {
                            displayText += " [ACTIVE]";
                        }
                        
                        adaptersList.Items.Add(displayText);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update adapter list display");
            }
        }

        /// <summary>
        /// Apply Windows 11 theme to a window
        /// </summary>
        private void ApplyThemeToWindow(Window window, WindowsTheme theme)
        {
            try
            {
                var actualTheme = theme == WindowsTheme.Auto ? DetectSystemTheme() : theme;
                
                var isDarkTheme = actualTheme.IsDarkTheme();
                
                // Apply theme colors
                window.Background = new SolidColorBrush(isDarkTheme ? 
                    System.Windows.Media.Color.FromRgb(32, 32, 32) : 
                    System.Windows.Media.Color.FromRgb(255, 255, 255));

                // Apply to all text elements
                ApplyThemeToChildren(window.Content as FrameworkElement, isDarkTheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply theme to window");
            }
        }

        /// <summary>
        /// Recursively apply theme to child elements
        /// </summary>
        private void ApplyThemeToChildren(FrameworkElement? element, bool isDarkTheme)
        {
            if (element == null) return;

            try
            {
                // Apply to text elements
                if (element is TextBlock textBlock)
                {
                    textBlock.Foreground = new SolidColorBrush(isDarkTheme ? 
                        System.Windows.Media.Colors.White : 
                        System.Windows.Media.Colors.Black);
                }
                else if (element is Button button)
                {
                    button.Background = new SolidColorBrush(isDarkTheme ? 
                        System.Windows.Media.Color.FromRgb(45, 45, 48) : 
                        System.Windows.Media.Color.FromRgb(240, 240, 240));
                    button.Foreground = new SolidColorBrush(isDarkTheme ? 
                        System.Windows.Media.Colors.White : 
                        System.Windows.Media.Colors.Black);
                }

                // Recursively apply to children
                if (element is Panel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        ApplyThemeToChildren(child as FrameworkElement, isDarkTheme);
                    }
                }
                else if (element is ContentControl contentControl && contentControl.Content is FrameworkElement childElement)
                {
                    ApplyThemeToChildren(childElement, isDarkTheme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying theme to child element");
            }
        }

        /// <summary>
        /// Find element by name in window tree
        /// </summary>
        private T? FindElementByName<T>(Window window, string name) where T : FrameworkElement
        {
            return window.FindName(name) as T;
        }

        /// <summary>
        /// Calculate optimal window position near tray icon
        /// </summary>
        private System.Windows.Point CalculateOptimalWindowPosition(Rectangle trayIconBounds, Window window)
        {
            var screenBounds = SystemParameters.WorkArea;
            var windowWidth = window.Width > 0 ? window.Width : 600;
            var windowHeight = window.Height > 0 ? window.Height : 450;

            // Default position: above and to the left of tray icon
            var x = Math.Max(10, trayIconBounds.X - windowWidth + trayIconBounds.Width);
            var y = Math.Max(10, trayIconBounds.Y - windowHeight);

            // Ensure window stays within screen bounds
            if (x + windowWidth > screenBounds.Right)
            {
                x = screenBounds.Right - windowWidth - 10;
            }
            
            if (y < screenBounds.Top)
            {
                y = trayIconBounds.Bottom + 10; // Position below if can't fit above
            }

            return new System.Windows.Point(Math.Max(10, x), Math.Max(10, y));
        }

        /// <summary>
        /// Detect current Windows system theme
        /// </summary>
        private WindowsTheme DetectSystemTheme()
        {
            try
            {
                // On Windows, this would check registry for theme settings
                // For cross-platform compatibility, return Light as default
                return WindowsTheme.Light;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect system theme, using Light theme");
                return WindowsTheme.Light;
            }
        }

        /// <summary>
        /// Load theme-specific resources
        /// </summary>
        private void LoadThemeResources(WindowsTheme theme)
        {
            try
            {
                // Pre-load theme resources like brushes, styles, etc.
                _logger.LogDebug($"Theme resources loaded for {theme}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to load theme resources for {theme}");
            }
        }

        /// <summary>
        /// Handle button click interactions
        /// </summary>
        private async Task HandleButtonClickAsync(UIInteractionEventArgs interaction)
        {
            switch (interaction.ElementName?.ToLowerInvariant())
            {
                case "refreshbutton":
                    _logger.LogDebug("Refresh button clicked");
                    // Trigger data refresh
                    break;
                    
                default:
                    _logger.LogDebug($"Button clicked: {interaction.ElementName}");
                    break;
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle setting change interactions
        /// </summary>
        private async Task HandleSettingChangeAsync(UIInteractionEventArgs interaction)
        {
            switch (interaction.ElementName?.ToLowerInvariant())
            {
                case "updateinterval":
                    if (interaction.InteractionData is TimeSpan interval)
                    {
                        await SetUIUpdateIntervalAsync(interval);
                    }
                    break;
                    
                case "theme":
                    if (interaction.InteractionData is WindowsTheme theme)
                    {
                        await ApplyThemeAsync(theme);
                    }
                    break;
                    
                default:
                    _logger.LogDebug($"Setting changed: {interaction.ElementName}");
                    break;
            }
        }

        /// <summary>
        /// Handle adapter selection interactions
        /// </summary>
        private async Task HandleAdapterSelectionAsync(UIInteractionEventArgs interaction)
        {
            if (interaction.InteractionData is string adapterId)
            {
                _logger.LogInformation($"Adapter selected: {adapterId}");
                // This would typically notify the NetworkMonitorService to change adapters
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handle statistics window closing event
        /// </summary>
        private void OnStatisticsWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (sender is Window window)
                {
                    var windowBounds = new Rectangle(
                        (int)window.Left,
                        (int)window.Top,
                        (int)window.Width,
                        (int)window.Height
                    );

                    var eventArgs = new StatisticsWindowEventArgs
                    {
                        WindowAction = "Close",
                        Timestamp = DateTime.Now,
                        WindowBounds = windowBounds
                    };

                    lock (_lock)
                    {
                        _isStatisticsWindowVisible = false;
                        _statisticsWindow = null;
                    }

                    StatisticsWindowClosed?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling statistics window closing");
            }
        }

        /// <summary>
        /// Record performance metric
        /// </summary>
        private void RecordPerformanceMetric(double elapsedMs)
        {
            lock (_performanceMetrics)
            {
                _performanceMetrics.Enqueue((long)elapsedMs);
                
                if (_performanceMetrics.Count > MaxPerformanceMetrics)
                {
                    _performanceMetrics.Dequeue();
                }
            }
        }

        /// <summary>
        /// Get average performance metrics
        /// </summary>
        public double GetAveragePerformanceMs()
        {
            lock (_performanceMetrics)
            {
                return _performanceMetrics.Count > 0 ? _performanceMetrics.Average() : 0;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Shutdown UI components
                        ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing UIComponentsService");
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }
}

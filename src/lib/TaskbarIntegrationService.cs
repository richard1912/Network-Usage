using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;

namespace NetworkUsage.Services
{
    /// <summary>
    /// Windows 11 taskbar/system tray integration service implementing ITaskbarIntegration
    /// Provides system tray icon management, tooltip updates, and user interaction handling
    /// Performance target: <100ms response times, Windows 11 theme compliance
    /// </summary>
    public class TaskbarIntegrationService : ITaskbarIntegration, IDisposable
    {
        private readonly ILogger<TaskbarIntegrationService> _logger;
        private readonly object _lock = new object();
        
        // System tray components
        private NotifyIcon? _notifyIcon = null;
        private ContextMenuStrip? _contextMenu = null;
        private Icon? _currentIcon = null;
        
        // State management
        private bool _isVisible = false;
        private string _currentTooltip = string.Empty;
        private string _displayFormat = "↓{0} ↑{1}";
        private WindowsTheme _currentTheme = WindowsTheme.Auto;
        
        // Icon resources for different themes
        private Icon? _lightThemeIcon = null;
        private Icon? _darkThemeIcon = null;
        private Icon? _highContrastIcon = null;

        // Performance tracking
        private readonly Queue<long> _performanceMetrics = new();
        private const int MaxPerformanceMetrics = 100;

        public TaskbarIntegrationService(ILogger<TaskbarIntegrationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("TaskbarIntegrationService initialized");
            
            // Initialize on UI thread
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(InitializeNotifyIcon);
            }
            else
            {
                // Fallback for non-WPF environments
                InitializeNotifyIcon();
            }
        }

        #region ITaskbarIntegration Events

        public event EventHandler<TrayIconClickEventArgs>? IconClicked;
        public event EventHandler<TrayIconHoverEventArgs>? IconHovered;

        #endregion

        #region ITaskbarIntegration Properties

        public bool IsVisible 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isVisible; 
                } 
            } 
        }

        public string CurrentTooltip 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _currentTooltip; 
                } 
            } 
        }

        #endregion

        #region ITaskbarIntegration Methods

        /// <summary>
        /// Initialize and show the system tray icon
        /// </summary>
        public async Task ShowAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (_isVisible)
                        {
                            _logger.LogDebug("System tray icon is already visible");
                            return;
                        }

                        if (_notifyIcon == null)
                        {
                            throw new InvalidOperationException("NotifyIcon not properly initialized");
                        }

                        // Apply current theme
                        ApplyCurrentThemeIcon();
                        
                        // Set initial tooltip
                        _notifyIcon.Text = string.IsNullOrEmpty(_currentTooltip) ? "Network Usage Monitor" : _currentTooltip;
                        
                        // Show the icon
                        _notifyIcon.Visible = true;
                        _isVisible = true;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 200)
                {
                    _logger.LogWarning($"ShowAsync took {elapsedMs:F1}ms (should be <200ms)");
                }

                _logger.LogInformation($"System tray icon shown successfully in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show system tray icon");
                throw;
            }
        }

        /// <summary>
        /// Hide the system tray icon and cleanup resources
        /// </summary>
        public async Task HideAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (!_isVisible)
                        {
                            _logger.LogDebug("System tray icon is already hidden");
                            return;
                        }

                        if (_notifyIcon != null)
                        {
                            _notifyIcon.Visible = false;
                        }
                        
                        _isVisible = false;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"System tray icon hidden successfully in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide system tray icon");
                throw;
            }
        }

        /// <summary>
        /// Update the system tray icon to reflect current network activity
        /// </summary>
        public async Task UpdateDisplayAsync(NetworkTrafficData trafficData)
        {
            if (trafficData == null)
                throw new ArgumentNullException(nameof(trafficData));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (!_isVisible || _notifyIcon == null)
                        {
                            return; // Nothing to update if not visible
                        }

                        // Format tooltip with current traffic data
                        var downloadSpeed = SpeedReading.FromBytesPerSecond(trafficData.ReceiveSpeed);
                        var uploadSpeed = SpeedReading.FromBytesPerSecond(trafficData.SendSpeed);
                        
                        var formattedTooltip = string.Format(_displayFormat, 
                            downloadSpeed.FormattedString, 
                            uploadSpeed.FormattedString);

                        // Update tooltip (max 127 characters for Windows compatibility)
                        if (formattedTooltip.Length > 127)
                        {
                            formattedTooltip = formattedTooltip.Substring(0, 124) + "...";
                        }

                        _notifyIcon.Text = formattedTooltip;
                        _currentTooltip = formattedTooltip;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 50)
                {
                    _logger.LogWarning($"UpdateDisplayAsync took {elapsedMs:F1}ms (should be <50ms)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update system tray display");
                throw;
            }
        }

        /// <summary>
        /// Update tooltip text shown on hover
        /// </summary>
        public async Task SetTooltipAsync(string tooltipText)
        {
            if (string.IsNullOrEmpty(tooltipText))
                throw new ArgumentException("Tooltip text cannot be null or empty", nameof(tooltipText));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        // Limit to 127 characters for Windows compatibility
                        var limitedText = tooltipText.Length > 127 ? tooltipText.Substring(0, 124) + "..." : tooltipText;
                        
                        if (_notifyIcon != null && _isVisible)
                        {
                            _notifyIcon.Text = limitedText;
                        }
                        
                        _currentTooltip = limitedText;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogDebug($"Tooltip updated to '{tooltipText}' in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set tooltip");
                throw;
            }
        }

        /// <summary>
        /// Change the system tray icon image
        /// </summary>
        public async Task SetIconAsync(byte[] iconData)
        {
            if (iconData == null || iconData.Length == 0)
                throw new ArgumentException("Icon data cannot be null or empty", nameof(iconData));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        // Create icon from byte array
                        using var iconStream = new System.IO.MemoryStream(iconData);
                        var newIcon = new Icon(iconStream);
                        
                        if (_notifyIcon != null)
                        {
                            var oldIcon = _notifyIcon.Icon;
                            _notifyIcon.Icon = newIcon;
                            oldIcon?.Dispose(); // Dispose old icon
                        }
                        
                        _currentIcon?.Dispose();
                        _currentIcon = newIcon;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogDebug($"System tray icon updated in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set system tray icon");
                throw;
            }
        }

        /// <summary>
        /// Apply Windows 11 theme styling to the tray integration
        /// </summary>
        public async Task ApplyThemeAsync(WindowsTheme theme)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        _currentTheme = theme;
                        
                        if (_notifyIcon != null && _isVisible)
                        {
                            ApplyCurrentThemeIcon();
                            
                            // Update context menu theme if it exists
                            if (_contextMenu != null)
                            {
                                ApplyThemeToContextMenu(theme);
                            }
                        }
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 200)
                {
                    _logger.LogWarning($"ApplyThemeAsync took {elapsedMs:F1}ms (should be <200ms)");
                }

                _logger.LogInformation($"Theme applied ({theme}) in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply theme: {theme}");
                throw;
            }
        }

        /// <summary>
        /// Show context menu when user right-clicks tray icon
        /// </summary>
        public async Task ShowContextMenuAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (_notifyIcon == null || !_isVisible)
                        {
                            return; // Can't show menu if icon not visible
                        }

                        if (_contextMenu == null)
                        {
                            CreateContextMenu();
                        }

                        // The context menu will be shown automatically by NotifyIcon
                        // when right-clicked due to the ContextMenuStrip assignment
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 100)
                {
                    _logger.LogWarning($"ShowContextMenuAsync took {elapsedMs:F1}ms (should be <100ms)");
                }

                _logger.LogDebug($"Context menu prepared in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show context menu");
                throw;
            }
        }

        /// <summary>
        /// Configure display format for network speeds in tooltip
        /// </summary>
        public async Task SetDisplayFormatAsync(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Display format cannot be null or empty", nameof(format));

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Validate format string has exactly 2 placeholders
                if (!format.Contains("{0}") || !format.Contains("{1}"))
                {
                    throw new ArgumentException("Display format must contain exactly {0} and {1} placeholders", nameof(format));
                }

                // Test format with sample values
                try
                {
                    string.Format(format, "1.5 MB/s", "750 KB/s");
                }
                catch (FormatException ex)
                {
                    throw new ArgumentException($"Invalid format string: {ex.Message}", nameof(format));
                }

                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        _displayFormat = format;
                    }
                });

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"Display format updated to '{format}' in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set display format: {format}");
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the NotifyIcon component
        /// </summary>
        private void InitializeNotifyIcon()
        {
            try
            {
                lock (_lock)
                {
                    if (_notifyIcon != null)
                    {
                        return; // Already initialized
                    }

                    _notifyIcon = new NotifyIcon();
                    
                    // Set up event handlers
                    _notifyIcon.MouseClick += OnNotifyIconMouseClick;
                    _notifyIcon.MouseMove += OnNotifyIconMouseMove;
                    
                    // Initialize with default icon
                    CreateDefaultIcons();
                    ApplyCurrentThemeIcon();
                    
                    // Create context menu
                    CreateContextMenu();
                    _notifyIcon.ContextMenuStrip = _contextMenu;
                    
                    _currentTooltip = "Network Usage Monitor";
                    _notifyIcon.Text = _currentTooltip;
                }

                _logger.LogDebug("NotifyIcon initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize NotifyIcon");
                throw;
            }
        }

        /// <summary>
        /// Create default icons for different themes
        /// </summary>
        private void CreateDefaultIcons()
        {
            try
            {
                // Create simple colored icons for different themes
                // In a real implementation, these would be loaded from resource files
                
                _lightThemeIcon = CreateSimpleIcon(Color.Black); // Dark icon for light theme
                _darkThemeIcon = CreateSimpleIcon(Color.White);  // Light icon for dark theme
                _highContrastIcon = CreateSimpleIcon(Color.Yellow); // High contrast color
                
                _currentIcon = _lightThemeIcon; // Default
                
                _logger.LogDebug("Default theme icons created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create default icons");
                throw;
            }
        }

        /// <summary>
        /// Create a simple icon with the specified color
        /// </summary>
        private static Icon CreateSimpleIcon(Color color)
        {
            // Create a 16x16 bitmap for the system tray icon
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Fill with transparent background
            graphics.Clear(Color.Transparent);
            
            // Draw a simple network activity indicator
            using var brush = new SolidBrush(color);
            using var pen = new Pen(color, 1);
            
            // Draw up/down arrows to represent network activity
            // Up arrow (upload)
            var upArrowPoints = new Point[]
            {
                new Point(4, 2),  // Top
                new Point(2, 4),  // Bottom left
                new Point(6, 4)   // Bottom right
            };
            graphics.FillPolygon(brush, upArrowPoints);
            
            // Down arrow (download)
            var downArrowPoints = new Point[]
            {
                new Point(12, 14), // Bottom
                new Point(10, 12), // Top left
                new Point(14, 12)  // Top right
            };
            graphics.FillPolygon(brush, downArrowPoints);
            
            // Convert bitmap to icon
            var iconHandle = bitmap.GetHicon();
            return Icon.FromHandle(iconHandle);
        }

        /// <summary>
        /// Apply icon based on current theme
        /// </summary>
        private void ApplyCurrentThemeIcon()
        {
            if (_notifyIcon == null) return;

            try
            {
                var themeToApply = _currentTheme;
                
                // Auto-detect system theme if needed
                if (themeToApply == WindowsTheme.Auto)
                {
                    themeToApply = DetectSystemTheme();
                }

                var iconToUse = themeToApply switch
                {
                    WindowsTheme.Light => _lightThemeIcon,
                    WindowsTheme.Dark => _darkThemeIcon,
                    WindowsTheme.HighContrast => _highContrastIcon,
                    _ => _lightThemeIcon
                };

                if (iconToUse != null)
                {
                    _notifyIcon.Icon = iconToUse;
                    _currentIcon = iconToUse;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme icon");
                // Continue with current icon rather than failing
            }
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
                
                // In a real Windows implementation:
                // var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                // var lightThemeValue = key?.GetValue("AppsUseLightTheme");
                // return lightThemeValue?.ToString() == "1" ? WindowsTheme.Light : WindowsTheme.Dark;
                
                return WindowsTheme.Light; // Default for non-Windows environments
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect system theme, using Light theme as fallback");
                return WindowsTheme.Light;
            }
        }

        /// <summary>
        /// Create context menu for tray icon
        /// </summary>
        private void CreateContextMenu()
        {
            try
            {
                _contextMenu = new ContextMenuStrip();
                
                // Show Statistics option
                var showStatsItem = new ToolStripMenuItem("Show Statistics");
                showStatsItem.Click += (sender, e) => OnShowStatisticsClicked();
                _contextMenu.Items.Add(showStatsItem);
                
                // Separator
                _contextMenu.Items.Add(new ToolStripSeparator());
                
                // Settings option
                var settingsItem = new ToolStripMenuItem("Settings");
                settingsItem.Click += (sender, e) => OnSettingsClicked();
                _contextMenu.Items.Add(settingsItem);
                
                // Separator
                _contextMenu.Items.Add(new ToolStripSeparator());
                
                // Exit option
                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (sender, e) => OnExitClicked();
                _contextMenu.Items.Add(exitItem);
                
                // Apply current theme to menu
                ApplyThemeToContextMenu(_currentTheme);
                
                _logger.LogDebug("Context menu created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create context menu");
                throw;
            }
        }

        /// <summary>
        /// Apply theme styling to context menu
        /// </summary>
        private void ApplyThemeToContextMenu(WindowsTheme theme)
        {
            if (_contextMenu == null) return;

            try
            {
                var isDarkTheme = theme.IsDarkTheme();
                
                // Apply theme colors
                _contextMenu.BackColor = isDarkTheme ? Color.FromArgb(45, 45, 48) : Color.White;
                _contextMenu.ForeColor = isDarkTheme ? Color.White : Color.Black;
                
                // Apply to menu items
                foreach (ToolStripItem item in _contextMenu.Items)
                {
                    if (item is ToolStripMenuItem menuItem)
                    {
                        menuItem.BackColor = _contextMenu.BackColor;
                        menuItem.ForeColor = _contextMenu.ForeColor;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply theme to context menu");
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

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle NotifyIcon mouse click events
        /// </summary>
        private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
        {
            try
            {
                var clickEventArgs = new TrayIconClickEventArgs
                {
                    Button = e.Button,
                    ClickCount = e.Clicks,
                    Timestamp = DateTime.Now
                };

                _logger.LogDebug($"Tray icon clicked: {e.Button}, {e.Clicks} clicks");
                IconClicked?.Invoke(this, clickEventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tray icon click");
            }
        }

        /// <summary>
        /// Handle NotifyIcon mouse move events (hover)
        /// </summary>
        private void OnNotifyIconMouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                var hoverEventArgs = new TrayIconHoverEventArgs
                {
                    IsEntering = true, // Simplified - in real implementation would track enter/leave
                    Timestamp = DateTime.Now
                };

                IconHovered?.Invoke(this, hoverEventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tray icon hover");
            }
        }

        /// <summary>
        /// Handle Show Statistics menu item click
        /// </summary>
        private void OnShowStatisticsClicked()
        {
            try
            {
                _logger.LogInformation("Show Statistics menu item clicked");
                // In real implementation, this would trigger showing the statistics window
                // This would typically be handled by the main application or UI service
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Show Statistics click");
            }
        }

        /// <summary>
        /// Handle Settings menu item click
        /// </summary>
        private void OnSettingsClicked()
        {
            try
            {
                _logger.LogInformation("Settings menu item clicked");
                // In real implementation, this would show settings dialog
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Settings click");
            }
        }

        /// <summary>
        /// Handle Exit menu item click
        /// </summary>
        private void OnExitClicked()
        {
            try
            {
                _logger.LogInformation("Exit menu item clicked");
                // In real implementation, this would trigger application shutdown
                // For now, just hide the tray icon
                _ = HideAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Exit click");
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
                        // Hide icon if visible
                        if (_isVisible)
                        {
                            HideAsync().Wait(TimeSpan.FromSeconds(2));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during disposal");
                    }
                    finally
                    {
                        // Dispose resources
                        _notifyIcon?.Dispose();
                        _contextMenu?.Dispose();
                        _currentIcon?.Dispose();
                        _lightThemeIcon?.Dispose();
                        _darkThemeIcon?.Dispose();
                        _highContrastIcon?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }
}

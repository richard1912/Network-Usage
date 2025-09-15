using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NetworkUsage.Contracts;

namespace NetworkUsage.Services
{
    /// <summary>
    /// Windows theme detection and automatic switching service
    /// Monitors Windows 11 system theme changes and notifies all components
    /// Provides: Registry monitoring, WinAPI integration, automatic theme switching
    /// </summary>
    public class WindowsThemeDetectionService : BackgroundService, IDisposable
    {
        private readonly ILogger<WindowsThemeDetectionService> _logger;
        private readonly TaskbarIntegrationService _taskbarIntegration;
        private readonly UIComponentsService _uiComponents;
        
        // Theme detection state
        private WindowsTheme _currentDetectedTheme = WindowsTheme.Light;
        private bool _isAutoThemeEnabled = true;
        private readonly object _themeLock = new object();
        
        // Windows registry monitoring
        private RegistryKey? _personalizeKey = null;
        private readonly ManualResetEventSlim _registryChangeEvent = new ManualResetEventSlim(false);
        
        // Performance tracking
        private DateTime _lastThemeChange = DateTime.Now;
        private int _themeChangeCount = 0;
        private readonly TimeSpan _themeChangeDebounce = TimeSpan.FromSeconds(2);

        public WindowsThemeDetectionService(
            TaskbarIntegrationService taskbarIntegration,
            UIComponentsService uiComponents,
            ILogger<WindowsThemeDetectionService> logger)
        {
            _taskbarIntegration = taskbarIntegration ?? throw new ArgumentNullException(nameof(taskbarIntegration));
            _uiComponents = uiComponents ?? throw new ArgumentNullException(nameof(uiComponents));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogInformation("Windows theme detection service initialized");
        }

        #region Theme Detection Events

        /// <summary>
        /// Event fired when Windows system theme changes
        /// </summary>
        public event EventHandler<WindowsThemeChangedEventArgs>? ThemeChanged;

        #endregion

        #region BackgroundService Implementation

        /// <summary>
        /// Main execution loop for theme detection
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Windows theme detection service started");
                
                // Initialize theme detection
                await InitializeThemeDetectionAsync();
                
                // Main monitoring loop
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await MonitorThemeChangesAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Expected when cancellation is requested
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during theme monitoring, retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Windows theme detection service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Windows theme detection service");
            }
        }

        #endregion

        #region Theme Detection Methods

        /// <summary>
        /// Initialize Windows theme detection
        /// </summary>
        private async Task InitializeThemeDetectionAsync()
        {
            try
            {
                // Detect initial theme
                var initialTheme = await DetectCurrentThemeAsync();
                
                lock (_themeLock)
                {
                    _currentDetectedTheme = initialTheme;
                }
                
                // Set up registry monitoring for Windows
                SetupRegistryMonitoring();
                
                _logger.LogInformation($"Theme detection initialized, current theme: {initialTheme}");
                
                // Apply initial theme if auto-theme is enabled
                if (_isAutoThemeEnabled)
                {
                    await ApplyDetectedThemeAsync(initialTheme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme detection");
                throw;
            }
        }

        /// <summary>
        /// Monitor for theme changes
        /// </summary>
        private async Task MonitorThemeChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Check for theme changes every 2 seconds
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                
                var detectedTheme = await DetectCurrentThemeAsync();
                
                bool themeChanged = false;
                WindowsTheme previousTheme;
                
                lock (_themeLock)
                {
                    previousTheme = _currentDetectedTheme;
                    themeChanged = _currentDetectedTheme != detectedTheme;
                    
                    if (themeChanged)
                    {
                        _currentDetectedTheme = detectedTheme;
                        _lastThemeChange = DateTime.Now;
                        _themeChangeCount++;
                    }
                }

                if (themeChanged)
                {
                    _logger.LogInformation($"Theme change detected: {previousTheme} → {detectedTheme}");
                    
                    // Debounce rapid theme changes
                    if (DateTime.Now - _lastThemeChange < _themeChangeDebounce)
                    {
                        _logger.LogDebug("Theme change debounced, waiting for stability...");
                        await Task.Delay(_themeChangeDebounce, cancellationToken);
                    }
                    
                    // Fire theme changed event
                    var eventArgs = new WindowsThemeChangedEventArgs
                    {
                        PreviousTheme = previousTheme,
                        NewTheme = detectedTheme,
                        ChangeTimestamp = DateTime.Now,
                        IsAutomatic = true
                    };
                    
                    ThemeChanged?.Invoke(this, eventArgs);
                    
                    // Apply theme automatically if enabled
                    if (_isAutoThemeEnabled)
                    {
                        await ApplyDetectedThemeAsync(detectedTheme);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring theme changes");
            }
        }

        /// <summary>
        /// Detect current Windows theme
        /// </summary>
        public async Task<WindowsTheme> DetectCurrentThemeAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    // Cross-platform theme detection simulation
                    // In real Windows implementation, this would use:
                    
                    // 1. Registry check for light/dark theme
                    // var personalizeKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                    // var appsUseLightTheme = personalizeKey?.GetValue("AppsUseLightTheme");
                    // var systemUsesLightTheme = personalizeKey?.GetValue("SystemUsesLightTheme");
                    
                    // 2. High contrast detection
                    // var highContrast = SystemInformation.HighContrast;
                    
                    // 3. WinAPI calls
                    // var dwmColors = GetDwmColorizationColor();
                    
                    // For demo/cross-platform, simulate based on time
                    return SimulateWindowsThemeDetection();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect Windows theme");
                return WindowsTheme.Light; // Safe fallback
            }
        }

        /// <summary>
        /// Simulate Windows theme detection for cross-platform development
        /// </summary>
        private WindowsTheme SimulateWindowsThemeDetection()
        {
            try
            {
                // Simulate realistic theme detection based on various factors
                var hour = DateTime.Now.Hour;
                var minute = DateTime.Now.Minute;
                
                // Simulate high contrast mode during certain times (for testing)
                if (minute >= 50 && minute < 55)
                {
                    return WindowsTheme.HighContrast;
                }
                
                // Simulate dark theme during evening/night hours
                if (hour >= 20 || hour < 6)
                {
                    return WindowsTheme.Dark;
                }
                
                // Light theme during day hours
                return WindowsTheme.Light;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in simulated theme detection");
                return WindowsTheme.Light;
            }
        }

        /// <summary>
        /// Set up Windows registry monitoring for theme changes
        /// </summary>
        private void SetupRegistryMonitoring()
        {
            try
            {
                // In real Windows implementation, this would monitor:
                // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
                
                // For cross-platform compatibility, we use timer-based checking instead
                _logger.LogDebug("Registry monitoring simulated (cross-platform mode)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set up registry monitoring, using fallback timer method");
            }
        }

        /// <summary>
        /// Apply detected theme to all components
        /// </summary>
        private async Task ApplyDetectedThemeAsync(WindowsTheme theme)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Apply theme to taskbar integration
                await _taskbarIntegration.ApplyThemeAsync(theme);
                
                // Apply theme to UI components
                await _uiComponents.ApplyThemeAsync(theme);
                
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogInformation($"Auto-applied theme {theme} to all components in {elapsedMs:F1}ms");
                
                // Performance warning if theme change takes too long
                if (elapsedMs > 500)
                {
                    _logger.LogWarning($"Theme application took {elapsedMs:F1}ms (should be <500ms)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply detected theme: {theme}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get currently detected system theme
        /// </summary>
        public WindowsTheme GetCurrentSystemTheme()
        {
            lock (_themeLock)
            {
                return _currentDetectedTheme;
            }
        }

        /// <summary>
        /// Enable or disable automatic theme switching
        /// </summary>
        public async Task SetAutoThemeEnabledAsync(bool enabled)
        {
            try
            {
                lock (_themeLock)
                {
                    if (_isAutoThemeEnabled == enabled)
                        return; // No change
                    
                    _isAutoThemeEnabled = enabled;
                }

                if (enabled)
                {
                    // Apply current detected theme immediately
                    var currentTheme = GetCurrentSystemTheme();
                    await ApplyDetectedThemeAsync(currentTheme);
                    _logger.LogInformation("Automatic theme switching enabled");
                }
                else
                {
                    _logger.LogInformation("Automatic theme switching disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set auto-theme enabled state");
                throw;
            }
        }

        /// <summary>
        /// Check if automatic theme switching is enabled
        /// </summary>
        public bool IsAutoThemeEnabled()
        {
            lock (_themeLock)
            {
                return _isAutoThemeEnabled;
            }
        }

        /// <summary>
        /// Force immediate theme detection and application
        /// </summary>
        public async Task RefreshThemeAsync()
        {
            try
            {
                var detectedTheme = await DetectCurrentThemeAsync();
                
                lock (_themeLock)
                {
                    _currentDetectedTheme = detectedTheme;
                }
                
                if (_isAutoThemeEnabled)
                {
                    await ApplyDetectedThemeAsync(detectedTheme);
                }
                
                _logger.LogInformation($"Theme refreshed: {detectedTheme}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh theme");
                throw;
            }
        }

        /// <summary>
        /// Get theme detection statistics
        /// </summary>
        public ThemeDetectionStatistics GetStatistics()
        {
            lock (_themeLock)
            {
                return new ThemeDetectionStatistics
                {
                    CurrentDetectedTheme = _currentDetectedTheme,
                    IsAutoThemeEnabled = _isAutoThemeEnabled,
                    LastThemeChange = _lastThemeChange,
                    ThemeChangeCount = _themeChangeCount,
                    UptimeHours = (DateTime.Now - _lastThemeChange).TotalHours
                };
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _personalizeKey?.Close();
                        _personalizeKey?.Dispose();
                        _registryChangeEvent?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing WindowsThemeDetectionService");
                    }
                }
                
                base.Dispose(disposing);
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for theme change notifications
    /// </summary>
    public class WindowsThemeChangedEventArgs : EventArgs
    {
        public WindowsTheme PreviousTheme { get; set; }
        public WindowsTheme NewTheme { get; set; }
        public DateTime ChangeTimestamp { get; set; }
        public bool IsAutomatic { get; set; }
        public string? ChangeReason { get; set; }
    }

    /// <summary>
    /// Statistics for theme detection monitoring
    /// </summary>
    public class ThemeDetectionStatistics
    {
        public WindowsTheme CurrentDetectedTheme { get; set; }
        public bool IsAutoThemeEnabled { get; set; }
        public DateTime LastThemeChange { get; set; }
        public int ThemeChangeCount { get; set; }
        public double UptimeHours { get; set; }
    }

    /// <summary>
    /// Windows API helpers for theme detection
    /// Note: These are placeholders for cross-platform compatibility
    /// Real Windows implementation would use actual WinAPI calls
    /// </summary>
    public static class WindowsThemeAPI
    {
        #region Windows API Constants (Placeholders)

        // Registry paths for Windows 11 theme settings
        public const string PersonalizationKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        public const string AppsUseLightThemeValue = "AppsUseLightTheme";
        public const string SystemUsesLightThemeValue = "SystemUsesLightTheme";
        public const string EnableTransparencyValue = "EnableTransparency";

        #endregion

        #region Theme Detection Methods

        /// <summary>
        /// Check if Windows is using light theme for applications
        /// </summary>
        public static bool IsLightThemeForApps()
        {
            try
            {
                // Cross-platform simulation
                // Real implementation: Registry.CurrentUser.OpenSubKey(PersonalizationKeyPath)?.GetValue(AppsUseLightThemeValue)
                return DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 18;
            }
            catch
            {
                return true; // Default to light theme
            }
        }

        /// <summary>
        /// Check if Windows is using light theme for system
        /// </summary>
        public static bool IsLightThemeForSystem()
        {
            try
            {
                // Cross-platform simulation  
                // Real implementation: Registry.CurrentUser.OpenSubKey(PersonalizationKeyPath)?.GetValue(SystemUsesLightThemeValue)
                return DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 18;
            }
            catch
            {
                return true; // Default to light theme
            }
        }

        /// <summary>
        /// Check if high contrast mode is enabled
        /// </summary>
        public static bool IsHighContrastEnabled()
        {
            try
            {
                // Cross-platform simulation
                // Real implementation: SystemInformation.HighContrast
                return DateTime.Now.Minute >= 50 && DateTime.Now.Minute < 55; // 5-minute window for testing
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detect comprehensive Windows theme state
        /// </summary>
        public static WindowsTheme DetectWindowsTheme()
        {
            try
            {
                // Check high contrast first (highest priority)
                if (IsHighContrastEnabled())
                {
                    return WindowsTheme.HighContrast;
                }
                
                // Check app theme preference
                var appsUseLight = IsLightThemeForApps();
                return appsUseLight ? WindowsTheme.Light : WindowsTheme.Dark;
            }
            catch (Exception)
            {
                return WindowsTheme.Light; // Safe fallback
            }
        }

        /// <summary>
        /// Get Windows 11 accent color
        /// </summary>
        public static Color GetAccentColor()
        {
            try
            {
                // Cross-platform simulation
                // Real implementation would use:
                // DwmGetColorizationColor() or Registry accent color values
                
                return Color.FromArgb(0, 120, 215); // Default Windows 11 blue
            }
            catch
            {
                return Color.FromArgb(0, 120, 215); // Fallback to Windows 11 blue
            }
        }

        /// <summary>
        /// Check if transparency effects are enabled
        /// </summary>
        public static bool IsTransparencyEnabled()
        {
            try
            {
                // Cross-platform simulation
                // Real implementation: Registry.CurrentUser.OpenSubKey(PersonalizationKeyPath)?.GetValue(EnableTransparencyValue)
                return true; // Assume enabled for modern Windows 11
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Registry Monitoring (Placeholder)

        /// <summary>
        /// Set up registry change monitoring
        /// Note: Placeholder for cross-platform compatibility
        /// </summary>
        public static void SetupRegistryMonitoring(Action<WindowsTheme> onThemeChanged)
        {
            try
            {
                // Real Windows implementation would use:
                // Microsoft.Win32.RegistryKey.OpenSubKey with change notifications
                // or WMI events for registry monitoring
                
                // For cross-platform, this is a no-op
            }
            catch (Exception)
            {
                // Ignore errors in cross-platform simulation
            }
        }

        #endregion
    }
}
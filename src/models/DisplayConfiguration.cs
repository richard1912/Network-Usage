using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// User preferences and system settings for UI display behavior
    /// Based on data-model.md specification with validation rules and default values
    /// Thread-safe with property change notifications
    /// </summary>
    public class DisplayConfiguration : INotifyPropertyChanged, IEquatable<DisplayConfiguration>
    {
        private TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
        private bool _autoScaleUnits = true;
        private WindowsTheme _currentTheme = WindowsTheme.Auto;
        private bool _showInSystemTray = true;
        private string _toolTipFormat = "↓{0} ↑{1}";
        private int _responseTimeoutMs = 100;

        private readonly object _lock = new object();

        /// <summary>
        /// How often to refresh network data (default: 1 second)
        /// Must be between 500ms and 10 seconds per validation rules
        /// </summary>
        public TimeSpan UpdateInterval
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _updateInterval; 
                } 
            }
            set
            {
                ValidateUpdateInterval(value);
                lock (_lock)
                {
                    if (_updateInterval != value)
                    {
                        _updateInterval = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Whether to auto-scale B/s → KB/s → MB/s → GB/s (default: true)
        /// </summary>
        public bool AutoScaleUnits
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _autoScaleUnits; 
                } 
            }
            set
            {
                lock (_lock)
                {
                    if (_autoScaleUnits != value)
                    {
                        _autoScaleUnits = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Current Windows theme (Light, Dark, HighContrast, Auto)
        /// </summary>
        public WindowsTheme CurrentTheme
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _currentTheme; 
                } 
            }
            set
            {
                lock (_lock)
                {
                    if (_currentTheme != value)
                    {
                        _currentTheme = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Whether to show icon in system tray (default: true)
        /// </summary>
        public bool ShowInSystemTray
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _showInSystemTray; 
                } 
            }
            set
            {
                lock (_lock)
                {
                    if (_showInSystemTray != value)
                    {
                        _showInSystemTray = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Format string for tray icon tooltip (default: "↓{0} ↑{1}")
        /// Must be a valid .NET format string with exactly 2 placeholders
        /// </summary>
        public string ToolTipFormat
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _toolTipFormat; 
                } 
            }
            set
            {
                ValidateToolTipFormat(value);
                lock (_lock)
                {
                    if (_toolTipFormat != value)
                    {
                        _toolTipFormat = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Maximum time to wait for UI responses (default: 100ms)
        /// Must be between 50ms and 1000ms per validation rules
        /// </summary>
        public int ResponseTimeoutMs
        {
            get 
            { 
                lock (_lock) 
                { 
                    return _responseTimeoutMs; 
                } 
            }
            set
            {
                ValidateResponseTimeout(value);
                lock (_lock)
                {
                    if (_responseTimeoutMs != value)
                    {
                        _responseTimeoutMs = value;
                        OnPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Default constructor with default values from data-model.md
        /// </summary>
        public DisplayConfiguration()
        {
            // Default values per specification
            _updateInterval = TimeSpan.FromSeconds(1);
            _autoScaleUnits = true;
            _currentTheme = WindowsTheme.Auto;
            _showInSystemTray = true;
            _toolTipFormat = "↓{0} ↑{1}";
            _responseTimeoutMs = 100;
        }

        /// <summary>
        /// Constructor with custom values
        /// </summary>
        public DisplayConfiguration(TimeSpan updateInterval, bool autoScaleUnits, WindowsTheme currentTheme,
            bool showInSystemTray, string toolTipFormat, int responseTimeoutMs)
        {
            // Use properties for validation
            UpdateInterval = updateInterval;
            AutoScaleUnits = autoScaleUnits;
            CurrentTheme = currentTheme;
            ShowInSystemTray = showInSystemTray;
            ToolTipFormat = toolTipFormat;
            ResponseTimeoutMs = responseTimeoutMs;
        }

        #region Validation Methods

        private static void ValidateUpdateInterval(TimeSpan interval)
        {
            if (interval < TimeSpan.FromMilliseconds(500) || interval > TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), 
                    "UpdateInterval must be between 500ms and 10 seconds");
            }
        }

        private static void ValidateResponseTimeout(int timeoutMs)
        {
            if (timeoutMs < 50 || timeoutMs > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMs), 
                    "ResponseTimeoutMs must be between 50ms and 1000ms");
            }
        }

        private static void ValidateToolTipFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("ToolTipFormat cannot be null or empty", nameof(format));
            }

            try
            {
                // Test format string with sample values
                string.Format(format, "1.5 MB/s", "750 KB/s");
                
                // Verify it has exactly 2 placeholders
                if (!format.Contains("{0}") || !format.Contains("{1}"))
                {
                    throw new ArgumentException("ToolTipFormat must contain exactly {0} and {1} placeholders", nameof(format));
                }
                
                // Verify it doesn't have more than 2 placeholders
                if (format.Contains("{2}"))
                {
                    throw new ArgumentException("ToolTipFormat must have exactly 2 placeholders, not more", nameof(format));
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"ToolTipFormat is not a valid .NET format string: {ex.Message}", nameof(format));
            }
        }

        #endregion

        #region Validation and State Methods

        /// <summary>
        /// Validate all configuration values according to data-model.md rules
        /// </summary>
        public bool IsValid()
        {
            try
            {
                ValidateUpdateInterval(UpdateInterval);
                ValidateResponseTimeout(ResponseTimeoutMs);
                ValidateToolTipFormat(ToolTipFormat);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Apply theme-specific configuration adjustments
        /// </summary>
        public DisplayConfiguration GetThemeAdjustedConfiguration(WindowsTheme theme)
        {
            var adjusted = Clone();
            adjusted.CurrentTheme = theme;
            
            // Theme-specific adjustments could be added here
            // For example, high contrast theme might need different timeout values
            if (theme == WindowsTheme.HighContrast)
            {
                // Allow more time for high contrast rendering
                if (adjusted.ResponseTimeoutMs < 150)
                    adjusted.ResponseTimeoutMs = 150;
            }
            
            return adjusted;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        public void ResetToDefaults()
        {
            lock (_lock)
            {
                _updateInterval = TimeSpan.FromSeconds(1);
                _autoScaleUnits = true;
                _currentTheme = WindowsTheme.Auto;
                _showInSystemTray = true;
                _toolTipFormat = "↓{0} ↑{1}";
                _responseTimeoutMs = 100;
                
                OnPropertyChanged(nameof(UpdateInterval));
                OnPropertyChanged(nameof(AutoScaleUnits));
                OnPropertyChanged(nameof(CurrentTheme));
                OnPropertyChanged(nameof(ShowInSystemTray));
                OnPropertyChanged(nameof(ToolTipFormat));
                OnPropertyChanged(nameof(ResponseTimeoutMs));
            }
        }

        /// <summary>
        /// Create a thread-safe copy of this configuration
        /// </summary>
        public DisplayConfiguration Clone()
        {
            lock (_lock)
            {
                return new DisplayConfiguration(
                    _updateInterval,
                    _autoScaleUnits,
                    _currentTheme,
                    _showInSystemTray,
                    _toolTipFormat,
                    _responseTimeoutMs
                );
            }
        }

        #endregion

        #region IEquatable<DisplayConfiguration> Implementation

        public bool Equals(DisplayConfiguration? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            lock (_lock)
            {
                lock (other._lock)
                {
                    return _updateInterval == other._updateInterval &&
                           _autoScaleUnits == other._autoScaleUnits &&
                           _currentTheme == other._currentTheme &&
                           _showInSystemTray == other._showInSystemTray &&
                           _toolTipFormat == other._toolTipFormat &&
                           _responseTimeoutMs == other._responseTimeoutMs;
                }
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DisplayConfiguration);
        }

        public override int GetHashCode()
        {
            lock (_lock)
            {
                return HashCode.Combine(
                    _updateInterval,
                    _autoScaleUnits,
                    _currentTheme,
                    _showInSystemTray,
                    _toolTipFormat,
                    _responseTimeoutMs
                );
            }
        }

        public static bool operator ==(DisplayConfiguration? left, DisplayConfiguration? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(DisplayConfiguration? left, DisplayConfiguration? right)
        {
            return !(left == right);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// String representation for debugging and logging
        /// </summary>
        public override string ToString()
        {
            lock (_lock)
            {
                return $"DisplayConfiguration: Interval={_updateInterval.TotalMilliseconds}ms, " +
                       $"AutoScale={_autoScaleUnits}, Theme={_currentTheme}, " +
                       $"SystemTray={_showInSystemTray}, Timeout={_responseTimeoutMs}ms, " +
                       $"ToolTip='{_toolTipFormat}'";
            }
        }
    }
}
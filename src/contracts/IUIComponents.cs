using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Contract for modern GUI components that display detailed network statistics
    /// Responsible for: Statistics window display, theme adaptation, user interactions
    /// </summary>
    public interface IUIComponents
    {
        /// <summary>
        /// Event fired when user closes the statistics window
        /// Used for: Cleanup, returning focus to system tray
        /// </summary>
        event EventHandler<StatisticsWindowEventArgs> StatisticsWindowClosed;

        /// <summary>
        /// Event fired when user interacts with statistics window
        /// Triggers: Button clicks, settings changes, window actions
        /// </summary>
        event EventHandler<UIInteractionEventArgs> UserInteraction;

        /// <summary>
        /// Show the detailed network statistics window
        /// Must: Display current and historical network data in modern Windows 11 UI
        /// Must: Position window appropriately relative to system tray icon
        /// Must: Apply current Windows theme per FR-016, FR-017
        /// Performance: Window must appear within 100ms per FR-013
        /// </summary>
        Task ShowDetailedStatsAsync();

        /// <summary>
        /// Hide the statistics window
        /// Must: Close window gracefully, save window state if needed
        /// Should: Return focus to previous application
        /// Performance: Must complete within 100ms
        /// </summary>
        Task HideDetailedStatsAsync();

        /// <summary>
        /// Update the statistics display with new network data
        /// Parameters: trafficData - Current network measurements
        /// Must: Update all speed displays, charts, and indicators in real-time
        /// Must: Auto-scale units per FR-010 (B/s → KB/s → MB/s → GB/s)
        /// Performance: UI updates must complete within 50ms
        /// </summary>
        Task UpdateStatisticsAsync(NetworkTrafficData trafficData);

        /// <summary>
        /// Apply Windows 11 theme to all UI components
        /// Parameters: theme - Target theme (Light, Dark, HighContrast)
        /// Must: Update colors, fonts, and styling to match Windows 11 design system
        /// Must: Handle automatic theme switching per FR-016
        /// Performance: Theme changes must complete within 200ms
        /// </summary>
        Task ApplyThemeAsync(WindowsTheme theme);

        /// <summary>
        /// Update display with information about multiple network adapters
        /// Parameters: adapters - List of available network adapters with status
        /// Used for: Adapter selection, status display, troubleshooting
        /// Must: Show adapter names, types, status, and current speeds
        /// </summary>
        Task UpdateAdapterListAsync(IEnumerable<NetworkAdapter> adapters);

        /// <summary>
        /// Show error message to user with appropriate styling
        /// Parameters: error - Error details and user-friendly message
        /// Must: Display in Windows 11 style error dialog
        /// Must: Provide actionable information when possible
        /// Performance: Error dialog must appear within 100ms
        /// </summary>
        Task ShowErrorAsync(string errorMessage, Exception? exception = null);

        /// <summary>
        /// Handle user interaction events from UI components
        /// Parameters: interaction - Details about what user action occurred
        /// Used for: Settings changes, adapter selection, window management
        /// Must: Process interactions according to UI event handling patterns
        /// Performance: Must respond to user input within 100ms per FR-013
        /// </summary>
        Task HandleInteractionAsync(UIInteractionEventArgs interaction);

        /// <summary>
        /// Check if the statistics window is currently visible
        /// Returns: True if ShowDetailedStatsAsync called and window not closed
        /// Must: Return immediately from cached window state
        /// </summary>
        bool IsStatisticsWindowVisible { get; }

        /// <summary>
        /// Get current theme applied to UI components
        /// Returns: Currently active Windows theme
        /// Used for: Theme synchronization, testing theme application
        /// </summary>
        WindowsTheme CurrentTheme { get; }

        /// <summary>
        /// Configure UI update frequency for statistics display
        /// Parameters: interval - How often to refresh UI elements
        /// Must: Balance smooth updates with performance per FR-014
        /// Should: Sync with network monitoring interval
        /// </summary>
        Task SetUIUpdateIntervalAsync(TimeSpan interval);

        /// <summary>
        /// Position statistics window relative to system tray icon
        /// Parameters: trayIconBounds - Screen coordinates of tray icon
        /// Must: Position window to avoid screen edges, taskbar overlap
        /// Must: Follow Windows 11 positioning guidelines for system tray apps
        /// </summary>
        Task PositionWindowAsync(System.Drawing.Rectangle trayIconBounds);

        /// <summary>
        /// Initialize UI components and prepare for display
        /// Must: Load resources, apply initial theme, prepare window templates
        /// Performance: Initialization must complete within 500ms
        /// Called: Once during application startup
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Cleanup UI resources and prepare for application shutdown
        /// Must: Dispose windows, clear event handlers, release UI resources
        /// Should: Save user preferences and window states
        /// Called: Once during application shutdown
        /// </summary>
        Task ShutdownAsync();
    }

    /// <summary>
    /// Event arguments for statistics window events
    /// </summary>
    public class StatisticsWindowEventArgs : EventArgs
    {
        public string WindowAction { get; set; } // "Close", "Minimize", "Resize"
        public DateTime Timestamp { get; set; }
        public System.Drawing.Rectangle WindowBounds { get; set; }
    }

    /// <summary>
    /// Event arguments for UI interaction events
    /// </summary>
    public class UIInteractionEventArgs : EventArgs
    {
        public string InteractionType { get; set; } // "ButtonClick", "SettingChange", "AdapterSelect"
        public string ElementName { get; set; }
        public object InteractionData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Configuration for UI component behavior and appearance
    /// </summary>
    public class UIConfiguration
    {
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(1);
        public WindowsTheme PreferredTheme { get; set; } = WindowsTheme.Auto;
        public bool ShowWindowOnStartup { get; set; } = false;
        public System.Drawing.Size PreferredWindowSize { get; set; }
        public System.Drawing.Point PreferredWindowPosition { get; set; }
        public string DateTimeFormat { get; set; } = "HH:mm:ss";
        public bool EnableAnimations { get; set; } = true;
    }
}

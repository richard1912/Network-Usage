using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Contract for Windows 11 taskbar/system tray integration functionality
    /// Responsible for: System tray icon management, tooltip updates, user interaction handling
    /// </summary>
    public interface ITaskbarIntegration
    {
        /// <summary>
        /// Event fired when user clicks on the system tray icon
        /// Triggers: Left click, right click, double click events
        /// </summary>
        event EventHandler<TrayIconClickEventArgs> IconClicked;

        /// <summary>
        /// Event fired when user hovers over the system tray icon
        /// Used for: Showing detailed statistics, tooltip updates
        /// </summary>
        event EventHandler<TrayIconHoverEventArgs> IconHovered;

        /// <summary>
        /// Initialize and show the system tray icon
        /// Must: Create NotifyIcon, set initial icon, register event handlers
        /// Must: Apply current Windows theme colors/styling per FR-016
        /// Throws: InvalidOperationException if system tray not available
        /// </summary>
        Task ShowAsync();

        /// <summary>
        /// Hide the system tray icon and cleanup resources
        /// Must: Remove icon from tray, dispose NotifyIcon, unregister events
        /// Should: Preserve configuration for future ShowAsync calls
        /// </summary>
        Task HideAsync();

        /// <summary>
        /// Update the system tray icon to reflect current network activity
        /// Parameters: trafficData - Current network speed information
        /// Must: Update tooltip with formatted speeds, change icon if needed
        /// Must: Complete within 50ms to maintain real-time feel per FR-013
        /// </summary>
        Task UpdateDisplayAsync(NetworkTrafficData trafficData);

        /// <summary>
        /// Update tooltip text shown on hover
        /// Parameters: tooltipText - Text to display (max 127 characters for Windows compatibility)
        /// Must: Format according to DisplayConfiguration.ToolTipFormat
        /// Must: Auto-scale units per FR-010 (B/s → KB/s → MB/s → GB/s)
        /// </summary>
        Task SetTooltipAsync(string tooltipText);

        /// <summary>
        /// Change the system tray icon image
        /// Parameters: iconData - Icon image data or path
        /// Used for: Showing activity state, theme adaptation
        /// Must: Support Windows theme changes (light/dark icons)
        /// </summary>
        Task SetIconAsync(byte[] iconData);

        /// <summary>
        /// Apply Windows 11 theme styling to the tray integration
        /// Parameters: theme - Windows theme (Light, Dark, HighContrast)
        /// Must: Update icon colors, tooltip styling per FR-016, FR-017
        /// Must: Handle theme changes automatically when system theme changes
        /// </summary>
        Task ApplyThemeAsync(WindowsTheme theme);

        /// <summary>
        /// Show context menu when user right-clicks tray icon
        /// Must: Include options for: Show Statistics, Settings, Exit
        /// Must: Style menu according to current Windows theme
        /// Performance: Menu must appear within 100ms of right-click
        /// </summary>
        Task ShowContextMenuAsync();

        /// <summary>
        /// Check if the tray icon is currently visible
        /// Returns: True if ShowAsync called and HideAsync not called
        /// Must: Return immediately from cached state
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Get current tooltip text
        /// Returns: Currently displayed tooltip text
        /// Used for: Testing, debugging tooltip formatting
        /// </summary>
        string CurrentTooltip { get; }

        /// <summary>
        /// Configure display format for network speeds in tooltip
        /// Parameters: format - Format string with placeholders for upload/download speeds
        /// Example: "↓{0} ↑{1}" where {0} = download, {1} = upload
        /// Must: Validate format string has exactly 2 placeholders
        /// </summary>
        Task SetDisplayFormatAsync(string format);
    }

    /// <summary>
    /// Event arguments for tray icon click events
    /// </summary>
    public class TrayIconClickEventArgs : EventArgs
    {
        public MouseButtons Button { get; set; }
        public int ClickCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for tray icon hover events
    /// </summary>
    public class TrayIconHoverEventArgs : EventArgs
    {
        public bool IsEntering { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

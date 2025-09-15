using System;
using System.ComponentModel;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Available Windows theme options
    /// Based on data-model.md specification with support for Windows 11 themes
    /// </summary>
    public enum WindowsTheme
    {
        /// <summary>
        /// Standard light theme (Windows default light mode)
        /// </summary>
        [Description("Light Theme")]
        Light = 0,

        /// <summary>
        /// Standard dark theme (Windows dark mode)
        /// </summary>
        [Description("Dark Theme")]
        Dark = 1,

        /// <summary>
        /// High contrast accessibility theme
        /// </summary>
        [Description("High Contrast Theme")]
        HighContrast = 2,

        /// <summary>
        /// Automatically detect and follow system theme
        /// </summary>
        [Description("Automatic (Follow System)")]
        Auto = 3
    }

    /// <summary>
    /// Units for network speed display
    /// Based on data-model.md specification for auto-scaling display
    /// </summary>
    public enum SpeedUnit
    {
        /// <summary>
        /// Bytes per second (B/s)
        /// </summary>
        [Description("B/s")]
        Bytes = 0,

        /// <summary>
        /// Kilobytes per second (KB/s)
        /// 1 KB = 1024 bytes
        /// </summary>
        [Description("KB/s")]
        Kilobytes = 1,

        /// <summary>
        /// Megabytes per second (MB/s)
        /// 1 MB = 1024 KB = 1,048,576 bytes
        /// </summary>
        [Description("MB/s")]
        Megabytes = 2,

        /// <summary>
        /// Gigabytes per second (GB/s)
        /// 1 GB = 1024 MB = 1,073,741,824 bytes
        /// </summary>
        [Description("GB/s")]
        Gigabytes = 3
    }

    /// <summary>
    /// Extension methods for enum types
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Get the description attribute value for an enum value
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();

            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? value.ToString();
        }

        /// <summary>
        /// Get user-friendly display name for WindowsTheme
        /// </summary>
        public static string GetDisplayName(this WindowsTheme theme)
        {
            return theme switch
            {
                WindowsTheme.Light => "Light",
                WindowsTheme.Dark => "Dark", 
                WindowsTheme.HighContrast => "High Contrast",
                WindowsTheme.Auto => "Auto",
                _ => theme.ToString()
            };
        }

        /// <summary>
        /// Get unit suffix for SpeedUnit
        /// </summary>
        public static string GetSuffix(this SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.Bytes => "B/s",
                SpeedUnit.Kilobytes => "KB/s",
                SpeedUnit.Megabytes => "MB/s", 
                SpeedUnit.Gigabytes => "GB/s",
                _ => "?/s"
            };
        }

        /// <summary>
        /// Get conversion factor to bytes for SpeedUnit
        /// </summary>
        public static double GetBytesMultiplier(this SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.Bytes => 1.0,
                SpeedUnit.Kilobytes => 1024.0,
                SpeedUnit.Megabytes => 1024.0 * 1024.0,
                SpeedUnit.Gigabytes => 1024.0 * 1024.0 * 1024.0,
                _ => 1.0
            };
        }

        /// <summary>
        /// Check if theme is a dark variant
        /// </summary>
        public static bool IsDarkTheme(this WindowsTheme theme)
        {
            return theme == WindowsTheme.Dark || theme == WindowsTheme.HighContrast;
        }

        /// <summary>
        /// Check if theme requires automatic detection
        /// </summary>
        public static bool RequiresAutoDetection(this WindowsTheme theme)
        {
            return theme == WindowsTheme.Auto;
        }

        /// <summary>
        /// Get the next larger speed unit for auto-scaling
        /// </summary>
        public static SpeedUnit GetNextLargerUnit(this SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.Bytes => SpeedUnit.Kilobytes,
                SpeedUnit.Kilobytes => SpeedUnit.Megabytes,
                SpeedUnit.Megabytes => SpeedUnit.Gigabytes,
                SpeedUnit.Gigabytes => SpeedUnit.Gigabytes, // Already at max
                _ => SpeedUnit.Bytes
            };
        }

        /// <summary>
        /// Get the next smaller speed unit for scaling down
        /// </summary>
        public static SpeedUnit GetNextSmallerUnit(this SpeedUnit unit)
        {
            return unit switch
            {
                SpeedUnit.Gigabytes => SpeedUnit.Megabytes,
                SpeedUnit.Megabytes => SpeedUnit.Kilobytes,
                SpeedUnit.Kilobytes => SpeedUnit.Bytes,
                SpeedUnit.Bytes => SpeedUnit.Bytes, // Already at min
                _ => SpeedUnit.Bytes
            };
        }

        /// <summary>
        /// Check if unit is appropriate for the given speed value
        /// </summary>
        public static bool IsAppropriateUnit(this SpeedUnit unit, double bytesPerSecond)
        {
            const double kb = 1024;
            const double mb = kb * 1024;
            const double gb = mb * 1024;

            return unit switch
            {
                SpeedUnit.Bytes => bytesPerSecond < kb,
                SpeedUnit.Kilobytes => bytesPerSecond >= kb && bytesPerSecond < mb,
                SpeedUnit.Megabytes => bytesPerSecond >= mb && bytesPerSecond < gb,
                SpeedUnit.Gigabytes => bytesPerSecond >= gb,
                _ => false
            };
        }

        /// <summary>
        /// Get all available WindowsTheme values for UI binding
        /// </summary>
        public static WindowsTheme[] GetAllThemes()
        {
            return new[] { WindowsTheme.Light, WindowsTheme.Dark, WindowsTheme.HighContrast, WindowsTheme.Auto };
        }

        /// <summary>
        /// Get all available SpeedUnit values for UI binding
        /// </summary>
        public static SpeedUnit[] GetAllSpeedUnits()
        {
            return new[] { SpeedUnit.Bytes, SpeedUnit.Kilobytes, SpeedUnit.Megabytes, SpeedUnit.Gigabytes };
        }

        /// <summary>
        /// Parse WindowsTheme from string (case-insensitive)
        /// </summary>
        public static WindowsTheme ParseWindowsTheme(string themeString)
        {
            if (string.IsNullOrWhiteSpace(themeString))
                return WindowsTheme.Auto;

            return themeString.ToLowerInvariant() switch
            {
                "light" => WindowsTheme.Light,
                "dark" => WindowsTheme.Dark,
                "highcontrast" or "high-contrast" or "high_contrast" => WindowsTheme.HighContrast,
                "auto" or "automatic" => WindowsTheme.Auto,
                _ => WindowsTheme.Auto // Default fallback
            };
        }

        /// <summary>
        /// Parse SpeedUnit from string (case-insensitive)
        /// </summary>
        public static SpeedUnit ParseSpeedUnit(string unitString)
        {
            if (string.IsNullOrWhiteSpace(unitString))
                return SpeedUnit.Bytes;

            var normalized = unitString.ToLowerInvariant().Replace("/s", "").Trim();
            
            return normalized switch
            {
                "b" or "bytes" or "byte" => SpeedUnit.Bytes,
                "kb" or "kilobytes" or "kilobyte" => SpeedUnit.Kilobytes,
                "mb" or "megabytes" or "megabyte" => SpeedUnit.Megabytes,
                "gb" or "gigabytes" or "gigabyte" => SpeedUnit.Gigabytes,
                _ => SpeedUnit.Bytes // Default fallback
            };
        }
    }
}

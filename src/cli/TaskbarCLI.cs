using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;
using NetworkUsage.Services;

namespace NetworkUsage.CLI
{
    /// <summary>
    /// Command-line interface for TaskbarIntegration library
    /// Constitutional requirement: Each library must have CLI interface
    /// Provides: --show --hide --position [coordinates] --theme [theme] --format [format]
    /// </summary>
    public class TaskbarCLI
    {
        private readonly TaskbarIntegrationService _taskbarIntegration;
        private readonly ILogger<TaskbarCLI> _logger;

        public TaskbarCLI(TaskbarIntegrationService taskbarIntegration, ILogger<TaskbarCLI> logger)
        {
            _taskbarIntegration = taskbarIntegration ?? throw new ArgumentNullException(nameof(taskbarIntegration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create the CLI command structure for TaskbarIntegration
        /// </summary>
        public static Command CreateCommand(TaskbarIntegrationService taskbarIntegration, ILogger<TaskbarCLI> logger)
        {
            var cli = new TaskbarCLI(taskbarIntegration, logger);
            
            var rootCommand = new Command("taskbar", "Taskbar integration CLI commands");

            // Show command
            var showCommand = cli.CreateShowCommand();
            rootCommand.AddCommand(showCommand);

            // Hide command
            var hideCommand = cli.CreateHideCommand();
            rootCommand.AddCommand(hideCommand);

            // Update command
            var updateCommand = cli.CreateUpdateCommand();
            rootCommand.AddCommand(updateCommand);

            // Theme command
            var themeCommand = cli.CreateThemeCommand();
            rootCommand.AddCommand(themeCommand);

            // Format command
            var formatCommand = cli.CreateFormatCommand();
            rootCommand.AddCommand(formatCommand);

            // Status command
            var statusCommand = cli.CreateStatusCommand();
            rootCommand.AddCommand(statusCommand);

            return rootCommand;
        }

        #region Command Creation Methods

        /// <summary>
        /// Create show command: show --theme [theme]
        /// </summary>
        private Command CreateShowCommand()
        {
            var command = new Command("show", "Show system tray icon");

            var themeOption = new Option<string>(
                aliases: new[] { "--theme", "-t" },
                description: "Theme to apply (auto|light|dark|highcontrast)",
                getDefaultValue: () => "auto"
            );

            command.AddOption(themeOption);

            command.SetHandler(async (context) =>
            {
                var themeString = context.ParseResult.GetValueForOption(themeOption)!;
                await ExecuteShowCommandAsync(themeString);
            });

            return command;
        }

        /// <summary>
        /// Create hide command: hide
        /// </summary>
        private Command CreateHideCommand()
        {
            var command = new Command("hide", "Hide system tray icon");

            command.SetHandler(async (context) =>
            {
                await ExecuteHideCommandAsync();
            });

            return command;
        }

        /// <summary>
        /// Create update command: update --download [speed] --upload [speed] --adapter [name]
        /// </summary>
        private Command CreateUpdateCommand()
        {
            var command = new Command("update", "Update tray icon with network data");

            var downloadOption = new Option<double>(
                aliases: new[] { "--download", "-d" },
                description: "Download speed in bytes per second",
                getDefaultValue: () => 0.0
            );

            var uploadOption = new Option<double>(
                aliases: new[] { "--upload", "-u" },
                description: "Upload speed in bytes per second",
                getDefaultValue: () => 0.0
            );

            var adapterOption = new Option<string>(
                aliases: new[] { "--adapter", "-a" },
                description: "Adapter name",
                getDefaultValue: () => "CLI Test Adapter"
            );

            command.AddOption(downloadOption);
            command.AddOption(uploadOption);
            command.AddOption(adapterOption);

            command.SetHandler(async (context) =>
            {
                var download = context.ParseResult.GetValueForOption(downloadOption);
                var upload = context.ParseResult.GetValueForOption(uploadOption);
                var adapter = context.ParseResult.GetValueForOption(adapterOption)!;

                await ExecuteUpdateCommandAsync(download, upload, adapter);
            });

            return command;
        }

        /// <summary>
        /// Create theme command: theme --set [theme]
        /// </summary>
        private Command CreateThemeCommand()
        {
            var command = new Command("theme", "Manage taskbar icon theme");

            var setOption = new Option<string>(
                aliases: new[] { "--set", "-s" },
                description: "Set theme (auto|light|dark|highcontrast)"
            );

            command.AddOption(setOption);

            command.SetHandler(async (context) =>
            {
                var themeString = context.ParseResult.GetValueForOption(setOption);
                await ExecuteThemeCommandAsync(themeString);
            });

            return command;
        }

        /// <summary>
        /// Create format command: format --set [format]
        /// </summary>
        private Command CreateFormatCommand()
        {
            var command = new Command("format", "Manage tooltip display format");

            var setOption = new Option<string>(
                aliases: new[] { "--set", "-s" },
                description: "Set display format (e.g., '↓{0} ↑{1}')"
            );

            command.AddOption(setOption);

            command.SetHandler(async (context) =>
            {
                var format = context.ParseResult.GetValueForOption(setOption);
                await ExecuteFormatCommandAsync(format);
            });

            return command;
        }

        /// <summary>
        /// Create status command: status --format [json|text]
        /// </summary>
        private Command CreateStatusCommand()
        {
            var command = new Command("status", "Get taskbar integration status");

            var formatOption = new Option<string>(
                aliases: new[] { "--format", "-f" },
                description: "Output format (json|text)",
                getDefaultValue: () => "text"
            );

            command.AddOption(formatOption);

            command.SetHandler(async (context) =>
            {
                var format = context.ParseResult.GetValueForOption(formatOption)!;
                await ExecuteStatusCommandAsync(format);
            });

            return command;
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Execute show command
        /// </summary>
        private async Task ExecuteShowCommandAsync(string themeString)
        {
            try
            {
                var theme = EnumExtensions.ParseWindowsTheme(themeString);
                
                Console.WriteLine($"Showing system tray icon with {theme} theme...");
                
                // Apply theme first, then show
                await _taskbarIntegration.ApplyThemeAsync(theme);
                await _taskbarIntegration.ShowAsync();

                Console.WriteLine("System tray icon is now visible.");
                Console.WriteLine($"Current tooltip: \"{_taskbarIntegration.CurrentTooltip}\"");
                
                _logger.LogInformation($"System tray icon shown via CLI with theme: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing system tray icon: {ex.Message}");
                _logger.LogError(ex, "Show command failed");
            }
        }

        /// <summary>
        /// Execute hide command
        /// </summary>
        private async Task ExecuteHideCommandAsync()
        {
            try
            {
                if (!_taskbarIntegration.IsVisible)
                {
                    Console.WriteLine("System tray icon is already hidden.");
                    return;
                }

                Console.WriteLine("Hiding system tray icon...");
                await _taskbarIntegration.HideAsync();
                
                Console.WriteLine("System tray icon has been hidden.");
                
                _logger.LogInformation("System tray icon hidden via CLI command");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding system tray icon: {ex.Message}");
                _logger.LogError(ex, "Hide command failed");
            }
        }

        /// <summary>
        /// Execute update command
        /// </summary>
        private async Task ExecuteUpdateCommandAsync(double downloadSpeed, double uploadSpeed, string adapterName)
        {
            try
            {
                if (!_taskbarIntegration.IsVisible)
                {
                    Console.WriteLine("Warning: System tray icon is not visible. Use 'show' command first.");
                    return;
                }

                Console.WriteLine($"Updating tray icon with speeds: Down={downloadSpeed:F0} B/s, Up={uploadSpeed:F0} B/s");

                var trafficData = new NetworkTrafficData(
                    0, 0, // Total bytes (not needed for display update)
                    downloadSpeed,
                    uploadSpeed,
                    adapterName,
                    System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                );

                await _taskbarIntegration.UpdateDisplayAsync(trafficData);

                Console.WriteLine("Tray icon updated successfully.");
                Console.WriteLine($"New tooltip: \"{_taskbarIntegration.CurrentTooltip}\"");
                
                _logger.LogInformation($"Tray icon updated via CLI: {downloadSpeed:F0}/{uploadSpeed:F0} B/s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating tray icon: {ex.Message}");
                _logger.LogError(ex, "Update command failed");
            }
        }

        /// <summary>
        /// Execute theme command
        /// </summary>
        private async Task ExecuteThemeCommandAsync(string? themeString)
        {
            try
            {
                if (string.IsNullOrEmpty(themeString))
                {
                    // Show current theme
                    Console.WriteLine("Available themes: auto, light, dark, highcontrast");
                    Console.WriteLine("Use --set to change theme");
                    return;
                }

                var theme = EnumExtensions.ParseWindowsTheme(themeString);
                
                Console.WriteLine($"Applying {theme} theme to taskbar integration...");
                await _taskbarIntegration.ApplyThemeAsync(theme);
                
                Console.WriteLine($"Theme successfully applied: {theme}");
                
                _logger.LogInformation($"Theme changed via CLI to: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
                _logger.LogError(ex, "Theme command failed");
            }
        }

        /// <summary>
        /// Execute format command
        /// </summary>
        private async Task ExecuteFormatCommandAsync(string? format)
        {
            try
            {
                if (string.IsNullOrEmpty(format))
                {
                    // Show current format
                    Console.WriteLine($"Current display format: \"{_taskbarIntegration.CurrentTooltip}\"");
                    Console.WriteLine("Common formats:");
                    Console.WriteLine("  \"↓{0} ↑{1}\"          - Default arrows format");
                    Console.WriteLine("  \"Down: {0} Up: {1}\"  - Text format");
                    Console.WriteLine("  \"{0} / {1}\"          - Simple slash format");
                    Console.WriteLine("  \"📥{0} 📤{1}\"        - Emoji format");
                    Console.WriteLine("Use --set to change format");
                    return;
                }

                Console.WriteLine($"Setting display format to: \"{format}\"");
                await _taskbarIntegration.SetDisplayFormatAsync(format);
                
                Console.WriteLine("Display format updated successfully.");
                Console.WriteLine("Next update will use the new format.");
                
                _logger.LogInformation($"Display format changed via CLI to: {format}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting display format: {ex.Message}");
                _logger.LogError(ex, "Format command failed");
            }
        }

        /// <summary>
        /// Execute status command
        /// </summary>
        private async Task ExecuteStatusCommandAsync(string format)
        {
            try
            {
                if (!IsValidFormat(format))
                {
                    Console.WriteLine("Error: Format must be 'json' or 'text'");
                    return;
                }

                var isVisible = _taskbarIntegration.IsVisible;
                var currentTooltip = _taskbarIntegration.CurrentTooltip;
                var averagePerformance = _taskbarIntegration.GetAveragePerformanceMs();

                if (format.ToLowerInvariant() == "json")
                {
                    var status = new
                    {
                        IsVisible = isVisible,
                        CurrentTooltip = currentTooltip,
                        AveragePerformanceMs = averagePerformance,
                        Timestamp = DateTime.Now
                    };

                    var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine("Taskbar Integration Status");
                    Console.WriteLine(new string('=', 35));
                    Console.WriteLine($"Icon Visible: {(isVisible ? "Yes" : "No")}");
                    Console.WriteLine($"Current Tooltip: \"{currentTooltip}\"");
                    Console.WriteLine($"Average Response Time: {averagePerformance:F1}ms");
                    Console.WriteLine($"Status Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }

                _logger.LogDebug($"Status reported via CLI in {format} format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting status: {ex.Message}");
                _logger.LogError(ex, "Status command failed");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validate output format parameter
        /// </summary>
        private static bool IsValidFormat(string format)
        {
            return format.ToLowerInvariant() is "json" or "text";
        }

        /// <summary>
        /// Parse position coordinates from string
        /// </summary>
        private static Rectangle ParsePositionCoordinates(string coordinates)
        {
            try
            {
                // Expected format: "x,y,width,height" or "x,y"
                var parts = coordinates.Split(',');
                
                if (parts.Length == 2)
                {
                    var x = int.Parse(parts[0].Trim());
                    var y = int.Parse(parts[1].Trim());
                    return new Rectangle(x, y, 32, 32); // Default icon size
                }
                else if (parts.Length == 4)
                {
                    var x = int.Parse(parts[0].Trim());
                    var y = int.Parse(parts[1].Trim());
                    var width = int.Parse(parts[2].Trim());
                    var height = int.Parse(parts[3].Trim());
                    return new Rectangle(x, y, width, height);
                }
                else
                {
                    throw new ArgumentException("Coordinates must be in format 'x,y' or 'x,y,width,height'");
                }
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid coordinate format. Use integers separated by commas.");
            }
        }

        /// <summary>
        /// Create sample network traffic data for testing
        /// </summary>
        private static NetworkTrafficData CreateSampleTrafficData(double downloadSpeed, double uploadSpeed, string adapterName)
        {
            return new NetworkTrafficData(
                bytesReceived: (long)(downloadSpeed * 60), // Simulate 1 minute of data
                bytesSent: (long)(uploadSpeed * 60),
                receiveSpeed: downloadSpeed,
                sendSpeed: uploadSpeed,
                adapterName: adapterName,
                adapterType: System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            );
        }

        #endregion

        #region Static Entry Point

        /// <summary>
        /// Main entry point for TaskbarIntegration CLI
        /// Usage: NetworkUsage.exe taskbar [command] [options]
        /// </summary>
        public static async Task<int> RunAsync(string[] args, TaskbarIntegrationService? taskbarIntegration = null, ILogger<TaskbarCLI>? logger = null)
        {
            try
            {
                // Create default services if not provided
                if (logger == null)
                {
                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    logger = loggerFactory.CreateLogger<TaskbarCLI>();
                }

                if (taskbarIntegration == null)
                {
                    taskbarIntegration = new TaskbarIntegrationService(
                        LoggerFactory.Create(builder => builder.AddConsole())
                            .CreateLogger<TaskbarIntegrationService>());
                }

                var rootCommand = CreateCommand(taskbarIntegration, logger);
                
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Show help information
        /// </summary>
        public static void ShowHelp()
        {
            Console.WriteLine("TaskbarIntegration CLI - System Tray Management Tool");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  show             Show system tray icon");
            Console.WriteLine("    --theme, -t    Theme to apply (auto|light|dark|highcontrast) [default: auto]");
            Console.WriteLine();
            Console.WriteLine("  hide             Hide system tray icon");
            Console.WriteLine();
            Console.WriteLine("  update           Update tray icon with network data");
            Console.WriteLine("    --download, -d Download speed in bytes/sec [default: 0]");
            Console.WriteLine("    --upload, -u   Upload speed in bytes/sec [default: 0]");
            Console.WriteLine("    --adapter, -a  Adapter name [default: 'CLI Test Adapter']");
            Console.WriteLine();
            Console.WriteLine("  theme            Manage icon theme");
            Console.WriteLine("    --set, -s      Set theme (auto|light|dark|highcontrast)");
            Console.WriteLine();
            Console.WriteLine("  format           Manage tooltip display format");
            Console.WriteLine("    --set, -s      Set format string (e.g., '↓{0} ↑{1}')");
            Console.WriteLine();
            Console.WriteLine("  status           Get taskbar integration status");
            Console.WriteLine("    --format, -f   Output format (json|text) [default: text]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  taskbar show --theme dark");
            Console.WriteLine("  taskbar update --download 1500000 --upload 750000");
            Console.WriteLine("  taskbar theme --set light");
            Console.WriteLine("  taskbar format --set \"📥{0} 📤{1}\"");
            Console.WriteLine("  taskbar status --format json");
        }

        #endregion
    }
}
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;
using NetworkUsage.Services;

namespace NetworkUsage.CLI
{
    /// <summary>
    /// Command-line interface for UIComponents library
    /// Constitutional requirement: Each library must have CLI interface
    /// Provides: --display-stats --theme [auto|light|dark] --position [x,y] --interval [ms]
    /// </summary>
    public class UIComponentsCLI
    {
        private readonly UIComponentsService _uiComponents;
        private readonly ILogger<UIComponentsCLI> _logger;

        public UIComponentsCLI(UIComponentsService uiComponents, ILogger<UIComponentsCLI> logger)
        {
            _uiComponents = uiComponents ?? throw new ArgumentNullException(nameof(uiComponents));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create the CLI command structure for UIComponents
        /// </summary>
        public static Command CreateCommand(UIComponentsService uiComponents, ILogger<UIComponentsCLI> logger)
        {
            var cli = new UIComponentsCLI(uiComponents, logger);
            
            var rootCommand = new Command("ui", "UI components CLI commands");

            // Display stats command
            var displayStatsCommand = cli.CreateDisplayStatsCommand();
            rootCommand.AddCommand(displayStatsCommand);

            // Hide stats command
            var hideStatsCommand = cli.CreateHideStatsCommand();
            rootCommand.AddCommand(hideStatsCommand);

            // Theme command
            var themeCommand = cli.CreateThemeCommand();
            rootCommand.AddCommand(themeCommand);

            // Position command
            var positionCommand = cli.CreatePositionCommand();
            rootCommand.AddCommand(positionCommand);

            // Update command
            var updateCommand = cli.CreateUpdateCommand();
            rootCommand.AddCommand(updateCommand);

            // Status command
            var statusCommand = cli.CreateStatusCommand();
            rootCommand.AddCommand(statusCommand);

            // Initialize command
            var initCommand = cli.CreateInitializeCommand();
            rootCommand.AddCommand(initCommand);

            return rootCommand;
        }

        #region Command Creation Methods

        /// <summary>
        /// Create display-stats command: display-stats --theme [theme] --position [x,y]
        /// </summary>
        private Command CreateDisplayStatsCommand()
        {
            var command = new Command("display-stats", "Show detailed network statistics window");

            var themeOption = new Option<string>(
                aliases: new[] { "--theme", "-t" },
                description: "Theme to apply (auto|light|dark|highcontrast)",
                getDefaultValue: () => "auto"
            );

            var positionOption = new Option<string?>(
                aliases: new[] { "--position", "-p" },
                description: "Window position as 'x,y' coordinates"
            );

            command.AddOption(themeOption);
            command.AddOption(positionOption);

            command.SetHandler(async (context) =>
            {
                var themeString = context.ParseResult.GetValueForOption(themeOption)!;
                var positionString = context.ParseResult.GetValueForOption(positionOption);

                await ExecuteDisplayStatsCommandAsync(themeString, positionString);
            });

            return command;
        }

        /// <summary>
        /// Create hide-stats command: hide-stats
        /// </summary>
        private Command CreateHideStatsCommand()
        {
            var command = new Command("hide-stats", "Hide detailed statistics window");

            command.SetHandler(async (context) =>
            {
                await ExecuteHideStatsCommandAsync();
            });

            return command;
        }

        /// <summary>
        /// Create theme command: theme --set [theme]
        /// </summary>
        private Command CreateThemeCommand()
        {
            var command = new Command("theme", "Manage UI theme");

            var setOption = new Option<string?>(
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
        /// Create position command: position --set [x,y,width,height]
        /// </summary>
        private Command CreatePositionCommand()
        {
            var command = new Command("position", "Manage statistics window position");

            var setOption = new Option<string?>(
                aliases: new[] { "--set", "-s" },
                description: "Set position as 'x,y,width,height' or 'x,y'"
            );

            command.AddOption(setOption);

            command.SetHandler(async (context) =>
            {
                var positionString = context.ParseResult.GetValueForOption(setOption);
                await ExecutePositionCommandAsync(positionString);
            });

            return command;
        }

        /// <summary>
        /// Create update command: update --download [speed] --upload [speed] --adapter [name]
        /// </summary>
        private Command CreateUpdateCommand()
        {
            var command = new Command("update", "Update statistics display with test data");

            var downloadOption = new Option<double>(
                aliases: new[] { "--download", "-d" },
                description: "Download speed in bytes per second",
                getDefaultValue: () => 1_500_000.0 // 1.5 MB/s default
            );

            var uploadOption = new Option<double>(
                aliases: new[] { "--upload", "-u" },
                description: "Upload speed in bytes per second",
                getDefaultValue: () => 750_000.0 // 0.75 MB/s default
            );

            var adapterOption = new Option<string>(
                aliases: new[] { "--adapter", "-a" },
                description: "Adapter name for display",
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
        /// Create status command: status --format [json|text]
        /// </summary>
        private Command CreateStatusCommand()
        {
            var command = new Command("status", "Get UI components status");

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

        /// <summary>
        /// Create initialize command: initialize --theme [theme]
        /// </summary>
        private Command CreateInitializeCommand()
        {
            var command = new Command("initialize", "Initialize UI components");

            var themeOption = new Option<string>(
                aliases: new[] { "--theme", "-t" },
                description: "Initial theme (auto|light|dark|highcontrast)",
                getDefaultValue: () => "auto"
            );

            command.AddOption(themeOption);

            command.SetHandler(async (context) =>
            {
                var themeString = context.ParseResult.GetValueForOption(themeOption)!;
                await ExecuteInitializeCommandAsync(themeString);
            });

            return command;
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Execute display-stats command
        /// </summary>
        private async Task ExecuteDisplayStatsCommandAsync(string themeString, string? positionString)
        {
            try
            {
                var theme = EnumExtensions.ParseWindowsTheme(themeString);
                
                Console.WriteLine($"Showing statistics window with {theme} theme...");

                // Apply theme first
                await _uiComponents.ApplyThemeAsync(theme);

                // Set position if specified
                if (!string.IsNullOrEmpty(positionString))
                {
                    try
                    {
                        var position = ParsePositionCoordinates(positionString);
                        await _uiComponents.PositionWindowAsync(position);
                        Console.WriteLine($"Window positioned at ({position.X}, {position.Y})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Invalid position format: {ex.Message}");
                    }
                }

                // Show the statistics window
                await _uiComponents.ShowDetailedStatsAsync();

                Console.WriteLine("Statistics window is now visible.");
                Console.WriteLine($"Applied theme: {_uiComponents.CurrentTheme}");
                
                _logger.LogInformation($"Statistics window shown via CLI with theme: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing statistics window: {ex.Message}");
                _logger.LogError(ex, "Display stats command failed");
            }
        }

        /// <summary>
        /// Execute hide-stats command
        /// </summary>
        private async Task ExecuteHideStatsCommandAsync()
        {
            try
            {
                if (!_uiComponents.IsStatisticsWindowVisible)
                {
                    Console.WriteLine("Statistics window is already hidden.");
                    return;
                }

                Console.WriteLine("Hiding statistics window...");
                await _uiComponents.HideDetailedStatsAsync();
                
                Console.WriteLine("Statistics window has been hidden.");
                
                _logger.LogInformation("Statistics window hidden via CLI command");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding statistics window: {ex.Message}");
                _logger.LogError(ex, "Hide stats command failed");
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
                    // Show current theme and available options
                    Console.WriteLine($"Current theme: {_uiComponents.CurrentTheme}");
                    Console.WriteLine("Available themes: auto, light, dark, highcontrast");
                    Console.WriteLine("Use --set to change theme");
                    return;
                }

                var theme = EnumExtensions.ParseWindowsTheme(themeString);
                
                Console.WriteLine($"Applying {theme} theme to UI components...");
                await _uiComponents.ApplyThemeAsync(theme);
                
                Console.WriteLine($"Theme successfully applied: {theme}");
                
                _logger.LogInformation($"UI theme changed via CLI to: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
                _logger.LogError(ex, "Theme command failed");
            }
        }

        /// <summary>
        /// Execute position command
        /// </summary>
        private async Task ExecutePositionCommandAsync(string? positionString)
        {
            try
            {
                if (string.IsNullOrEmpty(positionString))
                {
                    Console.WriteLine("Position format: 'x,y' or 'x,y,width,height'");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  --set 100,100        Position at (100,100)");
                    Console.WriteLine("  --set 200,150,600,450  Position and size");
                    return;
                }

                var position = ParsePositionCoordinates(positionString);
                
                Console.WriteLine($"Positioning window at ({position.X}, {position.Y}) with size ({position.Width}, {position.Height})");
                await _uiComponents.PositionWindowAsync(position);
                
                Console.WriteLine("Window position updated successfully.");
                
                _logger.LogInformation($"Window positioned via CLI to: {position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error positioning window: {ex.Message}");
                _logger.LogError(ex, "Position command failed");
            }
        }

        /// <summary>
        /// Execute update command
        /// </summary>
        private async Task ExecuteUpdateCommandAsync(double downloadSpeed, double uploadSpeed, string adapterName)
        {
            try
            {
                if (!_uiComponents.IsStatisticsWindowVisible)
                {
                    Console.WriteLine("Warning: Statistics window is not visible. Use 'display-stats' command first.");
                    return;
                }

                Console.WriteLine($"Updating statistics with test data:");
                Console.WriteLine($"  Download: {downloadSpeed:F0} B/s ({SpeedReading.FromBytesPerSecond(downloadSpeed)})");
                Console.WriteLine($"  Upload: {uploadSpeed:F0} B/s ({SpeedReading.FromBytesPerSecond(uploadSpeed)})");
                Console.WriteLine($"  Adapter: {adapterName}");

                var trafficData = new NetworkTrafficData(
                    bytesReceived: (long)(downloadSpeed * 300), // Simulate 5 minutes of data
                    bytesSent: (long)(uploadSpeed * 300),
                    receiveSpeed: downloadSpeed,
                    sendSpeed: uploadSpeed,
                    adapterName: adapterName,
                    adapterType: System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                );

                await _uiComponents.UpdateStatisticsAsync(trafficData);

                Console.WriteLine("Statistics display updated successfully.");
                
                _logger.LogInformation($"Statistics updated via CLI: {downloadSpeed:F0}/{uploadSpeed:F0} B/s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating statistics: {ex.Message}");
                _logger.LogError(ex, "Update command failed");
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

                var isWindowVisible = _uiComponents.IsStatisticsWindowVisible;
                var currentTheme = _uiComponents.CurrentTheme;
                var averagePerformance = _uiComponents.GetAveragePerformanceMs();

                if (format.ToLowerInvariant() == "json")
                {
                    var status = new
                    {
                        IsStatisticsWindowVisible = isWindowVisible,
                        CurrentTheme = currentTheme.ToString(),
                        AveragePerformanceMs = averagePerformance,
                        Timestamp = DateTime.Now
                    };

                    var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine("UI Components Status");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Statistics Window: {(isWindowVisible ? "Visible" : "Hidden")}");
                    Console.WriteLine($"Current Theme: {currentTheme}");
                    Console.WriteLine($"Average Response Time: {averagePerformance:F1}ms");
                    Console.WriteLine($"Status Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                }

                _logger.LogDebug($"UI status reported via CLI in {format} format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting UI status: {ex.Message}");
                _logger.LogError(ex, "Status command failed");
            }
        }

        /// <summary>
        /// Execute initialize command
        /// </summary>
        private async Task ExecuteInitializeCommandAsync(string themeString)
        {
            try
            {
                var theme = EnumExtensions.ParseWindowsTheme(themeString);
                
                Console.WriteLine($"Initializing UI components with {theme} theme...");

                // Apply theme first
                await _uiComponents.ApplyThemeAsync(theme);
                
                // Initialize UI components
                await _uiComponents.InitializeAsync();

                Console.WriteLine("UI components initialized successfully.");
                Console.WriteLine($"Applied theme: {_uiComponents.CurrentTheme}");
                
                _logger.LogInformation($"UI components initialized via CLI with theme: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing UI components: {ex.Message}");
                _logger.LogError(ex, "Initialize command failed");
            }
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Execute error display command for testing
        /// </summary>
        private Command CreateErrorCommand()
        {
            var command = new Command("error", "Show test error dialog");

            var messageOption = new Option<string>(
                aliases: new[] { "--message", "-m" },
                description: "Error message to display",
                getDefaultValue: () => "This is a test error message"
            );

            var detailsOption = new Option<string?>(
                aliases: new[] { "--details", "-d" },
                description: "Error details/exception message"
            );

            command.AddOption(messageOption);
            command.AddOption(detailsOption);

            command.SetHandler(async (context) =>
            {
                var message = context.ParseResult.GetValueForOption(messageOption)!;
                var details = context.ParseResult.GetValueForOption(detailsOption);

                await ExecuteErrorCommandAsync(message, details);
            });

            return command;
        }

        /// <summary>
        /// Execute error display command
        /// </summary>
        private async Task ExecuteErrorCommandAsync(string message, string? details)
        {
            try
            {
                Console.WriteLine($"Showing error dialog: {message}");

                Exception? exception = null;
                if (!string.IsNullOrEmpty(details))
                {
                    exception = new Exception(details);
                }

                await _uiComponents.ShowErrorAsync(message, exception);

                Console.WriteLine("Error dialog displayed successfully.");
                
                _logger.LogInformation($"Error dialog shown via CLI: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing error dialog: {ex.Message}");
                _logger.LogError(ex, "Error command failed");
            }
        }

        /// <summary>
        /// Execute interaction test command
        /// </summary>
        private async Task ExecuteInteractionTestCommandAsync(string interactionType, string elementName, string? data)
        {
            try
            {
                Console.WriteLine($"Testing interaction: {interactionType} on {elementName}");

                var interactionArgs = new UIInteractionEventArgs
                {
                    InteractionType = interactionType,
                    ElementName = elementName,
                    InteractionData = data ?? "CLI test data",
                    Timestamp = DateTime.Now
                };

                await _uiComponents.HandleInteractionAsync(interactionArgs);

                Console.WriteLine("Interaction handled successfully.");
                
                _logger.LogInformation($"Interaction tested via CLI: {interactionType} on {elementName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling interaction: {ex.Message}");
                _logger.LogError(ex, "Interaction test failed");
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
                    return new Rectangle(x, y, 600, 450); // Default window size
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
                    throw new ArgumentException("Position must be in format 'x,y' or 'x,y,width,height'");
                }
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid position format. Use integers separated by commas.");
            }
        }

        #endregion

        #region Static Entry Point

        /// <summary>
        /// Main entry point for UIComponents CLI
        /// Usage: NetworkUsage.exe ui [command] [options]
        /// </summary>
        public static async Task<int> RunAsync(string[] args, UIComponentsService? uiComponents = null, ILogger<UIComponentsCLI>? logger = null)
        {
            try
            {
                // Create default services if not provided
                if (logger == null)
                {
                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    logger = loggerFactory.CreateLogger<UIComponentsCLI>();
                }

                if (uiComponents == null)
                {
                    uiComponents = new UIComponentsService(
                        LoggerFactory.Create(builder => builder.AddConsole())
                            .CreateLogger<UIComponentsService>());
                }

                var rootCommand = CreateCommand(uiComponents, logger);
                
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
            Console.WriteLine("UIComponents CLI - User Interface Management Tool");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  display-stats    Show detailed network statistics window");
            Console.WriteLine("    --theme, -t    Theme to apply (auto|light|dark|highcontrast) [default: auto]");
            Console.WriteLine("    --position, -p Window position as 'x,y' coordinates");
            Console.WriteLine();
            Console.WriteLine("  hide-stats       Hide detailed statistics window");
            Console.WriteLine();
            Console.WriteLine("  theme            Manage UI theme");
            Console.WriteLine("    --set, -s      Set theme (auto|light|dark|highcontrast)");
            Console.WriteLine();
            Console.WriteLine("  position         Manage statistics window position");
            Console.WriteLine("    --set, -s      Set position as 'x,y,width,height' or 'x,y'");
            Console.WriteLine();
            Console.WriteLine("  update           Update statistics display with test data");
            Console.WriteLine("    --download, -d Download speed in bytes/sec [default: 1500000]");
            Console.WriteLine("    --upload, -u   Upload speed in bytes/sec [default: 750000]");
            Console.WriteLine("    --adapter, -a  Adapter name [default: 'CLI Test Adapter']");
            Console.WriteLine();
            Console.WriteLine("  status           Get UI components status");
            Console.WriteLine("    --format, -f   Output format (json|text) [default: text]");
            Console.WriteLine();
            Console.WriteLine("  initialize       Initialize UI components");
            Console.WriteLine("    --theme, -t    Initial theme (auto|light|dark|highcontrast) [default: auto]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ui display-stats --theme dark --position 100,100");
            Console.WriteLine("  ui update --download 5000000 --upload 2000000");
            Console.WriteLine("  ui theme --set light");
            Console.WriteLine("  ui position --set 300,200,800,600");
            Console.WriteLine("  ui status --format json");
        }

        #endregion
    }
}
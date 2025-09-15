using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;
using NetworkUsage.Services;

namespace NetworkUsage.CLI
{
    /// <summary>
    /// Command-line interface for NetworkMonitor library
    /// Constitutional requirement: Each library must have CLI interface
    /// Provides: --monitor --adapter [name] --format [json|text] --interval [ms]
    /// </summary>
    public class NetworkMonitorCLI
    {
        private readonly NetworkMonitorService _networkMonitor;
        private readonly ILogger<NetworkMonitorCLI> _logger;

        public NetworkMonitorCLI(NetworkMonitorService networkMonitor, ILogger<NetworkMonitorCLI> logger)
        {
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create the CLI command structure for NetworkMonitor
        /// </summary>
        public static Command CreateCommand(NetworkMonitorService networkMonitor, ILogger<NetworkMonitorCLI> logger)
        {
            var cli = new NetworkMonitorCLI(networkMonitor, logger);
            
            var rootCommand = new Command("network-monitor", "Network monitoring CLI commands");

            // Monitor command
            var monitorCommand = cli.CreateMonitorCommand();
            rootCommand.AddCommand(monitorCommand);

            // List adapters command
            var listCommand = cli.CreateListAdaptersCommand();
            rootCommand.AddCommand(listCommand);

            // Set adapter command
            var setAdapterCommand = cli.CreateSetAdapterCommand();
            rootCommand.AddCommand(setAdapterCommand);

            // Status command
            var statusCommand = cli.CreateStatusCommand();
            rootCommand.AddCommand(statusCommand);

            // Stop command
            var stopCommand = cli.CreateStopCommand();
            rootCommand.AddCommand(stopCommand);

            return rootCommand;
        }

        #region Command Creation Methods

        /// <summary>
        /// Create monitor command: --monitor --format [json|text] --interval [ms] --duration [seconds]
        /// </summary>
        private Command CreateMonitorCommand()
        {
            var command = new Command("monitor", "Start network monitoring");

            var formatOption = new Option<string>(
                aliases: new[] { "--format", "-f" },
                description: "Output format (json|text)",
                getDefaultValue: () => "text"
            );

            var intervalOption = new Option<int>(
                aliases: new[] { "--interval", "-i" },
                description: "Update interval in milliseconds (500-10000)",
                getDefaultValue: () => 1000
            );

            var durationOption = new Option<int>(
                aliases: new[] { "--duration", "-d" },
                description: "Monitoring duration in seconds (0 = infinite)",
                getDefaultValue: () => 0
            );

            var outputOption = new Option<FileInfo?>(
                aliases: new[] { "--output", "-o" },
                description: "Output file path (default: console)"
            );

            command.AddOption(formatOption);
            command.AddOption(intervalOption);
            command.AddOption(durationOption);
            command.AddOption(outputOption);

            command.SetHandler(async (context) =>
            {
                var format = context.ParseResult.GetValueForOption(formatOption)!;
                var interval = context.ParseResult.GetValueForOption(intervalOption);
                var duration = context.ParseResult.GetValueForOption(durationOption);
                var outputFile = context.ParseResult.GetValueForOption(outputOption);

                await ExecuteMonitorCommandAsync(format, interval, duration, outputFile);
            });

            return command;
        }

        /// <summary>
        /// Create list adapters command: list-adapters --format [json|text]
        /// </summary>
        private Command CreateListAdaptersCommand()
        {
            var command = new Command("list-adapters", "List all available network adapters");

            var formatOption = new Option<string>(
                aliases: new[] { "--format", "-f" },
                description: "Output format (json|text)",
                getDefaultValue: () => "text"
            );

            command.AddOption(formatOption);

            command.SetHandler(async (context) =>
            {
                var format = context.ParseResult.GetValueForOption(formatOption)!;
                await ExecuteListAdaptersCommandAsync(format);
            });

            return command;
        }

        /// <summary>
        /// Create set adapter command: set-adapter --adapter [id] --name [name]
        /// </summary>
        private Command CreateSetAdapterCommand()
        {
            var command = new Command("set-adapter", "Set the active network adapter");

            var adapterIdOption = new Option<string>(
                aliases: new[] { "--adapter", "-a" },
                description: "Adapter ID to set as active"
            ) { IsRequired = true };

            var adapterNameOption = new Option<string?>(
                aliases: new[] { "--name", "-n" },
                description: "Adapter name (alternative to ID)"
            );

            command.AddOption(adapterIdOption);
            command.AddOption(adapterNameOption);

            command.SetHandler(async (context) =>
            {
                var adapterId = context.ParseResult.GetValueForOption(adapterIdOption)!;
                var adapterName = context.ParseResult.GetValueForOption(adapterNameOption);

                await ExecuteSetAdapterCommandAsync(adapterId, adapterName);
            });

            return command;
        }

        /// <summary>
        /// Create status command: status --format [json|text]
        /// </summary>
        private Command CreateStatusCommand()
        {
            var command = new Command("status", "Get current monitoring status");

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
        /// Create stop command: stop
        /// </summary>
        private Command CreateStopCommand()
        {
            var command = new Command("stop", "Stop network monitoring");

            command.SetHandler(async (context) =>
            {
                await ExecuteStopCommandAsync();
            });

            return command;
        }

        #endregion

        #region Command Execution Methods

        /// <summary>
        /// Execute monitor command
        /// </summary>
        private async Task ExecuteMonitorCommandAsync(string format, int intervalMs, int durationSeconds, FileInfo? outputFile)
        {
            try
            {
                // Validate parameters
                if (intervalMs < 500 || intervalMs > 10000)
                {
                    Console.WriteLine("Error: Interval must be between 500 and 10000 milliseconds");
                    return;
                }

                if (!IsValidFormat(format))
                {
                    Console.WriteLine("Error: Format must be 'json' or 'text'");
                    return;
                }

                Console.WriteLine($"Starting network monitoring (format: {format}, interval: {intervalMs}ms)");

                // Set update interval
                await _networkMonitor.SetUpdateIntervalAsync(TimeSpan.FromMilliseconds(intervalMs));

                // Set up output stream
                TextWriter output = outputFile != null ? new StreamWriter(outputFile.FullName) : Console.Out;

                try
                {
                    // Set up event handler for traffic data
                    _networkMonitor.TrafficDataUpdated += (sender, data) =>
                    {
                        var outputText = format.ToLowerInvariant() switch
                        {
                            "json" => FormatTrafficDataAsJson(data),
                            "text" => FormatTrafficDataAsText(data),
                            _ => FormatTrafficDataAsText(data)
                        };

                        output.WriteLine(outputText);
                        output.Flush();
                    };

                    // Start monitoring
                    await _networkMonitor.StartMonitoringAsync();

                    if (format == "text")
                    {
                        Console.WriteLine("Monitoring started. Press Ctrl+C to stop.");
                        Console.WriteLine("Timestamp\t\tAdapter\t\tDown\t\tUp");
                        Console.WriteLine(new string('-', 80));
                    }

                    // Monitor for specified duration or until cancellation
                    if (durationSeconds > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
                        await _networkMonitor.StopMonitoringAsync();
                        Console.WriteLine("Monitoring completed.");
                    }
                    else
                    {
                        Console.WriteLine("Monitoring continuously. Press Ctrl+C to stop.");
                        // In a real CLI app, this would wait for Ctrl+C
                        await Task.Delay(TimeSpan.FromMinutes(60)); // Demo: run for 1 hour max
                    }
                }
                finally
                {
                    if (output != Console.Out)
                    {
                        await output.DisposeAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during monitoring: {ex.Message}");
                _logger.LogError(ex, "Monitor command failed");
            }
        }

        /// <summary>
        /// Execute list adapters command
        /// </summary>
        private async Task ExecuteListAdaptersCommandAsync(string format)
        {
            try
            {
                if (!IsValidFormat(format))
                {
                    Console.WriteLine("Error: Format must be 'json' or 'text'");
                    return;
                }

                var adapters = await _networkMonitor.GetAvailableAdaptersAsync();

                if (format.ToLowerInvariant() == "json")
                {
                    var json = JsonSerializer.Serialize(adapters, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine("Available Network Adapters:");
                    Console.WriteLine(new string('=', 50));
                    
                    foreach (var adapter in adapters.OrderByDescending(a => a.GetPriority()))
                    {
                        var activeMarker = adapter.IsActive ? "[ACTIVE] " : "";
                        Console.WriteLine($"{activeMarker}{adapter.Name}");
                        Console.WriteLine($"  ID: {adapter.Id}");
                        Console.WriteLine($"  Type: {adapter.Type}");
                        Console.WriteLine($"  Status: {adapter.GetStatusDescription()}");
                        Console.WriteLine($"  Speed: {adapter.GetSpeedDescription()}");
                        
                        if (!string.IsNullOrEmpty(adapter.IPv4Address))
                        {
                            Console.WriteLine($"  IP: {adapter.IPv4Address}");
                        }
                        
                        if (!string.IsNullOrEmpty(adapter.MacAddress))
                        {
                            Console.WriteLine($"  MAC: {adapter.MacAddress}");
                        }
                        
                        Console.WriteLine();
                    }
                }

                _logger.LogDebug($"Listed {adapters.Count()} adapters in {format} format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing adapters: {ex.Message}");
                _logger.LogError(ex, "List adapters command failed");
            }
        }

        /// <summary>
        /// Execute set adapter command
        /// </summary>
        private async Task ExecuteSetAdapterCommandAsync(string adapterId, string? adapterName)
        {
            try
            {
                string actualAdapterId = adapterId;

                // If adapter name is provided, find ID by name
                if (!string.IsNullOrEmpty(adapterName))
                {
                    var adapters = await _networkMonitor.GetAvailableAdaptersAsync();
                    var matchingAdapter = adapters.FirstOrDefault(a => 
                        a.Name.Equals(adapterName, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingAdapter != null)
                    {
                        actualAdapterId = matchingAdapter.Id;
                    }
                    else
                    {
                        Console.WriteLine($"Error: No adapter found with name '{adapterName}'");
                        return;
                    }
                }

                await _networkMonitor.SetActiveAdapterAsync(actualAdapterId);
                
                var activeAdapter = _networkMonitor.GetActiveAdapter();
                Console.WriteLine($"Active adapter set to: {activeAdapter.Name} ({actualAdapterId})");
                
                _logger.LogInformation($"Active adapter changed via CLI to: {activeAdapter.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting active adapter: {ex.Message}");
                _logger.LogError(ex, "Set adapter command failed");
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

                var isMonitoring = _networkMonitor.IsMonitoring;
                var currentTraffic = isMonitoring ? await _networkMonitor.GetCurrentTrafficAsync() : null;
                var activeAdapter = _networkMonitor.GetActiveAdapter();
                var averagePerformance = _networkMonitor.GetAveragePerformanceMs();

                if (format.ToLowerInvariant() == "json")
                {
                    var status = new
                    {
                        IsMonitoring = isMonitoring,
                        ActiveAdapter = activeAdapter,
                        CurrentTraffic = currentTraffic,
                        AveragePerformanceMs = averagePerformance,
                        Timestamp = DateTime.Now
                    };

                    var json = JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine("Network Monitor Status");
                    Console.WriteLine(new string('=', 30));
                    Console.WriteLine($"Monitoring: {(isMonitoring ? "Active" : "Stopped")}");
                    
                    if (activeAdapter != null)
                    {
                        Console.WriteLine($"Active Adapter: {activeAdapter.Name}");
                        Console.WriteLine($"Adapter Type: {activeAdapter.Type}");
                        Console.WriteLine($"Adapter Status: {activeAdapter.GetStatusDescription()}");
                        Console.WriteLine($"Speed Capability: {activeAdapter.GetSpeedDescription()}");
                        
                        if (!string.IsNullOrEmpty(activeAdapter.IPv4Address))
                        {
                            Console.WriteLine($"IP Address: {activeAdapter.IPv4Address}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Active Adapter: None");
                    }

                    if (currentTraffic != null)
                    {
                        var downloadSpeed = SpeedReading.FromBytesPerSecond(currentTraffic.ReceiveSpeed);
                        var uploadSpeed = SpeedReading.FromBytesPerSecond(currentTraffic.SendSpeed);
                        
                        Console.WriteLine($"Current Download: {downloadSpeed}");
                        Console.WriteLine($"Current Upload: {uploadSpeed}");
                        Console.WriteLine($"Total Downloaded: {FormatBytes(currentTraffic.BytesReceived)}");
                        Console.WriteLine($"Total Uploaded: {FormatBytes(currentTraffic.BytesSent)}");
                        Console.WriteLine($"Last Update: {currentTraffic.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    }

                    Console.WriteLine($"Average Response Time: {averagePerformance:F1}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting status: {ex.Message}");
                _logger.LogError(ex, "Status command failed");
            }
        }

        /// <summary>
        /// Execute stop command
        /// </summary>
        private async Task ExecuteStopCommandAsync()
        {
            try
            {
                if (!_networkMonitor.IsMonitoring)
                {
                    Console.WriteLine("Network monitoring is not currently active.");
                    return;
                }

                await _networkMonitor.StopMonitoringAsync();
                Console.WriteLine("Network monitoring stopped successfully.");
                
                _logger.LogInformation("Network monitoring stopped via CLI command");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping monitoring: {ex.Message}");
                _logger.LogError(ex, "Stop command failed");
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
        /// Format traffic data as JSON
        /// </summary>
        private static string FormatTrafficDataAsJson(NetworkTrafficData data)
        {
            var output = new
            {
                Timestamp = data.Timestamp,
                Adapter = new
                {
                    Name = data.AdapterName,
                    Type = data.AdapterType.ToString()
                },
                Traffic = new
                {
                    DownloadSpeedBps = data.ReceiveSpeed,
                    UploadSpeedBps = data.SendSpeed,
                    DownloadSpeedFormatted = SpeedReading.FromBytesPerSecond(data.ReceiveSpeed).FormattedString,
                    UploadSpeedFormatted = SpeedReading.FromBytesPerSecond(data.SendSpeed).FormattedString,
                    TotalBytesReceived = data.BytesReceived,
                    TotalBytesSent = data.BytesSent
                }
            };

            return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = false });
        }

        /// <summary>
        /// Format traffic data as human-readable text
        /// </summary>
        private static string FormatTrafficDataAsText(NetworkTrafficData data)
        {
            var downloadSpeed = SpeedReading.FromBytesPerSecond(data.ReceiveSpeed);
            var uploadSpeed = SpeedReading.FromBytesPerSecond(data.SendSpeed);
            
            return $"{data.Timestamp:HH:mm:ss}\t{data.AdapterName}\t{downloadSpeed}\t{uploadSpeed}";
        }

        /// <summary>
        /// Format byte count with appropriate units
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            var speedReading = SpeedReading.FromBytesPerSecond(bytes); // Reuse formatting logic
            return speedReading.FormattedString.Replace("/s", ""); // Remove per-second suffix
        }

        #endregion

        #region Static Entry Point

        /// <summary>
        /// Main entry point for NetworkMonitor CLI
        /// Usage: NetworkMonitor.exe network-monitor [command] [options]
        /// </summary>
        public static async Task<int> RunAsync(string[] args, NetworkMonitorService? networkMonitor = null, ILogger<NetworkMonitorCLI>? logger = null)
        {
            try
            {
                // Create default services if not provided
                if (logger == null)
                {
                    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    logger = loggerFactory.CreateLogger<NetworkMonitorCLI>();
                }

                if (networkMonitor == null)
                {
                    networkMonitor = new NetworkMonitorService(
                        LoggerFactory.Create(builder => builder.AddConsole())
                            .CreateLogger<NetworkMonitorService>());
                }

                var rootCommand = CreateCommand(networkMonitor, logger);
                
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
            Console.WriteLine("NetworkMonitor CLI - Network Traffic Monitoring Tool");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  monitor          Start network monitoring");
            Console.WriteLine("    --format, -f   Output format (json|text) [default: text]");
            Console.WriteLine("    --interval, -i Update interval in ms (500-10000) [default: 1000]");
            Console.WriteLine("    --duration, -d Duration in seconds (0=infinite) [default: 0]");
            Console.WriteLine("    --output, -o   Output file path [default: console]");
            Console.WriteLine();
            Console.WriteLine("  list-adapters    List all available network adapters");
            Console.WriteLine("    --format, -f   Output format (json|text) [default: text]");
            Console.WriteLine();
            Console.WriteLine("  set-adapter      Set active network adapter");
            Console.WriteLine("    --adapter, -a  Adapter ID (required)");
            Console.WriteLine("    --name, -n     Adapter name (alternative to ID)");
            Console.WriteLine();
            Console.WriteLine("  status           Get current monitoring status");
            Console.WriteLine("    --format, -f   Output format (json|text) [default: text]");
            Console.WriteLine();
            Console.WriteLine("  stop             Stop network monitoring");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  network-monitor monitor --format json --interval 2000");
            Console.WriteLine("  network-monitor list-adapters");
            Console.WriteLine("  network-monitor set-adapter --name \"Ethernet\"");
            Console.WriteLine("  network-monitor status --format json");
        }

        #endregion
    }
}
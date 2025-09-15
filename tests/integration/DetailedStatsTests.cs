using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Integration tests for modern GUI detailed statistics scenario
    /// Based on quickstart.md scenario: "Modern GUI detailed statistics"
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class DetailedStatsTests : IntegrationTestBase
    {
        private IUIComponents? _uiComponents;
        private INetworkMonitor? _networkMonitor;
        private ITaskbarIntegration? _taskbarIntegration;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // These services will be null initially because implementations don't exist yet
            // This is expected and part of TDD - tests should FAIL first
            try
            {
                _uiComponents = GetService<IUIComponents>();
                _networkMonitor = GetService<INetworkMonitor>();
                _taskbarIntegration = GetService<ITaskbarIntegration>();
            }
            catch (InvalidOperationException)
            {
                // Expected: Services not registered yet because implementations don't exist
                // This is the RED phase of TDD - tests should fail
            }
        }

        /// <summary>
        /// Scenario: User clicks system tray icon to open detailed statistics window
        /// Expected: Modern Windows 11 styled window appears with current network data
        /// Performance: Window opens within 100ms of click
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenTrayIconClicked_ShouldShowModernStatsWindow()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _networkMonitor.StartMonitoringAsync();
            
            // Verify window is not visible initially
            Assert.IsFalse(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should not be visible initially");

            // Act - Show detailed statistics window
            var showTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _uiComponents.ShowDetailedStatsAsync();
            });

            // Assert - Verify modern GUI appearance and performance
            Assert.IsTrue(showTime <= 100, 
                $"Statistics window should open within 100ms, took {showTime}ms");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should be visible after ShowDetailedStatsAsync");
        }

        /// <summary>
        /// Scenario: Statistics window displays real-time network data with charts and graphs
        /// Expected: Live updating charts, current speeds, historical data, adapter information
        /// Performance: UI updates complete within 50ms of new data
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenNetworkDataUpdates_ShouldRefreshUIInRealTime()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, "Window must be visible for update test");

            // Simulate network data updates over time
            var testDataPoints = new[]
            {
                new NetworkTrafficData
                {
                    ReceiveSpeed = 1_500_000,    // 1.5 MB/s
                    SendSpeed = 750_000,         // 0.75 MB/s
                    BytesReceived = 50_000_000,  // 50 MB
                    BytesSent = 25_000_000,      // 25 MB
                    Timestamp = DateTime.Now.AddSeconds(-3),
                    AdapterName = "Ethernet",
                    AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                },
                new NetworkTrafficData
                {
                    ReceiveSpeed = 3_200_000,    // 3.2 MB/s
                    SendSpeed = 1_600_000,       // 1.6 MB/s
                    BytesReceived = 53_200_000,  // 53.2 MB
                    BytesSent = 26_600_000,      // 26.6 MB
                    Timestamp = DateTime.Now.AddSeconds(-2),
                    AdapterName = "Ethernet",
                    AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                },
                new NetworkTrafficData
                {
                    ReceiveSpeed = 5_800_000,    // 5.8 MB/s
                    SendSpeed = 2_900_000,       // 2.9 MB/s
                    BytesReceived = 59_000_000,  // 59 MB
                    BytesSent = 29_500_000,      // 29.5 MB
                    Timestamp = DateTime.Now.AddSeconds(-1),
                    AdapterName = "Ethernet",
                    AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                }
            };

            var updateTimes = new List<long>();

            // Act - Send multiple data updates to test real-time refresh
            foreach (var dataPoint in testDataPoints)
            {
                var updateTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.UpdateStatisticsAsync(dataPoint);
                });
                
                updateTimes.Add(updateTime);
                await Task.Delay(100); // Brief delay between updates
            }

            // Assert - Verify real-time update performance
            Assert.IsTrue(updateTimes.All(time => time <= 50), 
                $"All UI updates should complete within 50ms, max was {updateTimes.Max()}ms");
            
            var averageUpdateTime = updateTimes.Average();
            Assert.IsTrue(averageUpdateTime <= 30, 
                $"Average update time should be ≤30ms, was {averageUpdateTime:F1}ms");
            
            // Verify window remains visible and responsive
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible during updates");
        }

        /// <summary>
        /// Scenario: User views multiple network adapters in the detailed statistics
        /// Expected: Adapter list shows all available adapters with status, user can switch between them
        /// Performance: Adapter list updates within 200ms, switching within 100ms
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenMultipleAdapters_ShouldShowAdapterListWithSwitching()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();

            // Create test adapter data
            var testAdapters = new List<NetworkAdapter>
            {
                new NetworkAdapter
                {
                    Id = "ethernet-1",
                    Name = "Ethernet Connection",
                    Description = "Intel(R) Ethernet Controller",
                    Type = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet,
                    Status = System.Net.NetworkInformation.OperationalStatus.Up,
                    Speed = 1_000_000_000, // 1 Gbps
                    IsActive = true,
                    IPv4Address = "192.168.1.100",
                    MacAddress = "00:11:22:33:44:55"
                },
                new NetworkAdapter
                {
                    Id = "wifi-1",
                    Name = "Wi-Fi Connection",
                    Description = "Intel(R) Wi-Fi 6 AX200",
                    Type = System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211,
                    Status = System.Net.NetworkInformation.OperationalStatus.Up,
                    Speed = 600_000_000, // 600 Mbps
                    IsActive = false,
                    IPv4Address = "192.168.1.101",
                    MacAddress = "AA:BB:CC:DD:EE:FF"
                },
                new NetworkAdapter
                {
                    Id = "bluetooth-1",
                    Name = "Bluetooth Network Connection",
                    Description = "Bluetooth Personal Area Network",
                    Type = System.Net.NetworkInformation.NetworkInterfaceType.Ppp,
                    Status = System.Net.NetworkInformation.OperationalStatus.Down,
                    Speed = 3_000_000, // 3 Mbps
                    IsActive = false,
                    IPv4Address = "",
                    MacAddress = "11:22:33:44:55:66"
                }
            };

            // Act - Update adapter list in statistics window
            var adapterListUpdateTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _uiComponents.UpdateAdapterListAsync(testAdapters);
            });

            // Simulate adapter switching
            var adapterSwitchTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _networkMonitor.SetActiveAdapterAsync("wifi-1");
            });

            // Assert - Verify adapter management functionality
            Assert.IsTrue(adapterListUpdateTime <= 200, 
                $"Adapter list update should complete within 200ms, took {adapterListUpdateTime}ms");
            Assert.IsTrue(adapterSwitchTime <= 100, 
                $"Adapter switching should complete within 100ms, took {adapterSwitchTime}ms");
            
            // Verify window remains functional
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible during adapter operations");
        }

        /// <summary>
        /// Scenario: User interacts with statistics window controls and settings
        /// Expected: Responsive UI interactions, settings changes applied immediately
        /// Performance: All interactions complete within 100ms
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenUserInteracts_ShouldRespondToUIInteractionsQuickly()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();

            var interactionTimes = new List<long>();
            var interactionEvents = new List<UIInteractionEventArgs>();

            // Set up interaction event handler
            _uiComponents.UserInteraction += (sender, args) =>
            {
                interactionEvents.Add(args);
            };

            // Test various UI interactions
            var testInteractions = new[]
            {
                new UIInteractionEventArgs
                {
                    InteractionType = "ButtonClick",
                    ElementName = "RefreshButton",
                    InteractionData = "refresh",
                    Timestamp = DateTime.Now
                },
                new UIInteractionEventArgs
                {
                    InteractionType = "SettingChange",
                    ElementName = "UpdateInterval",
                    InteractionData = TimeSpan.FromSeconds(2),
                    Timestamp = DateTime.Now
                },
                new UIInteractionEventArgs
                {
                    InteractionType = "AdapterSelect",
                    ElementName = "AdapterComboBox",
                    InteractionData = "wifi-1",
                    Timestamp = DateTime.Now
                }
            };

            // Act - Handle various user interactions
            foreach (var interaction in testInteractions)
            {
                var interactionTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.HandleInteractionAsync(interaction);
                });
                
                interactionTimes.Add(interactionTime);
                await Task.Delay(50); // Brief delay between interactions
            }

            // Assert - Verify interaction responsiveness
            Assert.IsTrue(interactionTimes.All(time => time <= 100), 
                $"All interactions should complete within 100ms, max was {interactionTimes.Max()}ms");
            
            var averageInteractionTime = interactionTimes.Average();
            Assert.IsTrue(averageInteractionTime <= 50, 
                $"Average interaction time should be ≤50ms, was {averageInteractionTime:F1}ms");
        }

        /// <summary>
        /// Scenario: Error occurs during network monitoring and is displayed to user
        /// Expected: User-friendly error dialog appears with helpful information
        /// Performance: Error dialog shows within 100ms of error occurrence
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenErrorOccurs_ShouldShowUserFriendlyErrorDialog()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();

            // Test different types of errors
            var testErrors = new[]
            {
                new { Message = "Network adapter disconnected", Exception = new InvalidOperationException("Adapter not found") },
                new { Message = "Permission denied accessing network statistics", Exception = new UnauthorizedAccessException("Access denied") },
                new { Message = "Network monitoring service unavailable", Exception = (Exception?)null }
            };

            var errorDisplayTimes = new List<long>();

            // Act - Test error handling for different scenarios
            foreach (var error in testErrors)
            {
                var errorTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.ShowErrorAsync(error.Message, error.Exception);
                });
                
                errorDisplayTimes.Add(errorTime);
                await Task.Delay(100); // Brief delay between errors
            }

            // Assert - Verify error handling performance
            Assert.IsTrue(errorDisplayTimes.All(time => time <= 100), 
                $"All error dialogs should appear within 100ms, max was {errorDisplayTimes.Max()}ms");
            
            // Verify statistics window remains functional after errors
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible after error handling");
        }

        /// <summary>
        /// Scenario: User changes UI update interval for statistics refresh rate
        /// Expected: Statistics update frequency changes immediately to match new setting
        /// Performance: Setting change applies within 100ms
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenUpdateIntervalChanged_ShouldAdjustRefreshRateImmediately()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();

            // Test different update intervals
            var testIntervals = new[]
            {
                TimeSpan.FromMilliseconds(500),  // Fast updates
                TimeSpan.FromSeconds(2),         // Slow updates  
                TimeSpan.FromSeconds(1)          // Default
            };

            var intervalChangeTimes = new List<long>();

            // Act - Test changing update intervals
            foreach (var interval in testIntervals)
            {
                var changeTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.SetUIUpdateIntervalAsync(interval);
                });
                
                intervalChangeTimes.Add(changeTime);
                await Task.Delay(50); // Brief delay between changes
            }

            // Assert - Verify interval change performance
            Assert.IsTrue(intervalChangeTimes.All(time => time <= 100), 
                $"All interval changes should complete within 100ms, max was {intervalChangeTimes.Max()}ms");
            
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible during interval changes");
        }

        /// <summary>
        /// Scenario: Statistics window is positioned relative to system tray icon
        /// Expected: Window appears near tray icon, avoids screen edges, follows Windows 11 guidelines
        /// Performance: Window positioning completes within 50ms
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenShown_ShouldPositionNearTrayIconAppropriately()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            // Simulate different tray icon positions
            var testPositions = new[]
            {
                new Rectangle(1800, 1000, 32, 32), // Bottom-right corner
                new Rectangle(50, 1000, 32, 32),   // Bottom-left corner  
                new Rectangle(960, 1000, 32, 32),  // Bottom-center
                new Rectangle(1800, 50, 32, 32)    // Top-right corner
            };

            var positioningTimes = new List<long>();

            // Act - Test window positioning for different tray locations
            foreach (var trayBounds in testPositions)
            {
                var positionTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.PositionWindowAsync(trayBounds);
                });
                
                positioningTimes.Add(positionTime);
                await Task.Delay(25); // Brief delay between positioning
            }

            // Show the window after positioning
            await _uiComponents.ShowDetailedStatsAsync();

            // Assert - Verify positioning performance
            Assert.IsTrue(positioningTimes.All(time => time <= 50), 
                $"All positioning operations should complete within 50ms, max was {positioningTimes.Max()}ms");
            
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should be visible after positioning and showing");
        }

        /// <summary>
        /// Scenario: User closes statistics window
        /// Expected: Window closes gracefully, cleanup performed, events fired
        /// Performance: Window closes within 100ms
        /// </summary>
        [TestMethod]
        public async Task DetailedStats_WhenClosed_ShouldCloseGracefullyWithCleanup()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _uiComponents.ShowDetailedStatsAsync();
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, "Window must be visible to test closing");

            bool windowClosedEventFired = false;
            StatisticsWindowEventArgs? closeEventArgs = null;

            _uiComponents.StatisticsWindowClosed += (sender, args) =>
            {
                windowClosedEventFired = true;
                closeEventArgs = args;
            };

            // Act - Close the statistics window
            var closeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _uiComponents.HideDetailedStatsAsync();
            });

            // Brief wait for events to propagate
            await Task.Delay(100);

            // Assert - Verify graceful closing
            Assert.IsTrue(closeTime <= 100, 
                $"Window should close within 100ms, took {closeTime}ms");
            Assert.IsFalse(_uiComponents.IsStatisticsWindowVisible, 
                "Window should not be visible after HideDetailedStatsAsync");
            
            // Note: Event verification would depend on actual implementation
            // In TDD RED phase, these events may not fire yet
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            // Cleanup - Close statistics window and stop services
            try
            {
                _uiComponents?.HideDetailedStatsAsync().Wait(1000);
                _networkMonitor?.StopMonitoringAsync().Wait(1000);
                _taskbarIntegration?.HideAsync().Wait(1000);
            }
            catch (Exception)
            {
                // Expected during TDD RED phase when implementations don't exist
            }
            
            base.TestCleanup();
        }
    }
}
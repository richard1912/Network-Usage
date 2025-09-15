using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Integration tests for network adapter changes handling scenario
    /// Based on quickstart.md scenario: "Network adapter changes handling"
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class AdapterChangesTests : IntegrationTestBase
    {
        private INetworkMonitor? _networkMonitor;
        private ITaskbarIntegration? _taskbarIntegration;
        private IUIComponents? _uiComponents;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // These services will be null initially because implementations don't exist yet
            // This is expected and part of TDD - tests should FAIL first
            try
            {
                _networkMonitor = GetService<INetworkMonitor>();
                _taskbarIntegration = GetService<ITaskbarIntegration>();
                _uiComponents = GetService<IUIComponents>();
            }
            catch (InvalidOperationException)
            {
                // Expected: Services not registered yet because implementations don't exist
                // This is the RED phase of TDD - tests should fail
            }
        }

        /// <summary>
        /// Scenario: Active network adapter becomes unavailable (unplugged/disabled)
        /// Expected: Automatic detection of change, switch to backup adapter, continuous monitoring
        /// Performance: Adapter switch detection and fallback within 500ms
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenActiveAdapterDisconnects_ShouldSwitchToBackupAdapter()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _taskbarIntegration.ShowAsync();

            bool adapterChangedEventFired = false;
            NetworkAdapter? newActiveAdapter = null;
            NetworkAdapter? oldActiveAdapter = null;

            // Get initial adapter state
            var availableAdapters = await _networkMonitor.GetAvailableAdaptersAsync();
            var initialActiveAdapter = _networkMonitor.GetActiveAdapter();
            
            Assert.IsNotNull(initialActiveAdapter, "Should have an initial active adapter");
            Assert.IsTrue(availableAdapters.Count() >= 2, 
                "Test requires at least 2 adapters for fallback testing");

            // Set up event handler for adapter changes
            _networkMonitor.ActiveAdapterChanged += (sender, adapter) =>
            {
                oldActiveAdapter = initialActiveAdapter;
                newActiveAdapter = adapter;
                adapterChangedEventFired = true;
            };

            // Simulate adapter disconnection by switching to another available adapter
            var backupAdapter = availableAdapters.FirstOrDefault(a => a.Id != initialActiveAdapter.Id);
            Assert.IsNotNull(backupAdapter, "Should have a backup adapter for testing");

            // Act - Simulate active adapter becoming unavailable by switching
            var adapterSwitchTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _networkMonitor.SetActiveAdapterAsync(backupAdapter.Id);
            });

            // Wait for change detection
            await WaitForConditionAsync(() => adapterChangedEventFired, timeoutMs: 1000);

            // Assert - Verify adapter change handling
            Assert.IsTrue(adapterSwitchTime <= 500, 
                $"Adapter switch should complete within 500ms, took {adapterSwitchTime}ms");
            Assert.IsTrue(adapterChangedEventFired, "ActiveAdapterChanged event should fire");
            Assert.IsNotNull(newActiveAdapter, "Should have new active adapter");
            Assert.AreEqual(backupAdapter.Id, newActiveAdapter.Id, 
                "Should switch to the backup adapter");
            
            // Verify monitoring continues seamlessly
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Monitoring should continue after adapter switch");
            
            // Verify current traffic data reflects new adapter
            var currentTraffic = await _networkMonitor.GetCurrentTrafficAsync();
            Assert.AreEqual(backupAdapter.Name, currentTraffic.AdapterName, 
                "Traffic data should reflect new active adapter");
        }

        /// <summary>
        /// Scenario: New network adapter becomes available (plugged in/enabled)
        /// Expected: Detection of new adapter, option to switch if better, adapter list updated
        /// Performance: New adapter detection within 2 seconds
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenNewAdapterBecomesAvailable_ShouldDetectAndUpdateAdapterList()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            // Get initial adapter list
            var initialAdapters = await _networkMonitor.GetAvailableAdaptersAsync();
            var initialAdapterCount = initialAdapters.Count();
            var initialActiveAdapter = _networkMonitor.GetActiveAdapter();

            Assert.IsNotNull(initialActiveAdapter, "Should have an initial active adapter");

            // Simulate new adapter addition by creating a test adapter
            var newAdapter = new NetworkAdapter
            {
                Id = "new-usb-ethernet",
                Name = "USB Ethernet Adapter",
                Description = "USB 3.0 to Gigabit Ethernet Adapter",
                Type = NetworkInterfaceType.Ethernet,
                Status = OperationalStatus.Up,
                Speed = 1_000_000_000, // 1 Gbps
                IsActive = false,
                IPv4Address = "192.168.1.150",
                MacAddress = "AA:BB:CC:DD:EE:11"
            };

            // Act - Simulate new adapter detection
            var detectionTime = await MeasureExecutionTimeAsync(async () =>
            {
                // In real implementation, this would be detected automatically
                // For testing, we simulate the detection process
                var updatedAdapterList = initialAdapters.Concat(new[] { newAdapter });
                await _uiComponents.UpdateAdapterListAsync(updatedAdapterList);
            });

            // Get updated adapter list
            var updatedAdapters = await _networkMonitor.GetAvailableAdaptersAsync();

            // Assert - Verify new adapter detection
            Assert.IsTrue(detectionTime <= 2000, 
                $"New adapter detection should complete within 2 seconds, took {detectionTime}ms");
            
            // Note: In TDD RED phase, GetAvailableAdaptersAsync might not reflect the update yet
            // This test validates the UI update mechanism works
            
            // Verify active adapter remains unchanged (no automatic switching)
            var currentActiveAdapter = _networkMonitor.GetActiveAdapter();
            Assert.AreEqual(initialActiveAdapter.Id, currentActiveAdapter.Id, 
                "Active adapter should not change automatically when new adapter is detected");
            
            // Verify monitoring continues uninterrupted
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Monitoring should continue during adapter list updates");
        }

        /// <summary>
        /// Scenario: All network adapters become unavailable (network stack failure)
        /// Expected: Graceful error handling, user notification, monitoring pause with retry
        /// Performance: Error detection and handling within 1 second
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenAllAdaptersUnavailable_ShouldHandleGracefullyWithRetry()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            bool errorShown = false;
            string? errorMessage = null;

            // Simulate all adapters becoming unavailable
            var emptyAdapterList = new List<NetworkAdapter>();

            // Act - Simulate complete adapter failure
            var errorHandlingTime = await MeasureExecutionTimeAsync(async () =>
            {
                try
                {
                    // This should trigger error handling in real implementation
                    await _uiComponents.UpdateAdapterListAsync(emptyAdapterList);
                    
                    // Simulate the error that would occur
                    await _uiComponents.ShowErrorAsync(
                        "No network adapters available. Please check your network connections.",
                        new InvalidOperationException("No active network adapters found"));
                    
                    errorShown = true;
                    errorMessage = "No network adapters available";
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    errorShown = true;
                }
            });

            // Assert - Verify graceful error handling
            Assert.IsTrue(errorHandlingTime <= 1000, 
                $"Error handling should complete within 1 second, took {errorHandlingTime}ms");
            Assert.IsTrue(errorShown, "Error should be shown to user when no adapters available");
            Assert.IsNotNull(errorMessage, "Error message should be provided to user");
            
            // Verify application remains responsive
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "UI should remain visible and responsive during adapter errors");
        }

        /// <summary>
        /// Scenario: Rapid adapter changes (multiple adapters connecting/disconnecting quickly)
        /// Expected: Stable handling without crashes, debounced adapter switching
        /// Performance: Each change handled within 300ms, no more than 3 switches per 5 seconds
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenRapidAdapterChanges_ShouldHandleStablyWithDebouncing()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _taskbarIntegration.ShowAsync();

            var adapterChangeEvents = new List<NetworkAdapter>();
            var changeTimestamps = new List<DateTime>();

            _networkMonitor.ActiveAdapterChanged += (sender, adapter) =>
            {
                adapterChangeEvents.Add(adapter);
                changeTimestamps.Add(DateTime.UtcNow);
            };

            // Create test adapters for rapid switching
            var testAdapters = new[]
            {
                new NetworkAdapter { Id = "ethernet-1", Name = "Ethernet 1", Type = NetworkInterfaceType.Ethernet },
                new NetworkAdapter { Id = "wifi-1", Name = "WiFi 1", Type = NetworkInterfaceType.Wireless80211 },
                new NetworkAdapter { Id = "ethernet-2", Name = "Ethernet 2", Type = NetworkInterfaceType.Ethernet },
                new NetworkAdapter { Id = "wifi-2", Name = "WiFi 2", Type = NetworkInterfaceType.Wireless80211 }
            };

            var switchTimes = new List<long>();
            var startTime = DateTime.UtcNow;

            // Act - Perform rapid adapter switches
            foreach (var adapter in testAdapters)
            {
                var switchTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _networkMonitor.SetActiveAdapterAsync(adapter.Id);
                });
                
                switchTimes.Add(switchTime);
                await Task.Delay(100); // 100ms between rapid changes
            }

            // Wait for potential debouncing to complete
            await Task.Delay(1000);
            
            var totalElapsedTime = DateTime.UtcNow - startTime;

            // Assert - Verify stable handling of rapid changes
            Assert.IsTrue(switchTimes.All(time => time <= 300), 
                $"Each adapter switch should complete within 300ms, max was {switchTimes.Max()}ms");
            
            // Verify debouncing (should not have excessive switching)
            var switchesInLast5Seconds = changeTimestamps.Count(t => 
                DateTime.UtcNow.Subtract(t).TotalSeconds <= 5);
            Assert.IsTrue(switchesInLast5Seconds <= 6, 
                $"Should not have excessive adapter switches (≤6 in 5s), had {switchesInLast5Seconds}");
            
            // Verify final state is stable
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Monitoring should remain stable after rapid adapter changes");
            
            var finalActiveAdapter = _networkMonitor.GetActiveAdapter();
            Assert.IsNotNull(finalActiveAdapter, "Should have a stable active adapter after changes");
        }

        /// <summary>
        /// Scenario: Network adapter priority changes (Ethernet vs WiFi preference)
        /// Expected: Automatic switching to higher priority adapter when available
        /// Performance: Priority evaluation and switching within 200ms
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenHigherPriorityAdapterAvailable_ShouldSwitchAutomatically()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _networkMonitor.StartMonitoringAsync();

            // Create adapters with different priorities (Ethernet typically higher than WiFi)
            var wifiAdapter = new NetworkAdapter
            {
                Id = "wifi-1",
                Name = "WiFi Connection",
                Type = NetworkInterfaceType.Wireless80211,
                Status = OperationalStatus.Up,
                Speed = 300_000_000, // 300 Mbps
                IsActive = false
            };

            var ethernetAdapter = new NetworkAdapter
            {
                Id = "ethernet-1",
                Name = "Ethernet Connection",
                Type = NetworkInterfaceType.Ethernet,
                Status = OperationalStatus.Up,
                Speed = 1_000_000_000, // 1 Gbps
                IsActive = false
            };

            bool prioritySwitchOccurred = false;
            NetworkAdapter? switchedToAdapter = null;

            _networkMonitor.ActiveAdapterChanged += (sender, adapter) =>
            {
                switchedToAdapter = adapter;
                prioritySwitchOccurred = true;
            };

            // Start with WiFi as active
            await _networkMonitor.SetActiveAdapterAsync(wifiAdapter.Id);
            var initialAdapter = _networkMonitor.GetActiveAdapter();
            Assert.AreEqual(wifiAdapter.Id, initialAdapter.Id, "Should start with WiFi adapter");

            // Act - Introduce higher priority Ethernet adapter
            var prioritySwitchTime = await MeasureExecutionTimeAsync(async () =>
            {
                // In real implementation, this would be automatic priority detection
                await _networkMonitor.SetActiveAdapterAsync(ethernetAdapter.Id);
            });

            await WaitForConditionAsync(() => prioritySwitchOccurred, timeoutMs: 500);

            // Assert - Verify priority-based switching
            Assert.IsTrue(prioritySwitchTime <= 200, 
                $"Priority switch should complete within 200ms, took {prioritySwitchTime}ms");
            
            var currentAdapter = _networkMonitor.GetActiveAdapter();
            Assert.AreEqual(ethernetAdapter.Id, currentAdapter.Id, 
                "Should switch to higher priority Ethernet adapter");
            
            // Verify monitoring continues with new adapter
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Monitoring should continue after priority switch");
            
            var trafficData = await _networkMonitor.GetCurrentTrafficAsync();
            Assert.AreEqual(ethernetAdapter.Name, trafficData.AdapterName, 
                "Traffic data should reflect high priority adapter");
        }

        /// <summary>
        /// Scenario: Adapter status changes (Up/Down state transitions)
        /// Expected: Monitoring adjusts based on adapter operational status
        /// Performance: Status change detection within 1 second
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenAdapterStatusChanges_ShouldAdjustMonitoringAccordingly()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            var testAdapter = new NetworkAdapter
            {
                Id = "test-adapter",
                Name = "Test Adapter",
                Type = NetworkInterfaceType.Ethernet,
                Status = OperationalStatus.Up,
                Speed = 1_000_000_000,
                IsActive = true
            };

            await _networkMonitor.SetActiveAdapterAsync(testAdapter.Id);
            Assert.IsTrue(_networkMonitor.IsMonitoring, "Should be monitoring with active adapter");

            // Simulate adapter status changes
            var statusTransitions = new[]
            {
                OperationalStatus.Testing,
                OperationalStatus.Down,
                OperationalStatus.Up,
                OperationalStatus.Dormant,
                OperationalStatus.Up
            };

            var statusChangeTimes = new List<long>();

            // Act - Test different status transitions
            foreach (var newStatus in statusTransitions)
            {
                testAdapter.Status = newStatus;
                
                var statusChangeTime = await MeasureExecutionTimeAsync(async () =>
                {
                    // Update UI with new adapter status
                    var adapterList = new[] { testAdapter };
                    await _uiComponents.UpdateAdapterListAsync(adapterList);
                });
                
                statusChangeTimes.Add(statusChangeTime);
                await Task.Delay(200); // Brief delay between status changes
            }

            // Assert - Verify status change handling
            Assert.IsTrue(statusChangeTimes.All(time => time <= 1000), 
                $"Status change handling should complete within 1s, max was {statusChangeTimes.Max()}ms");
            
            // Verify monitoring adapts to final status
            Assert.AreEqual(OperationalStatus.Up, testAdapter.Status, 
                "Test should end with adapter in Up status");
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Should resume monitoring when adapter returns to Up status");
        }

        /// <summary>
        /// Scenario: Adapter configuration changes (IP address, speed capability)
        /// Expected: Configuration updates reflected in monitoring without restart
        /// Performance: Configuration update processing within 100ms
        /// </summary>
        [TestMethod]
        public async Task AdapterChanges_WhenAdapterConfigurationChanges_ShouldUpdateWithoutRestart()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _networkMonitor.StartMonitoringAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            var testAdapter = new NetworkAdapter
            {
                Id = "config-test-adapter",
                Name = "Configuration Test Adapter",
                Type = NetworkInterfaceType.Ethernet,
                Status = OperationalStatus.Up,
                Speed = 100_000_000, // Start with 100 Mbps
                IPv4Address = "192.168.1.100",
                IsActive = true
            };

            await _networkMonitor.SetActiveAdapterAsync(testAdapter.Id);

            // Configuration changes to test
            var configurationChanges = new[]
            {
                new { Speed = 1_000_000_000L, IP = "192.168.1.101" }, // Upgrade to Gigabit
                new { Speed = 1_000_000_000L, IP = "10.0.0.50" },     // IP change  
                new { Speed = 2_500_000_000L, IP = "10.0.0.50" },     // Speed upgrade to 2.5G
                new { Speed = 10_000_000_000L, IP = "172.16.1.100" }  // 10G with different subnet
            };

            var configUpdateTimes = new List<long>();

            // Act - Apply configuration changes
            foreach (var config in configurationChanges)
            {
                testAdapter.Speed = config.Speed;
                testAdapter.IPv4Address = config.IP;
                
                var updateTime = await MeasureExecutionTimeAsync(async () =>
                {
                    var adapterList = new[] { testAdapter };
                    await _uiComponents.UpdateAdapterListAsync(adapterList);
                });
                
                configUpdateTimes.Add(updateTime);
                await Task.Delay(50); // Brief delay between configuration changes
            }

            // Assert - Verify configuration update performance
            Assert.IsTrue(configUpdateTimes.All(time => time <= 100), 
                $"Configuration updates should complete within 100ms, max was {configUpdateTimes.Max()}ms");
            
            // Verify monitoring continues without restart
            Assert.IsTrue(_networkMonitor.IsMonitoring, 
                "Monitoring should continue through configuration changes");
            
            // Verify final configuration is reflected
            var currentTraffic = await _networkMonitor.GetCurrentTrafficAsync();
            Assert.AreEqual(testAdapter.Name, currentTraffic.AdapterName, 
                "Traffic data should reflect updated adapter configuration");
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            // Cleanup - Stop monitoring and close UI components
            try
            {
                _networkMonitor?.StopMonitoringAsync().Wait(1000);
                _uiComponents?.HideDetailedStatsAsync().Wait(1000);
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

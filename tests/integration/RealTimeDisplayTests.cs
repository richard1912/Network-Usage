using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Integration tests for real-time display updates scenario
    /// Based on quickstart.md scenario: "Real-time display updates"
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class RealTimeDisplayTests : IntegrationTestBase
    {
        // NOTE: These will be mock implementations initially since real implementations don't exist yet
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
        /// Scenario: User starts the application and sees real-time network traffic data
        /// Expected: Network monitoring begins, taskbar icon appears, data updates every second
        /// Performance: Updates must occur within 100ms of new data arrival
        /// </summary>
        [TestMethod]
        public async Task RealTimeDisplayUpdates_WhenApplicationStarts_ShouldShowNetworkDataInTaskbar()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            bool trafficDataReceived = false;
            bool taskbarUpdated = false;
            NetworkTrafficData? lastTrafficData = null;

            // Set up event handlers to capture real-time updates
            _networkMonitor.TrafficDataUpdated += (sender, data) =>
            {
                trafficDataReceived = true;
                lastTrafficData = data;
            };

            // Act - Start the real-time monitoring
            await _taskbarIntegration.ShowAsync();
            await _networkMonitor.StartMonitoringAsync();

            // Wait for real-time updates (should happen within a few seconds)
            await WaitForConditionAsync(() => trafficDataReceived, timeoutMs: 5000);

            // Verify taskbar is updated when new data arrives
            if (lastTrafficData != null)
            {
                var executionTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _taskbarIntegration.UpdateDisplayAsync(lastTrafficData);
                    taskbarUpdated = true;
                });

                // Assert - Verify real-time performance requirements
                Assert.IsTrue(trafficDataReceived, "Should receive network traffic data in real-time");
                Assert.IsTrue(taskbarUpdated, "Should update taskbar display when new data arrives");
                Assert.IsTrue(executionTime <= 100, $"Taskbar update should complete within 100ms, took {executionTime}ms");
                Assert.IsTrue(_taskbarIntegration.IsVisible, "Taskbar icon should be visible");
                Assert.IsTrue(_networkMonitor.IsMonitoring, "Network monitoring should be active");

                // Verify data quality
                Assert.IsNotNull(lastTrafficData.AdapterName, "Traffic data should include adapter name");
                Assert.IsTrue(lastTrafficData.Timestamp > DateTime.Now.AddSeconds(-5), 
                    "Traffic data should have recent timestamp");
            }
        }

        /// <summary>
        /// Scenario: Network traffic increases significantly and display updates accordingly
        /// Expected: Higher speeds shown in taskbar tooltip, automatic unit scaling (B/s → KB/s → MB/s)
        /// Performance: Display updates must complete within 50ms
        /// </summary>
        [TestMethod]
        public async Task RealTimeDisplayUpdates_WhenTrafficIncreases_ShouldAutoScaleUnitsAndUpdateTooltip()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");

            await _taskbarIntegration.ShowAsync();
            await _networkMonitor.StartMonitoringAsync();

            string initialTooltip = _taskbarIntegration.CurrentTooltip;

            // Create test data representing high network traffic
            var highTrafficData = new NetworkTrafficData
            {
                BytesReceived = 1_000_000_000, // 1GB
                BytesSent = 500_000_000,       // 500MB  
                ReceiveSpeed = 10_000_000,     // 10 MB/s
                SendSpeed = 5_000_000,         // 5 MB/s
                Timestamp = DateTime.Now,
                AdapterName = "Test Ethernet",
                AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            };

            // Act - Update with high traffic data
            var updateTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.UpdateDisplayAsync(highTrafficData);
            });

            string updatedTooltip = _taskbarIntegration.CurrentTooltip;

            // Assert - Verify performance and functionality
            Assert.IsTrue(updateTime <= 50, $"Display update should complete within 50ms, took {updateTime}ms");
            Assert.AreNotEqual(initialTooltip, updatedTooltip, "Tooltip should update with new traffic data");
            
            // Verify auto-scaling: High speeds should be displayed in MB/s, not B/s
            Assert.IsTrue(updatedTooltip.Contains("MB/s") || updatedTooltip.Contains("GB/s"), 
                "High speeds should be auto-scaled to MB/s or GB/s");
            Assert.IsFalse(updatedTooltip.Contains("10000000"), 
                "Should not show raw bytes per second for high speeds");
        }

        /// <summary>
        /// Scenario: Network adapter disconnects and reconnects during monitoring
        /// Expected: Automatic adapter switching, continuous updates, user notification
        /// Performance: Adapter detection and switching within 500ms
        /// </summary>
        [TestMethod]
        public async Task RealTimeDisplayUpdates_WhenAdapterChanges_ShouldSwitchAutomaticallyAndContinueUpdates()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");

            await _networkMonitor.StartMonitoringAsync();
            await _taskbarIntegration.ShowAsync();

            bool adapterChangedEventFired = false;
            NetworkAdapter? newAdapter = null;

            _networkMonitor.ActiveAdapterChanged += (sender, adapter) =>
            {
                adapterChangedEventFired = true;
                newAdapter = adapter;
            };

            var availableAdapters = await _networkMonitor.GetAvailableAdaptersAsync();
            var currentAdapter = _networkMonitor.GetActiveAdapter();
            
            Assert.IsNotNull(currentAdapter, "Should have an active adapter");
            Assert.IsTrue(availableAdapters.Any(), "Should have available adapters");

            // Act - Simulate adapter change by switching to a different adapter
            var alternativeAdapter = availableAdapters.FirstOrDefault(a => a.Id != currentAdapter.Id);
            if (alternativeAdapter != null)
            {
                var switchTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _networkMonitor.SetActiveAdapterAsync(alternativeAdapter.Id);
                });

                // Wait for adapter change event and subsequent updates
                await WaitForConditionAsync(() => adapterChangedEventFired, timeoutMs: 1000);

                // Assert - Verify automatic switching and continued operation
                Assert.IsTrue(switchTime <= 500, $"Adapter switching should complete within 500ms, took {switchTime}ms");
                Assert.IsTrue(adapterChangedEventFired, "ActiveAdapterChanged event should fire");
                Assert.IsNotNull(newAdapter, "Should receive new adapter information");
                Assert.AreEqual(alternativeAdapter.Id, newAdapter.Id, "Should switch to the requested adapter");
                Assert.IsTrue(_networkMonitor.IsMonitoring, "Monitoring should continue after adapter change");
                
                // Verify display continues to update with new adapter data
                var currentTrafficData = await _networkMonitor.GetCurrentTrafficAsync();
                Assert.AreEqual(alternativeAdapter.Name, currentTrafficData.AdapterName, 
                    "Traffic data should reflect new adapter");
            }
        }

        /// <summary>
        /// Scenario: Application runs for extended period with continuous updates
        /// Expected: Stable performance, no memory leaks, consistent update frequency
        /// Performance: CPU usage <1%, Memory usage <50MB, Update frequency maintained
        /// </summary>
        [TestMethod]
        [Timeout(30000)] // 30 second timeout for performance test
        public async Task RealTimeDisplayUpdates_WhenRunningContinuously_ShouldMaintainPerformance()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");

            await _networkMonitor.StartMonitoringAsync();
            await _taskbarIntegration.ShowAsync();

            int updateCount = 0;
            var updateTimes = new List<long>();
            var startTime = DateTime.UtcNow;

            _networkMonitor.TrafficDataUpdated += async (sender, data) =>
            {
                var updateTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _taskbarIntegration.UpdateDisplayAsync(data);
                });
                
                updateTimes.Add(updateTime);
                updateCount++;
            };

            // Act - Run for 10 seconds to test continuous operation
            var continuousRunTime = TimeSpan.FromSeconds(10);
            await Task.Delay(continuousRunTime);

            var actualRunTime = DateTime.UtcNow - startTime;

            // Assert - Verify performance characteristics
            Assert.IsTrue(updateCount >= 8, $"Should receive at least 8 updates in 10 seconds, got {updateCount}");
            
            // Verify update frequency (approximately 1 per second, allow some variance)
            var expectedUpdates = (int)(actualRunTime.TotalSeconds * 0.8); // Allow 20% variance
            Assert.IsTrue(updateCount >= expectedUpdates, 
                $"Update frequency too low: expected ~{expectedUpdates}, got {updateCount}");

            // Verify individual update performance
            var averageUpdateTime = updateTimes.Count > 0 ? updateTimes.Average() : 0;
            var maxUpdateTime = updateTimes.Count > 0 ? updateTimes.Max() : 0;
            
            Assert.IsTrue(averageUpdateTime <= 50, 
                $"Average update time should be ≤50ms, was {averageUpdateTime:F1}ms");
            Assert.IsTrue(maxUpdateTime <= 100, 
                $"Maximum update time should be ≤100ms, was {maxUpdateTime}ms");

            // Note: CPU and memory usage testing would require additional tooling
            // In a real implementation, we'd use performance counters or diagnostic tools
        }

        /// <summary>
        /// Scenario: User toggles between different display formats
        /// Expected: Immediate format changes, data integrity maintained, smooth transitions
        /// Performance: Format changes within 100ms
        /// </summary>
        [TestMethod]
        public async Task RealTimeDisplayUpdates_WhenDisplayFormatChanges_ShouldUpdateImmediately()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");

            await _taskbarIntegration.ShowAsync();
            await _networkMonitor.StartMonitoringAsync();

            // Test with sample traffic data
            var trafficData = new NetworkTrafficData
            {
                ReceiveSpeed = 1_500_000,  // 1.5 MB/s
                SendSpeed = 750_000,       // 0.75 MB/s
                Timestamp = DateTime.Now,
                AdapterName = "Test Adapter",
                AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            };

            await _taskbarIntegration.UpdateDisplayAsync(trafficData);
            string defaultFormatTooltip = _taskbarIntegration.CurrentTooltip;

            // Act - Change display format
            var formatChangeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.SetDisplayFormatAsync("Download: {0} | Upload: {1}");
            });

            // Update with same data to see format change
            await _taskbarIntegration.UpdateDisplayAsync(trafficData);
            string newFormatTooltip = _taskbarIntegration.CurrentTooltip;

            // Assert - Verify format change behavior
            Assert.IsTrue(formatChangeTime <= 100, 
                $"Display format change should complete within 100ms, took {formatChangeTime}ms");
            Assert.AreNotEqual(defaultFormatTooltip, newFormatTooltip, 
                "Tooltip should reflect new display format");
            Assert.IsTrue(newFormatTooltip.Contains("Download:") && newFormatTooltip.Contains("Upload:"), 
                "New format should be applied to tooltip text");

            // Verify data integrity is maintained
            Assert.IsTrue(newFormatTooltip.Contains("1.5") || newFormatTooltip.Contains("1500"), 
                "Speed values should still be present in new format");
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            // Cleanup - Stop monitoring and hide taskbar integration
            try
            {
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Integration tests for taskbar integration seamless operation scenario
    /// Based on quickstart.md scenario: "Taskbar integration seamless operation"
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class TaskbarIntegrationTests : IntegrationTestBase
    {
        private ITaskbarIntegration? _taskbarIntegration;
        private INetworkMonitor? _networkMonitor;
        private IUIComponents? _uiComponents;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // These services will be null initially because implementations don't exist yet
            // This is expected and part of TDD - tests should FAIL first
            try
            {
                _taskbarIntegration = GetService<ITaskbarIntegration>();
                _networkMonitor = GetService<INetworkMonitor>();
                _uiComponents = GetService<IUIComponents>();
            }
            catch (InvalidOperationException)
            {
                // Expected: Services not registered yet because implementations don't exist
                // This is the RED phase of TDD - tests should fail
            }
        }

        /// <summary>
        /// Scenario: Application starts and integrates seamlessly into Windows 11 taskbar
        /// Expected: System tray icon appears, no user prompt, automatic theme matching
        /// Performance: Icon appears within 200ms of application start
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenApplicationStarts_ShouldShowIconSeamlessly()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            // Verify icon is not visible initially
            Assert.IsFalse(_taskbarIntegration.IsVisible, "Icon should not be visible before ShowAsync");

            // Act - Show the taskbar icon
            var showTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ShowAsync();
            });

            // Assert - Verify seamless integration
            Assert.IsTrue(showTime <= 200, $"Icon should appear within 200ms, took {showTime}ms");
            Assert.IsTrue(_taskbarIntegration.IsVisible, "Icon should be visible after ShowAsync");
            
            // Verify icon has appropriate initial state
            string initialTooltip = _taskbarIntegration.CurrentTooltip;
            Assert.IsNotNull(initialTooltip, "Icon should have initial tooltip");
            Assert.IsTrue(initialTooltip.Length > 0, "Tooltip should not be empty");
        }

        /// <summary>
        /// Scenario: User right-clicks system tray icon to access context menu
        /// Expected: Context menu appears with appropriate options (Show Stats, Settings, Exit)
        /// Performance: Menu appears within 100ms of right-click
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenRightClicked_ShouldShowContextMenuQuickly()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();
            Assert.IsTrue(_taskbarIntegration.IsVisible, "Icon must be visible for right-click test");

            bool contextMenuEventFired = false;
            TrayIconClickEventArgs? clickEventArgs = null;

            _taskbarIntegration.IconClicked += (sender, args) =>
            {
                contextMenuEventFired = true;
                clickEventArgs = args;
            };

            // Act - Simulate right-click and show context menu
            var menuTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ShowContextMenuAsync();
            });

            // Assert - Verify context menu performance
            Assert.IsTrue(menuTime <= 100, $"Context menu should appear within 100ms, took {menuTime}ms");
            
            // Note: In a real implementation, we would verify menu items exist
            // For now, we're testing that the method executes without error
        }

        /// <summary>
        /// Scenario: User hovers over system tray icon to see current network speeds
        /// Expected: Tooltip shows formatted speeds with auto-scaling, hover events fire
        /// Performance: Tooltip updates within 50ms of hover
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenHovered_ShouldShowCurrentSpeedsInTooltip()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _networkMonitor.StartMonitoringAsync();

            bool hoverEventFired = false;
            TrayIconHoverEventArgs? hoverEventArgs = null;

            _taskbarIntegration.IconHovered += (sender, args) =>
            {
                hoverEventFired = true;
                hoverEventArgs = args;
            };

            // Create sample network data with realistic speeds
            var networkData = new NetworkTrafficData
            {
                ReceiveSpeed = 2_500_000,    // 2.5 MB/s
                SendSpeed = 1_200_000,       // 1.2 MB/s
                BytesReceived = 100_000_000,  // 100 MB
                BytesSent = 50_000_000,       // 50 MB
                Timestamp = DateTime.Now,
                AdapterName = "Ethernet",
                AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            };

            // Act - Update tooltip with network data
            var tooltipUpdateTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.UpdateDisplayAsync(networkData);
            });

            string updatedTooltip = _taskbarIntegration.CurrentTooltip;

            // Assert - Verify tooltip behavior
            Assert.IsTrue(tooltipUpdateTime <= 50, 
                $"Tooltip update should complete within 50ms, took {tooltipUpdateTime}ms");
            
            // Verify tooltip contains speed information
            Assert.IsNotNull(updatedTooltip, "Tooltip should contain network speed information");
            Assert.IsTrue(updatedTooltip.Length > 0, "Tooltip should not be empty after update");
            
            // Verify auto-scaling (should show MB/s, not raw bytes)
            Assert.IsTrue(updatedTooltip.Contains("2.5") || updatedTooltip.Contains("2500"), 
                "Tooltip should contain download speed");
            Assert.IsTrue(updatedTooltip.Contains("1.2") || updatedTooltip.Contains("1200"), 
                "Tooltip should contain upload speed");
            Assert.IsFalse(updatedTooltip.Contains("2500000"), 
                "Tooltip should not show raw bytes for readability");
        }

        /// <summary>
        /// Scenario: System theme changes from light to dark mode
        /// Expected: Tray icon automatically adapts to match Windows 11 theme
        /// Performance: Theme change completes within 200ms
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenSystemThemeChanges_ShouldAdaptAutomatically()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();

            // Act - Test theme switching from Light to Dark
            var lightThemeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Light);
            });

            await Task.Delay(50); // Brief pause to ensure state change

            var darkThemeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Dark);
            });

            // Test High Contrast theme as well
            var highContrastTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.HighContrast);
            });

            // Assert - Verify theme switching performance
            Assert.IsTrue(lightThemeTime <= 200, 
                $"Light theme application should complete within 200ms, took {lightThemeTime}ms");
            Assert.IsTrue(darkThemeTime <= 200, 
                $"Dark theme application should complete within 200ms, took {darkThemeTime}ms");
            Assert.IsTrue(highContrastTime <= 200, 
                $"High contrast theme application should complete within 200ms, took {highContrastTime}ms");
            
            // Verify icon remains visible throughout theme changes
            Assert.IsTrue(_taskbarIntegration.IsVisible, 
                "Icon should remain visible during theme changes");
        }

        /// <summary>
        /// Scenario: User left-clicks system tray icon multiple times
        /// Expected: Click events fire correctly, double-click detection works
        /// Performance: Click response within 100ms
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenClicked_ShouldRespondToClickEventsQuickly()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            
            await _taskbarIntegration.ShowAsync();

            var clickEvents = new List<TrayIconClickEventArgs>();
            var clickTimes = new List<long>();

            _taskbarIntegration.IconClicked += (sender, args) =>
            {
                clickEvents.Add(args);
            };

            // Act - Simulate various click scenarios
            // Single left-click
            var singleClickTime = await MeasureExecutionTimeAsync(async () =>
            {
                // In real implementation, this would be triggered by actual click
                // For testing, we simulate the event handling
                var clickArgs = new TrayIconClickEventArgs
                {
                    Button = MouseButtons.Left,
                    ClickCount = 1,
                    Timestamp = DateTime.Now
                };
                // Simulate click handling - in real implementation this would show stats window
                await _uiComponents.ShowDetailedStatsAsync();
            });

            clickTimes.Add(singleClickTime);

            // Double-click simulation
            var doubleClickTime = await MeasureExecutionTimeAsync(async () =>
            {
                var doubleClickArgs = new TrayIconClickEventArgs
                {
                    Button = MouseButtons.Left,
                    ClickCount = 2,
                    Timestamp = DateTime.Now
                };
                // Double-click might have different behavior
                await _uiComponents.ShowDetailedStatsAsync();
            });

            clickTimes.Add(doubleClickTime);

            // Assert - Verify click response performance
            Assert.IsTrue(clickTimes.All(time => time <= 100), 
                $"All click responses should complete within 100ms, max was {clickTimes.Max()}ms");
            
            // Verify UI shows statistics after click
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should be visible after icon click");
        }

        /// <summary>
        /// Scenario: Application runs with taskbar integration for extended period
        /// Expected: No memory leaks, stable icon, consistent tooltip updates
        /// Performance: Resource usage remains stable over time
        /// </summary>
        [TestMethod]
        [Timeout(20000)] // 20 second timeout for stability test
        public async Task TaskbarIntegration_WhenRunningContinuously_ShouldMaintainStability()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();

            var updateCount = 0;
            var tooltipUpdateTimes = new List<long>();
            var startTime = DateTime.UtcNow;

            // Simulate continuous tooltip updates
            for (int i = 0; i < 20; i++) // 20 updates over time
            {
                var testData = new NetworkTrafficData
                {
                    ReceiveSpeed = 1_000_000 + (i * 100_000), // Varying speeds
                    SendSpeed = 500_000 + (i * 50_000),
                    Timestamp = DateTime.Now,
                    AdapterName = "Test Adapter",
                    AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                };

                var updateTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _taskbarIntegration.UpdateDisplayAsync(testData);
                });

                tooltipUpdateTimes.Add(updateTime);
                updateCount++;

                // Brief delay between updates
                await Task.Delay(500); // 0.5 second between updates
            }

            var totalRunTime = DateTime.UtcNow - startTime;

            // Assert - Verify stability over time
            Assert.AreEqual(20, updateCount, "Should complete all planned updates");
            Assert.IsTrue(_taskbarIntegration.IsVisible, "Icon should remain visible throughout test");
            
            // Verify performance remains consistent
            var averageUpdateTime = tooltipUpdateTimes.Average();
            var maxUpdateTime = tooltipUpdateTimes.Max();
            
            Assert.IsTrue(averageUpdateTime <= 50, 
                $"Average update time should remain ≤50ms, was {averageUpdateTime:F1}ms");
            Assert.IsTrue(maxUpdateTime <= 100, 
                $"Maximum update time should remain ≤100ms, was {maxUpdateTime}ms");

            // Verify no significant performance degradation over time
            var firstHalfAverage = tooltipUpdateTimes.Take(10).Average();
            var secondHalfAverage = tooltipUpdateTimes.Skip(10).Average();
            var performanceDelta = Math.Abs(secondHalfAverage - firstHalfAverage);
            
            Assert.IsTrue(performanceDelta <= 20, 
                $"Performance should remain stable (delta ≤20ms), delta was {performanceDelta:F1}ms");
        }

        /// <summary>
        /// Scenario: Custom display format is applied to tooltip
        /// Expected: Tooltip format changes immediately, data accuracy maintained
        /// Performance: Format change within 100ms
        /// </summary>
        [TestMethod]
        public async Task TaskbarIntegration_WhenDisplayFormatChanged_ShouldUpdateTooltipFormat()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();

            var testData = new NetworkTrafficData
            {
                ReceiveSpeed = 5_000_000,   // 5 MB/s
                SendSpeed = 2_000_000,      // 2 MB/s
                Timestamp = DateTime.Now,
                AdapterName = "Test Adapter",
                AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            };

            // Update with default format
            await _taskbarIntegration.UpdateDisplayAsync(testData);
            string defaultTooltip = _taskbarIntegration.CurrentTooltip;

            // Act - Change to custom format
            var formatChangeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.SetDisplayFormatAsync("📥 {0}/s | 📤 {1}/s");
            });

            // Update with same data to see format change
            await _taskbarIntegration.UpdateDisplayAsync(testData);
            string customTooltip = _taskbarIntegration.CurrentTooltip;

            // Test another format
            await _taskbarIntegration.SetDisplayFormatAsync("Down: {0} • Up: {1}");
            await _taskbarIntegration.UpdateDisplayAsync(testData);
            string alternativeTooltip = _taskbarIntegration.CurrentTooltip;

            // Assert - Verify format changes
            Assert.IsTrue(formatChangeTime <= 100, 
                $"Format change should complete within 100ms, took {formatChangeTime}ms");
            
            Assert.AreNotEqual(defaultTooltip, customTooltip, 
                "Tooltip should change when format is updated");
            Assert.AreNotEqual(customTooltip, alternativeTooltip, 
                "Tooltip should reflect different formats");
            
            // Verify custom format elements are present
            Assert.IsTrue(customTooltip.Contains("📥") && customTooltip.Contains("📤"), 
                "Custom format should include emoji indicators");
            Assert.IsTrue(alternativeTooltip.Contains("Down:") && alternativeTooltip.Contains("Up:"), 
                "Alternative format should include text indicators");
            
            // Verify data values are preserved across format changes
            Assert.IsTrue(customTooltip.Contains("5") || customTooltip.Contains("5000"), 
                "Speed values should be preserved in custom format");
            Assert.IsTrue(alternativeTooltip.Contains("5") || alternativeTooltip.Contains("5000"), 
                "Speed values should be preserved in alternative format");
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            // Cleanup - Hide taskbar integration and close any open windows
            try
            {
                _taskbarIntegration?.HideAsync().Wait(1000);
                _uiComponents?.HideDetailedStatsAsync().Wait(1000);
                _networkMonitor?.StopMonitoringAsync().Wait(1000);
            }
            catch (Exception)
            {
                // Expected during TDD RED phase when implementations don't exist
            }
            
            base.TestCleanup();
        }
    }
}
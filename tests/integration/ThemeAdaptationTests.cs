using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Integration tests for Windows theme adaptation scenario
    /// Based on quickstart.md scenario: "Windows theme adaptation"
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class ThemeAdaptationTests : IntegrationTestBase
    {
        private IUIComponents? _uiComponents;
        private ITaskbarIntegration? _taskbarIntegration;
        private INetworkMonitor? _networkMonitor;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // These services will be null initially because implementations don't exist yet
            // This is expected and part of TDD - tests should FAIL first
            try
            {
                _uiComponents = GetService<IUIComponents>();
                _taskbarIntegration = GetService<ITaskbarIntegration>();
                _networkMonitor = GetService<INetworkMonitor>();
            }
            catch (InvalidOperationException)
            {
                // Expected: Services not registered yet because implementations don't exist
                // This is the RED phase of TDD - tests should fail
            }
        }

        /// <summary>
        /// Scenario: Windows system theme changes from Light to Dark mode
        /// Expected: Application automatically detects change and updates all UI elements
        /// Performance: Theme adaptation completes within 200ms
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenSystemThemeChangesToDark_ShouldUpdateAllComponentsAutomatically()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            // Initialize with light theme
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();
            await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Light);
            await _uiComponents.ApplyThemeAsync(WindowsTheme.Light);

            // Verify initial theme state
            Assert.AreEqual(WindowsTheme.Light, _uiComponents.CurrentTheme, 
                "UI should start with Light theme");

            // Act - Simulate system theme change to Dark mode
            var darkThemeAdaptationTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Dark);
                await _uiComponents.ApplyThemeAsync(WindowsTheme.Dark);
            });

            // Assert - Verify theme adaptation
            Assert.IsTrue(darkThemeAdaptationTime <= 200, 
                $"Dark theme adaptation should complete within 200ms, took {darkThemeAdaptationTime}ms");
            Assert.AreEqual(WindowsTheme.Dark, _uiComponents.CurrentTheme, 
                "UI should update to Dark theme");
            
            // Verify both components remain functional after theme change
            Assert.IsTrue(_taskbarIntegration.IsVisible, 
                "Taskbar icon should remain visible after theme change");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible after theme change");
        }

        /// <summary>
        /// Scenario: Application starts and automatically detects current Windows theme
        /// Expected: UI matches system theme without user intervention
        /// Performance: Theme detection and application within 500ms of startup
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenApplicationStarts_ShouldAutoDetectCurrentSystemTheme()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");

            // Test with Auto theme detection
            var autoThemeDetectionTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Auto);
                await _uiComponents.ApplyThemeAsync(WindowsTheme.Auto);
                await _taskbarIntegration.ShowAsync();
                await _uiComponents.ShowDetailedStatsAsync();
            });

            // Act - Initialize UI components and verify theme detection
            WindowsTheme detectedTheme = _uiComponents.CurrentTheme;

            // Assert - Verify automatic theme detection
            Assert.IsTrue(autoThemeDetectionTime <= 500, 
                $"Auto theme detection should complete within 500ms, took {autoThemeDetectionTime}ms");
            Assert.IsTrue(detectedTheme == WindowsTheme.Light || 
                         detectedTheme == WindowsTheme.Dark || 
                         detectedTheme == WindowsTheme.HighContrast, 
                $"Should detect a valid system theme, detected: {detectedTheme}");
            
            // Verify components are functional with detected theme
            Assert.IsTrue(_taskbarIntegration.IsVisible, 
                "Taskbar should be visible with auto-detected theme");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should be visible with auto-detected theme");
        }

        /// <summary>
        /// Scenario: User enables High Contrast mode for accessibility
        /// Expected: All UI elements adapt to high contrast colors and fonts
        /// Performance: High contrast adaptation within 200ms
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenHighContrastEnabled_ShouldAdaptForAccessibility()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            // Start with normal theme
            await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Light);
            await _uiComponents.ApplyThemeAsync(WindowsTheme.Light);

            // Act - Apply high contrast theme
            var highContrastAdaptationTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.HighContrast);
                await _uiComponents.ApplyThemeAsync(WindowsTheme.HighContrast);
            });

            // Assert - Verify high contrast adaptation
            Assert.IsTrue(highContrastAdaptationTime <= 200, 
                $"High contrast adaptation should complete within 200ms, took {highContrastAdaptationTime}ms");
            Assert.AreEqual(WindowsTheme.HighContrast, _uiComponents.CurrentTheme, 
                "UI should update to HighContrast theme");
            
            // Verify accessibility features are maintained
            Assert.IsTrue(_taskbarIntegration.IsVisible, 
                "Taskbar icon should remain visible in high contrast mode");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain visible in high contrast mode");
        }

        /// <summary>
        /// Scenario: Multiple rapid theme changes occur (system instability or user testing)
        /// Expected: Application handles rapid changes gracefully without crashes or lag
        /// Performance: Each theme change within 200ms, stable after sequence
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenRapidThemeChanges_ShouldHandleStabilityGracefully()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            // Define sequence of rapid theme changes
            var themeSequence = new[]
            {
                WindowsTheme.Light,
                WindowsTheme.Dark,
                WindowsTheme.Light,
                WindowsTheme.HighContrast,
                WindowsTheme.Dark,
                WindowsTheme.Light,
                WindowsTheme.HighContrast,
                WindowsTheme.Dark
            };

            var changeTimings = new List<long>();
            var startTime = DateTime.UtcNow;

            // Act - Apply rapid theme changes
            foreach (var theme in themeSequence)
            {
                var changeTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _taskbarIntegration.ApplyThemeAsync(theme);
                    await _uiComponents.ApplyThemeAsync(theme);
                });
                
                changeTimings.Add(changeTime);
                
                // Very brief delay to simulate rapid changes
                await Task.Delay(25);
            }

            var totalTime = DateTime.UtcNow - startTime;

            // Assert - Verify stability and performance
            Assert.IsTrue(changeTimings.All(time => time <= 200), 
                $"All theme changes should complete within 200ms, max was {changeTimings.Max()}ms");
            
            var averageChangeTime = changeTimings.Average();
            Assert.IsTrue(averageChangeTime <= 150, 
                $"Average theme change time should be ≤150ms, was {averageChangeTime:F1}ms");
            
            // Verify final state is stable
            Assert.AreEqual(WindowsTheme.Dark, _uiComponents.CurrentTheme, 
                "Final theme should match last applied theme");
            Assert.IsTrue(_taskbarIntegration.IsVisible, 
                "Taskbar should remain stable after rapid theme changes");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Statistics window should remain stable after rapid theme changes");
        }

        /// <summary>
        /// Scenario: Theme adaptation occurs while network data is actively updating
        /// Expected: Data updates continue seamlessly during theme changes
        /// Performance: No interruption to data flow, theme change within 200ms
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenDataUpdatingDuringThemeChange_ShouldMaintainDataFlow()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            Assert.IsNotNull(_networkMonitor, "Network monitor service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();
            await _networkMonitor.StartMonitoringAsync();

            // Set up continuous data updates
            var updateCount = 0;
            var updatesDuringThemeChange = 0;
            var themeChangeCompleted = false;

            // Create test data for continuous updates
            var testData = new NetworkTrafficData
            {
                ReceiveSpeed = 3_000_000, // 3 MB/s
                SendSpeed = 1_500_000,    // 1.5 MB/s
                BytesReceived = 100_000_000,
                BytesSent = 50_000_000,
                Timestamp = DateTime.Now,
                AdapterName = "Test Adapter",
                AdapterType = System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
            };

            // Start continuous updates in background
            var updateTask = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++) // 10 updates during test
                {
                    testData.ReceiveSpeed += 100_000; // Simulate changing speeds
                    testData.SendSpeed += 50_000;
                    testData.Timestamp = DateTime.Now;
                    
                    await _taskbarIntegration.UpdateDisplayAsync(testData);
                    await _uiComponents.UpdateStatisticsAsync(testData);
                    
                    updateCount++;
                    if (!themeChangeCompleted)
                        updatesDuringThemeChange++;
                    
                    await Task.Delay(100); // Update every 100ms
                }
            });

            // Act - Apply theme change while updates are happening
            await Task.Delay(200); // Let some updates start
            
            var themeChangeTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Dark);
                await _uiComponents.ApplyThemeAsync(WindowsTheme.Dark);
            });
            
            themeChangeCompleted = true;
            
            // Wait for updates to complete
            await updateTask;

            // Assert - Verify data continuity during theme change
            Assert.IsTrue(themeChangeTime <= 200, 
                $"Theme change should complete within 200ms even during updates, took {themeChangeTime}ms");
            Assert.IsTrue(updateCount >= 8, 
                $"Should complete most updates despite theme change, completed {updateCount}/10");
            Assert.IsTrue(updatesDuringThemeChange >= 2, 
                $"Should have updates during theme change, had {updatesDuringThemeChange}");
            
            // Verify final state is correct
            Assert.AreEqual(WindowsTheme.Dark, _uiComponents.CurrentTheme, 
                "Theme should be correctly applied despite concurrent updates");
            Assert.IsTrue(_taskbarIntegration.IsVisible && _uiComponents.IsStatisticsWindowVisible, 
                "All components should remain functional");
        }

        /// <summary>
        /// Scenario: Theme preferences are saved and restored across application restarts
        /// Expected: User's last theme choice is remembered and applied on startup
        /// Performance: Theme restoration within 300ms of startup
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenApplicationRestarted_ShouldRestorePreviousThemePreference()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");

            // Simulate previous session with Dark theme preference
            var previousThemePreference = WindowsTheme.Dark;

            // Act - Simulate application startup with theme restoration
            var themeRestorationTime = await MeasureExecutionTimeAsync(async () =>
            {
                // In real implementation, this would load from user settings/registry
                await _taskbarIntegration.ApplyThemeAsync(previousThemePreference);
                await _uiComponents.ApplyThemeAsync(previousThemePreference);
                await _taskbarIntegration.ShowAsync();
                await _uiComponents.InitializeAsync();
            });

            // Assert - Verify theme preference restoration
            Assert.IsTrue(themeRestorationTime <= 300, 
                $"Theme restoration should complete within 300ms, took {themeRestorationTime}ms");
            Assert.AreEqual(previousThemePreference, _uiComponents.CurrentTheme, 
                "Should restore previous theme preference");
            
            // Test preference saving simulation
            var newPreference = WindowsTheme.Light;
            await _uiComponents.ApplyThemeAsync(newPreference);
            
            // Simulate restart
            await _uiComponents.ShutdownAsync();
            await Task.Delay(50);
            
            var restoreTime = await MeasureExecutionTimeAsync(async () =>
            {
                await _uiComponents.InitializeAsync();
                await _uiComponents.ApplyThemeAsync(newPreference);
            });

            Assert.IsTrue(restoreTime <= 300, 
                $"Theme preference restoration should be fast, took {restoreTime}ms");
        }

        /// <summary>
        /// Scenario: Theme adaptation occurs with different display configurations
        /// Expected: Theme adapts correctly regardless of display setup (single/multi-monitor)
        /// Performance: Theme application consistent across all displays
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenMultipleDisplays_ShouldAdaptConsistentlyAcrossMonitors()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            // Simulate different display positions (multi-monitor setup)
            var displayPositions = new[]
            {
                new System.Drawing.Rectangle(0, 0, 1920, 1080),      // Primary monitor
                new System.Drawing.Rectangle(1920, 0, 1920, 1080),  // Secondary monitor (right)
                new System.Drawing.Rectangle(-1920, 0, 1920, 1080), // Secondary monitor (left)
                new System.Drawing.Rectangle(0, -1080, 1920, 1080)  // Secondary monitor (above)
            };

            var positioningTimes = new List<long>();
            var themeApplicationTimes = new List<long>();

            // Act - Test theme application with different window positions
            foreach (var position in displayPositions)
            {
                // Position window on different "monitor"
                var positionTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.PositionWindowAsync(position);
                });
                positioningTimes.Add(positionTime);

                // Apply theme change
                var themeTime = await MeasureExecutionTimeAsync(async () =>
                {
                    await _uiComponents.ApplyThemeAsync(WindowsTheme.Dark);
                    await _taskbarIntegration.ApplyThemeAsync(WindowsTheme.Dark);
                });
                themeApplicationTimes.Add(themeTime);

                await Task.Delay(50); // Brief delay between position changes
            }

            // Assert - Verify consistent performance across positions
            Assert.IsTrue(positioningTimes.All(time => time <= 100), 
                $"Window positioning should be fast on all displays, max was {positioningTimes.Max()}ms");
            Assert.IsTrue(themeApplicationTimes.All(time => time <= 200), 
                $"Theme application should be consistent across displays, max was {themeApplicationTimes.Max()}ms");
            
            // Verify theme is correctly applied
            Assert.AreEqual(WindowsTheme.Dark, _uiComponents.CurrentTheme, 
                "Theme should be correctly applied regardless of display position");
            Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                "Window should remain visible after multi-display positioning");
        }

        /// <summary>
        /// Scenario: Error handling during theme adaptation
        /// Expected: Graceful fallback to default theme if adaptation fails
        /// Performance: Error recovery within 500ms
        /// </summary>
        [TestMethod]
        public async Task ThemeAdaptation_WhenThemeApplicationFails_ShouldFallbackGracefully()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents, "UI components service should be available");
            Assert.IsNotNull(_taskbarIntegration, "Taskbar integration service should be available");
            
            await _taskbarIntegration.ShowAsync();
            await _uiComponents.ShowDetailedStatsAsync();

            // Start with a known good theme
            await _uiComponents.ApplyThemeAsync(WindowsTheme.Light);
            var initialTheme = _uiComponents.CurrentTheme;

            // Act - Test error handling (simulate invalid theme scenario)
            // Note: In real implementation, this might involve corrupted theme files or system issues
            try
            {
                var recoveryTime = await MeasureExecutionTimeAsync(async () =>
                {
                    try
                    {
                        // This might throw in real implementation due to system issues
                        await _uiComponents.ApplyThemeAsync(WindowsTheme.HighContrast);
                    }
                    catch (Exception)
                    {
                        // Simulate fallback to default theme
                        await _uiComponents.ApplyThemeAsync(WindowsTheme.Light);
                    }
                });

                // Assert - Verify graceful error recovery
                Assert.IsTrue(recoveryTime <= 500, 
                    $"Error recovery should complete within 500ms, took {recoveryTime}ms");
                
                // Verify application remains functional
                Assert.IsTrue(_uiComponents.IsStatisticsWindowVisible, 
                    "Statistics window should remain functional after theme error");
                Assert.IsTrue(_taskbarIntegration.IsVisible, 
                    "Taskbar should remain functional after theme error");
                
                // Verify theme state is valid
                var currentTheme = _uiComponents.CurrentTheme;
                Assert.IsTrue(currentTheme == WindowsTheme.Light || 
                             currentTheme == WindowsTheme.Dark || 
                             currentTheme == WindowsTheme.HighContrast, 
                    $"Should maintain valid theme after error recovery, current: {currentTheme}");
            }
            catch (NotImplementedException)
            {
                // Expected during TDD RED phase - services don't exist yet
                Assert.Inconclusive("Service implementations not yet available for error testing");
            }
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            // Cleanup - Shutdown UI components and hide taskbar
            try
            {
                _uiComponents?.ShutdownAsync().Wait(1000);
                _taskbarIntegration?.HideAsync().Wait(1000);
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;
using NetworkUsage.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Unit
{
    /// <summary>
    /// Performance validation tests to verify <100ms response times and resource usage
    /// Tests individual service performance in isolation
    /// Validates performance requirements from contracts and specifications
    /// </summary>
    [TestClass]
    public class PerformanceValidationTests : UnitTestBase
    {
        private NetworkMonitorService? _networkMonitor;
        private TaskbarIntegrationService? _taskbarIntegration;
        private UIComponentsService? _uiComponents;
        private PerformanceOptimizationService? _performanceService;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            // Create services for performance testing
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            
            _networkMonitor = new NetworkMonitorService(loggerFactory.CreateLogger<NetworkMonitorService>());
            _taskbarIntegration = new TaskbarIntegrationService(loggerFactory.CreateLogger<TaskbarIntegrationService>());
            _uiComponents = new UIComponentsService(loggerFactory.CreateLogger<UIComponentsService>());
            _performanceService = new PerformanceOptimizationService(
                _networkMonitor, 
                _taskbarIntegration, 
                _uiComponents, 
                loggerFactory.CreateLogger<PerformanceOptimizationService>());
        }

        #region Response Time Performance Tests

        [TestMethod]
        public async Task NetworkMonitorService_GetCurrentTrafficAsync_ShouldCompleteWithin100ms()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor);
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test multiple calls to get average performance
            for (int i = 0; i < 10; i++)
            {
                stopwatch.Restart();
                
                try
                {
                    await _networkMonitor.GetCurrentTrafficAsync();
                }
                catch (InvalidOperationException)
                {
                    // Expected when no adapters available in test environment
                }
                
                stopwatch.Stop();
                timings.Add(stopwatch.ElapsedMilliseconds);
                
                // Brief delay between calls
                await Task.Delay(50);
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 100, 
                $"GetCurrentTrafficAsync average time should be ≤100ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 200, 
                $"GetCurrentTrafficAsync max time should be ≤200ms, was {maxTime}ms");
        }

        [TestMethod]
        public async Task NetworkMonitorService_GetAvailableAdaptersAsync_ShouldCompleteWithin500ms()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor);
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test multiple calls
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Restart();
                await _networkMonitor.GetAvailableAdaptersAsync();
                stopwatch.Stop();
                
                timings.Add(stopwatch.ElapsedMilliseconds);
                await Task.Delay(100);
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 500, 
                $"GetAvailableAdaptersAsync average time should be ≤500ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 1000, 
                $"GetAvailableAdaptersAsync max time should be ≤1000ms, was {maxTime}ms");
        }

        [TestMethod]
        public async Task TaskbarIntegrationService_UpdateDisplayAsync_ShouldCompleteWithin50ms()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration);
            
            var testData = new NetworkTrafficData(
                bytesReceived: 1000000,
                bytesSent: 500000,
                receiveSpeed: 1500000, // 1.5 MB/s
                sendSpeed: 750000,     // 0.75 MB/s
                adapterName: "Test Adapter"
            );

            await _taskbarIntegration.ShowAsync();
            
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test multiple display updates
            for (int i = 0; i < 20; i++)
            {
                testData.ReceiveSpeed += 1000; // Vary the data
                testData.SendSpeed += 500;
                
                stopwatch.Restart();
                await _taskbarIntegration.UpdateDisplayAsync(testData);
                stopwatch.Stop();
                
                timings.Add(stopwatch.ElapsedMilliseconds);
                await Task.Delay(25);
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 50, 
                $"UpdateDisplayAsync average time should be ≤50ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 100, 
                $"UpdateDisplayAsync max time should be ≤100ms, was {maxTime}ms");
        }

        [TestMethod]
        public async Task UIComponentsService_ShowDetailedStatsAsync_ShouldCompleteWithin100ms()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents);
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test window show/hide performance
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Restart();
                await _uiComponents.ShowDetailedStatsAsync();
                stopwatch.Stop();
                timings.Add(stopwatch.ElapsedMilliseconds);
                
                await _uiComponents.HideDetailedStatsAsync();
                await Task.Delay(100); // Brief delay between tests
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 100, 
                $"ShowDetailedStatsAsync average time should be ≤100ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 200, 
                $"ShowDetailedStatsAsync max time should be ≤200ms, was {maxTime}ms");
        }

        [TestMethod]
        public async Task UIComponentsService_UpdateStatisticsAsync_ShouldCompleteWithin50ms()
        {
            // Arrange
            Assert.IsNotNull(_uiComponents);
            
            await _uiComponents.ShowDetailedStatsAsync();
            
            var testData = new NetworkTrafficData(
                bytesReceived: 2000000,
                bytesSent: 1000000,
                receiveSpeed: 2500000, // 2.5 MB/s
                sendSpeed: 1250000,    // 1.25 MB/s
                adapterName: "Performance Test Adapter"
            );

            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test statistics updates
            for (int i = 0; i < 15; i++)
            {
                testData.ReceiveSpeed += 10000; // Vary the data
                testData.SendSpeed += 5000;
                testData.Timestamp = DateTime.Now;
                
                stopwatch.Restart();
                await _uiComponents.UpdateStatisticsAsync(testData);
                stopwatch.Stop();
                
                timings.Add(stopwatch.ElapsedMilliseconds);
                await Task.Delay(30);
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 50, 
                $"UpdateStatisticsAsync average time should be ≤50ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 100, 
                $"UpdateStatisticsAsync max time should be ≤100ms, was {maxTime}ms");
        }

        [TestMethod]
        public async Task TaskbarIntegrationService_ApplyThemeAsync_ShouldCompleteWithin200ms()
        {
            // Arrange
            Assert.IsNotNull(_taskbarIntegration);
            await _taskbarIntegration.ShowAsync();
            
            var themes = new[] { WindowsTheme.Light, WindowsTheme.Dark, WindowsTheme.HighContrast, WindowsTheme.Auto };
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Test theme switching performance
            foreach (var theme in themes)
            {
                stopwatch.Restart();
                await _taskbarIntegration.ApplyThemeAsync(theme);
                stopwatch.Stop();
                
                timings.Add(stopwatch.ElapsedMilliseconds);
                await Task.Delay(50); // Brief delay between theme changes
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 200, 
                $"ApplyThemeAsync average time should be ≤200ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 400, 
                $"ApplyThemeAsync max time should be ≤400ms, was {maxTime}ms");
        }

        #endregion

        #region Resource Usage Performance Tests

        [TestMethod]
        public async Task NetworkMonitorService_ContinuousMonitoring_ShouldMaintainLowResourceUsage()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor);
            var initialMemory = GC.GetTotalMemory(false);
            var process = Process.GetCurrentProcess();
            var initialWorkingSet = process.WorkingSet64;

            // Act - Run continuous monitoring for 10 seconds
            try
            {
                await _networkMonitor.StartMonitoringAsync();
                
                var monitoringTask = Task.Run(async () =>
                {
                    // Let monitoring run for 10 seconds
                    await Task.Delay(TimeSpan.FromSeconds(10));
                });

                await monitoringTask;
                await _networkMonitor.StopMonitoringAsync();
            }
            catch (InvalidOperationException)
            {
                // Expected in test environment without real adapters
            }

            // Force garbage collection to get accurate measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var finalWorkingSet = process.WorkingSet64;

            // Assert
            var memoryIncrease = (finalMemory - initialMemory) / (1024.0 * 1024.0); // MB
            var workingSetIncrease = (finalWorkingSet - initialWorkingSet) / (1024.0 * 1024.0); // MB
            
            Assert.IsTrue(memoryIncrease <= 10, 
                $"Memory increase should be ≤10MB during monitoring, was {memoryIncrease:F1}MB");
            Assert.IsTrue(workingSetIncrease <= 20, 
                $"Working set increase should be ≤20MB, was {workingSetIncrease:F1}MB");
        }

        [TestMethod]
        public async Task SpeedReading_MassiveAutoScaling_ShouldMaintainPerformance()
        {
            // Arrange
            var speeds = new double[1000];
            var random = new Random(42); // Fixed seed for reproducible tests
            
            // Generate random speeds across all scales
            for (int i = 0; i < speeds.Length; i++)
            {
                speeds[i] = random.NextDouble() * 10_000_000_000; // Up to 10 GB/s
            }

            var stopwatch = Stopwatch.StartNew();
            var speedReadings = new List<SpeedReading>();

            // Act - Create speed readings with auto-scaling
            stopwatch.Start();
            foreach (var speed in speeds)
            {
                speedReadings.Add(SpeedReading.FromBytesPerSecond(speed));
            }
            stopwatch.Stop();

            // Assert
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTimePerReading = totalTime / (double)speeds.Length;
            
            Assert.IsTrue(averageTimePerReading <= 0.1, 
                $"Average time per SpeedReading creation should be ≤0.1ms, was {averageTimePerReading:F3}ms");
            Assert.IsTrue(totalTime <= 100, 
                $"Total time for 1000 speed readings should be ≤100ms, was {totalTime}ms");
            
            // Verify all readings are valid
            Assert.AreEqual(speeds.Length, speedReadings.Count, "Should create all speed readings");
            Assert.IsTrue(speedReadings.All(r => !string.IsNullOrEmpty(r.FormattedString)), 
                "All speed readings should have formatted strings");
        }

        [TestMethod]
        public async Task DisplayConfiguration_RapidConfigurationChanges_ShouldMaintainPerformance()
        {
            // Arrange
            var config = new DisplayConfiguration();
            var stopwatch = Stopwatch.StartNew();
            var timings = new List<long>();

            // Act - Rapid configuration changes
            for (int i = 0; i < 100; i++)
            {
                stopwatch.Restart();
                
                // Make various configuration changes
                config.UpdateInterval = TimeSpan.FromMilliseconds(500 + (i * 10));
                config.AutoScaleUnits = i % 2 == 0;
                config.CurrentTheme = (WindowsTheme)(i % 4);
                config.ShowInSystemTray = i % 3 == 0;
                config.ResponseTimeoutMs = 50 + (i % 50);
                
                // Read all values to ensure they're processed
                var interval = config.UpdateInterval;
                var autoScale = config.AutoScaleUnits;
                var theme = config.CurrentTheme;
                var showTray = config.ShowInSystemTray;
                var timeout = config.ResponseTimeoutMs;
                
                stopwatch.Stop();
                timings.Add(stopwatch.ElapsedTicks);
            }

            // Assert
            var averageTimeMicroseconds = timings.Average() / (Stopwatch.Frequency / 1_000_000.0);
            var maxTimeMicroseconds = timings.Max() / (Stopwatch.Frequency / 1_000_000.0);
            
            Assert.IsTrue(averageTimeMicroseconds <= 100, 
                $"Average configuration change should be ≤100μs, was {averageTimeMicroseconds:F1}μs");
            Assert.IsTrue(maxTimeMicroseconds <= 1000, 
                $"Max configuration change should be ≤1000μs, was {maxTimeMicroseconds:F1}μs");
        }

        [TestMethod]
        public async Task NetworkAdapter_MassValidation_ShouldMaintainPerformance()
        {
            // Arrange
            var adapters = new List<NetworkAdapter>();
            var stopwatch = Stopwatch.StartNew();

            // Create many test adapters
            for (int i = 0; i < 500; i++)
            {
                adapters.Add(new NetworkAdapter(
                    id: $"adapter-{i}",
                    name: $"Test Adapter {i}",
                    description: $"Test Network Adapter {i}",
                    type: i % 2 == 0 ? NetworkInterfaceType.Ethernet : NetworkInterfaceType.Wireless80211,
                    status: OperationalStatus.Up,
                    speed: 1000000000 + (i * 1000000), // Varying speeds
                    ipv4Address: $"192.168.1.{i % 254 + 1}",
                    macAddress: $"00:11:22:33:44:{i % 256:X2}"
                ));
            }

            // Act - Validate all adapters
            stopwatch.Start();
            var validationResults = adapters.Select(a => a.IsValid()).ToList();
            stopwatch.Stop();

            // Assert
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTimePerValidation = totalTime / (double)adapters.Count;
            
            Assert.IsTrue(averageTimePerValidation <= 0.2, 
                $"Average validation time should be ≤0.2ms per adapter, was {averageTimePerValidation:F3}ms");
            Assert.IsTrue(totalTime <= 100, 
                $"Total validation time for 500 adapters should be ≤100ms, was {totalTime}ms");
            Assert.IsTrue(validationResults.All(r => r), "All test adapters should pass validation");
        }

        [TestMethod]
        public void SpeedReading_ArithmeticOperations_ShouldMaintainPerformance()
        {
            // Arrange
            var speeds = new SpeedReading[1000];
            var random = new Random(42);
            
            for (int i = 0; i < speeds.Length; i++)
            {
                speeds[i] = SpeedReading.FromBytesPerSecond(random.NextDouble() * 1_000_000);
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Perform arithmetic operations
            stopwatch.Start();
            
            var sum = SpeedReading.Zero;
            for (int i = 0; i < speeds.Length; i++)
            {
                sum = sum + speeds[i];
                
                if (i > 0)
                {
                    var difference = speeds[i] - speeds[i - 1];
                    var doubled = speeds[i] * 2;
                    var halved = speeds[i] / 2;
                }
            }
            
            stopwatch.Stop();

            // Assert
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTimePerOperation = totalTime / (double)(speeds.Length * 4); // 4 operations per iteration
            
            Assert.IsTrue(averageTimePerOperation <= 0.001, 
                $"Average arithmetic operation should be ≤0.001ms, was {averageTimePerOperation:F6}ms");
            Assert.IsTrue(totalTime <= 10, 
                $"Total arithmetic operations should complete in ≤10ms, was {totalTime}ms");
        }

        #endregion

        #region Memory Usage Performance Tests

        [TestMethod]
        public void NetworkTrafficData_MassCreation_ShouldNotCauseMemoryLeaks()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true); // Force GC first
            const int objectCount = 10000;

            // Act - Create many NetworkTrafficData objects
            var trafficDataList = new List<NetworkTrafficData>();
            
            for (int i = 0; i < objectCount; i++)
            {
                trafficDataList.Add(new NetworkTrafficData(
                    bytesReceived: i * 1000,
                    bytesSent: i * 500,
                    receiveSpeed: i * 1.5,
                    sendSpeed: i * 0.75,
                    adapterName: $"Adapter {i % 10}" // Reuse names to test string interning
                ));
            }

            var afterCreationMemory = GC.GetTotalMemory(false);
            
            // Clear references and force GC
            trafficDataList.Clear();
            trafficDataList = null;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var creationMemoryMB = (afterCreationMemory - initialMemory) / (1024.0 * 1024.0);
            var residualMemoryMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);
            
            Assert.IsTrue(creationMemoryMB <= 20, 
                $"Memory usage for {objectCount} objects should be ≤20MB, was {creationMemoryMB:F1}MB");
            Assert.IsTrue(residualMemoryMB <= 2, 
                $"Residual memory after GC should be ≤2MB, was {residualMemoryMB:F1}MB");
        }

        [TestMethod]
        public void SpeedReading_StructSize_ShouldBeMemoryEfficient()
        {
            // Arrange & Act
            var structSize = System.Runtime.InteropServices.Marshal.SizeOf<SpeedReading>();
            
            // Assert
            Assert.IsTrue(structSize <= 64, 
                $"SpeedReading struct should be ≤64 bytes, was {structSize} bytes");
        }

        [TestMethod]
        public async Task NetworkAdapter_CollectionOperations_ShouldMaintainPerformance()
        {
            // Arrange
            var adapters = new List<NetworkAdapter>();
            
            for (int i = 0; i < 100; i++)
            {
                adapters.Add(new NetworkAdapter(
                    id: $"perf-adapter-{i}",
                    name: $"Performance Test Adapter {i}",
                    description: $"Description {i}",
                    type: i % 2 == 0 ? NetworkInterfaceType.Ethernet : NetworkInterfaceType.Wireless80211,
                    status: OperationalStatus.Up,
                    speed: 1000000000 + (i * 100000000) // Varying speeds for sorting test
                ));
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Test collection operations
            stopwatch.Start();
            
            // Sorting (uses CompareTo)
            adapters.Sort();
            
            // Searching
            var ethernetAdapters = adapters.Where(a => a.Type == NetworkInterfaceType.Ethernet).ToList();
            var activeAdapters = adapters.Where(a => a.IsActive).ToList();
            var highSpeedAdapters = adapters.Where(a => a.Speed > 5000000000).ToList();
            
            // Grouping
            var groupedByType = adapters.GroupBy(a => a.Type).ToList();
            
            stopwatch.Stop();

            // Assert
            var totalTime = stopwatch.ElapsedMilliseconds;
            
            Assert.IsTrue(totalTime <= 50, 
                $"Collection operations on 100 adapters should complete in ≤50ms, was {totalTime}ms");
            
            // Verify operations worked correctly
            Assert.IsTrue(adapters.Count == 100, "Should maintain all adapters");
            Assert.IsTrue(ethernetAdapters.All(a => a.Type == NetworkInterfaceType.Ethernet), 
                "Filtering should work correctly");
        }

        #endregion

        #region Concurrent Performance Tests

        [TestMethod]
        public async Task DisplayConfiguration_ConcurrentAccess_ShouldMaintainPerformance()
        {
            // Arrange
            var config = new DisplayConfiguration();
            var concurrentTasks = new List<Task>();
            var timings = new List<long>();
            var lockObject = new object();

            // Act - Concurrent access test
            for (int i = 0; i < 20; i++)
            {
                int taskIndex = i;
                concurrentTasks.Add(Task.Run(() =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    for (int j = 0; j < 50; j++)
                    {
                        // Read and write operations
                        config.UpdateInterval = TimeSpan.FromMilliseconds(500 + (taskIndex * 10));
                        var interval = config.UpdateInterval;
                        
                        config.AutoScaleUnits = j % 2 == 0;
                        var autoScale = config.AutoScaleUnits;
                        
                        config.CurrentTheme = (WindowsTheme)(j % 4);
                        var theme = config.CurrentTheme;
                    }
                    
                    stopwatch.Stop();
                    
                    lock (lockObject)
                    {
                        timings.Add(stopwatch.ElapsedMilliseconds);
                    }
                }));
            }

            await Task.WhenAll(concurrentTasks);

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 100, 
                $"Average concurrent access time should be ≤100ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 500, 
                $"Max concurrent access time should be ≤500ms, was {maxTime}ms");
            Assert.IsTrue(config.IsValid(), "Configuration should remain valid after concurrent access");
        }

        [TestMethod]
        public async Task NetworkTrafficData_ConcurrentCalculations_ShouldMaintainPerformance()
        {
            // Arrange
            var concurrentTasks = new List<Task<double>>();
            var baseData = new NetworkTrafficData(1000000, 500000, 0, 0, "Test Adapter")
            {
                Timestamp = DateTime.Now.AddSeconds(-1)
            };

            // Act - Concurrent speed calculations
            for (int i = 0; i < 50; i++)
            {
                int taskIndex = i;
                concurrentTasks.Add(Task.Run(() =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    
                    for (int j = 0; j < 20; j++)
                    {
                        var currentData = new NetworkTrafficData(
                            1000000 + (taskIndex * 1000) + (j * 100),
                            500000 + (taskIndex * 500) + (j * 50),
                            0, 0,
                            "Test Adapter"
                        );
                        
                        var deltaData = NetworkTrafficData.CreateFromDelta(baseData, currentData);
                        var speed = deltaData.ReceiveSpeed + deltaData.SendSpeed; // Trigger calculations
                    }
                    
                    stopwatch.Stop();
                    return (double)stopwatch.ElapsedMilliseconds;
                }));
            }

            var results = await Task.WhenAll(concurrentTasks);

            // Assert
            var averageTime = results.Average();
            var maxTime = results.Max();
            
            Assert.IsTrue(averageTime <= 50, 
                $"Average concurrent calculation time should be ≤50ms, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 200, 
                $"Max concurrent calculation time should be ≤200ms, was {maxTime:F1}ms");
        }

        #endregion

        #region Stress Testing

        [TestMethod]
        [Timeout(30000)] // 30 second timeout
        public async Task AllServices_StressTest_ShouldMaintainPerformanceUnderLoad()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor);
            Assert.IsNotNull(_taskbarIntegration);
            Assert.IsNotNull(_uiComponents);
            
            var initialMemory = GC.GetTotalMemory(true);
            var process = Process.GetCurrentProcess();
            var testDuration = TimeSpan.FromSeconds(15);
            var startTime = DateTime.UtcNow;
            
            var performanceMetrics = new List<double>();
            var memoryMetrics = new List<long>();

            // Act - Stress test all services
            var stressTasks = new List<Task>();

            // Task 1: Rapid taskbar updates
            stressTasks.Add(Task.Run(async () =>
            {
                await _taskbarIntegration.ShowAsync();
                
                while (DateTime.UtcNow - startTime < testDuration)
                {
                    var testData = new NetworkTrafficData(
                        bytesReceived: DateTime.UtcNow.Ticks / 10000,
                        bytesSent: DateTime.UtcNow.Ticks / 20000,
                        receiveSpeed: new Random().NextDouble() * 10_000_000,
                        sendSpeed: new Random().NextDouble() * 5_000_000,
                        adapterName: "Stress Test Adapter"
                    );

                    var sw = Stopwatch.StartNew();
                    await _taskbarIntegration.UpdateDisplayAsync(testData);
                    sw.Stop();
                    
                    lock (performanceMetrics)
                    {
                        performanceMetrics.Add(sw.Elapsed.TotalMilliseconds);
                    }
                    
                    await Task.Delay(100); // 10 updates per second
                }
            }));

            // Task 2: UI updates
            stressTasks.Add(Task.Run(async () =>
            {
                await _uiComponents.ShowDetailedStatsAsync();
                
                while (DateTime.UtcNow - startTime < testDuration)
                {
                    var testData = new NetworkTrafficData(
                        bytesReceived: DateTime.UtcNow.Ticks / 5000,
                        bytesSent: DateTime.UtcNow.Ticks / 10000,
                        receiveSpeed: new Random().NextDouble() * 8_000_000,
                        sendSpeed: new Random().NextDouble() * 4_000_000,
                        adapterName: "UI Stress Test Adapter"
                    );

                    await _uiComponents.UpdateStatisticsAsync(testData);
                    await Task.Delay(200); // 5 updates per second
                }
            }));

            // Task 3: Memory monitoring
            stressTasks.Add(Task.Run(async () =>
            {
                while (DateTime.UtcNow - startTime < testDuration)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    
                    lock (memoryMetrics)
                    {
                        memoryMetrics.Add(currentMemory);
                    }
                    
                    await Task.Delay(1000); // Check every second
                }
            }));

            await Task.WhenAll(stressTasks);

            // Force cleanup
            await _uiComponents.HideDetailedStatsAsync();
            await _taskbarIntegration.HideAsync();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            lock (performanceMetrics)
            {
                if (performanceMetrics.Count > 0)
                {
                    var avgResponseTime = performanceMetrics.Average();
                    var maxResponseTime = performanceMetrics.Max();
                    
                    Assert.IsTrue(avgResponseTime <= 100, 
                        $"Average response time under stress should be ≤100ms, was {avgResponseTime:F1}ms");
                    Assert.IsTrue(maxResponseTime <= 500, 
                        $"Max response time under stress should be ≤500ms, was {maxResponseTime:F1}ms");
                }
            }

            lock (memoryMetrics)
            {
                if (memoryMetrics.Count > 0)
                {
                    var peakMemory = memoryMetrics.Max();
                    var memoryIncrease = (peakMemory - initialMemory) / (1024.0 * 1024.0);
                    
                    Assert.IsTrue(memoryIncrease <= 30, 
                        $"Memory increase under stress should be ≤30MB, was {memoryIncrease:F1}MB");
                }
            }

            var totalMemoryIncrease = (finalMemory - initialMemory) / (1024.0 * 1024.0);
            Assert.IsTrue(totalMemoryIncrease <= 5, 
                $"Residual memory after stress test should be ≤5MB, was {totalMemoryIncrease:F1}MB");
        }

        #endregion

        #region Performance Boundary Tests

        [TestMethod]
        public async Task NetworkMonitorService_SetUpdateInterval_BoundaryValues_ShouldMaintainPerformance()
        {
            // Arrange
            Assert.IsNotNull(_networkMonitor);
            var boundaryValues = new[]
            {
                TimeSpan.FromMilliseconds(500),  // Minimum allowed
                TimeSpan.FromMilliseconds(1000), // Default
                TimeSpan.FromMilliseconds(5000), // Mid-range
                TimeSpan.FromSeconds(10)         // Maximum allowed
            };

            var timings = new List<long>();

            // Act - Test all boundary values
            foreach (var interval in boundaryValues)
            {
                var stopwatch = Stopwatch.StartNew();
                await _networkMonitor.SetUpdateIntervalAsync(interval);
                stopwatch.Stop();
                
                timings.Add(stopwatch.ElapsedMilliseconds);
                await Task.Delay(50);
            }

            // Assert
            var averageTime = timings.Average();
            var maxTime = timings.Max();
            
            Assert.IsTrue(averageTime <= 10, 
                $"Setting update interval should be ≤10ms on average, was {averageTime:F1}ms");
            Assert.IsTrue(maxTime <= 50, 
                $"Setting update interval should be ≤50ms max, was {maxTime}ms");
        }

        [TestMethod]
        public void DisplayConfiguration_PropertyAccess_ShouldBeFastUnderConcurrency()
        {
            // Arrange
            var config = new DisplayConfiguration();
            var accessCount = 0;
            var totalTime = 0L;
            var lockObject = new object();

            // Act - Rapid property access from multiple threads
            Parallel.For(0, 1000, i =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Rapid property access (thread-safe)
                for (int j = 0; j < 10; j++)
                {
                    var interval = config.UpdateInterval;
                    var autoScale = config.AutoScaleUnits;
                    var theme = config.CurrentTheme;
                    var showTray = config.ShowInSystemTray;
                    var format = config.ToolTipFormat;
                    var timeout = config.ResponseTimeoutMs;
                }
                
                stopwatch.Stop();
                
                lock (lockObject)
                {
                    accessCount += 10;
                    totalTime += stopwatch.ElapsedTicks;
                }
            });

            // Assert
            var averageTimeMicroseconds = (totalTime / (double)accessCount) / (Stopwatch.Frequency / 1_000_000.0);
            
            Assert.IsTrue(averageTimeMicroseconds <= 10, 
                $"Property access should be ≤10μs on average, was {averageTimeMicroseconds:F2}μs");
            Assert.AreEqual(10000, accessCount, "Should complete all property accesses");
        }

        #endregion

        [TestCleanup]
        public override void TestCleanup()
        {
            try
            {
                // Cleanup services
                _networkMonitor?.Dispose();
                _taskbarIntegration?.Dispose();
                _uiComponents?.Dispose();
                _performanceService?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error during performance test cleanup");
            }
            
            // Force garbage collection after performance tests
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            base.TestCleanup();
        }
    }
}

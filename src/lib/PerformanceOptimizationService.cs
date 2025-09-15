using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;

namespace NetworkUsage.Services
{
    /// <summary>
    /// Performance optimization service to meet <1% CPU and <50MB RAM constraints
    /// Monitors resource usage and applies optimizations to maintain performance targets
    /// Provides: CPU monitoring, memory management, GC optimization, resource cleanup
    /// </summary>
    public class PerformanceOptimizationService : BackgroundService, IDisposable
    {
        private readonly ILogger<PerformanceOptimizationService> _logger;
        private readonly NetworkMonitorService _networkMonitor;
        private readonly TaskbarIntegrationService _taskbarIntegration;
        private readonly UIComponentsService _uiComponents;
        
        // Performance monitoring
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly Process _currentProcess;
        
        // Performance targets and metrics
        private const double MaxCpuUsagePercent = 1.0;
        private const long MaxMemoryUsageBytes = 50 * 1024 * 1024; // 50 MB
        private const double MaxResponseTimeMs = 100.0;
        
        // Performance tracking
        private readonly Queue<double> _cpuUsageHistory = new();
        private readonly Queue<long> _memoryUsageHistory = new();
        private readonly Queue<double> _responseTimeHistory = new();
        private const int MaxHistoryCount = 300; // 5 minutes at 1-second intervals
        
        // Optimization state
        private bool _isOptimizationActive = false;
        private DateTime _lastOptimization = DateTime.Now;
        private int _optimizationCount = 0;
        private readonly object _optimizationLock = new object();

        public PerformanceOptimizationService(
            NetworkMonitorService networkMonitor,
            TaskbarIntegrationService taskbarIntegration,
            UIComponentsService uiComponents,
            ILogger<PerformanceOptimizationService> logger)
        {
            _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            _taskbarIntegration = taskbarIntegration ?? throw new ArgumentNullException(nameof(taskbarIntegration));
            _uiComponents = uiComponents ?? throw new ArgumentNullException(nameof(uiComponents));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize performance counters
            _currentProcess = Process.GetCurrentProcess();
            _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _currentProcess.ProcessName, true);
            _memoryCounter = new PerformanceCounter("Process", "Working Set - Private", _currentProcess.ProcessName, true);
            
            // Initialize baseline reading
            _cpuCounter.NextValue(); // First call returns 0, subsequent calls return actual values
            
            _logger.LogInformation("Performance optimization service initialized");
        }

        #region BackgroundService Implementation

        /// <summary>
        /// Main execution loop for performance monitoring and optimization
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Performance optimization service started");
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await MonitorPerformanceAsync();
                        await ApplyOptimizationsIfNeededAsync();
                        
                        // Monitor every second
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during performance monitoring");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Back off on errors
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Performance optimization service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in performance optimization service");
            }
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Monitor current performance metrics
        /// </summary>
        private async Task MonitorPerformanceAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Get CPU usage (percentage)
                var cpuUsage = await Task.Run(() =>
                {
                    try
                    {
                        return _cpuCounter.NextValue() / Environment.ProcessorCount; // Normalize for multi-core
                    }
                    catch
                    {
                        return 0.0; // Fallback if counter fails
                    }
                });

                // Get memory usage (bytes)
                var memoryUsage = await Task.Run(() =>
                {
                    try
                    {
                        return (long)_memoryCounter.NextValue();
                    }
                    catch
                    {
                        return _currentProcess.WorkingSet64; // Fallback to process working set
                    }
                });

                // Calculate response time for this monitoring cycle
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Update history queues
                lock (_optimizationLock)
                {
                    UpdatePerformanceHistory(cpuUsage, memoryUsage, responseTime);
                }

                // Log performance if outside targets
                if (cpuUsage > MaxCpuUsagePercent)
                {
                    _logger.LogWarning($"CPU usage above target: {cpuUsage:F1}% (target: <{MaxCpuUsagePercent}%)");
                }

                if (memoryUsage > MaxMemoryUsageBytes)
                {
                    var memoryMB = memoryUsage / (1024.0 * 1024.0);
                    _logger.LogWarning($"Memory usage above target: {memoryMB:F1}MB (target: <50MB)");
                }

                if (responseTime > MaxResponseTimeMs)
                {
                    _logger.LogWarning($"Response time above target: {responseTime:F1}ms (target: <{MaxResponseTimeMs}ms)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring performance metrics");
            }
        }

        /// <summary>
        /// Update performance history queues
        /// </summary>
        private void UpdatePerformanceHistory(double cpuUsage, long memoryUsage, double responseTime)
        {
            _cpuUsageHistory.Enqueue(cpuUsage);
            _memoryUsageHistory.Enqueue(memoryUsage);
            _responseTimeHistory.Enqueue(responseTime);

            // Maintain history size limits
            while (_cpuUsageHistory.Count > MaxHistoryCount)
                _cpuUsageHistory.Dequeue();
            
            while (_memoryUsageHistory.Count > MaxHistoryCount)
                _memoryUsageHistory.Dequeue();
            
            while (_responseTimeHistory.Count > MaxHistoryCount)
                _responseTimeHistory.Dequeue();
        }

        #endregion

        #region Performance Optimizations

        /// <summary>
        /// Apply performance optimizations if metrics exceed targets
        /// </summary>
        private async Task ApplyOptimizationsIfNeededAsync()
        {
            try
            {
                var stats = GetCurrentPerformanceStatistics();
                bool needsOptimization = false;
                var optimizations = new List<string>();

                // Check if any metric exceeds target
                if (stats.AverageCpuUsagePercent > MaxCpuUsagePercent)
                {
                    needsOptimization = true;
                    optimizations.Add("CPU");
                }

                if (stats.AverageMemoryUsageMB > (MaxMemoryUsageBytes / (1024.0 * 1024.0)))
                {
                    needsOptimization = true;
                    optimizations.Add("Memory");
                }

                if (stats.AverageResponseTimeMs > MaxResponseTimeMs)
                {
                    needsOptimization = true;
                    optimizations.Add("ResponseTime");
                }

                if (needsOptimization)
                {
                    _logger.LogInformation($"Applying performance optimizations for: {string.Join(", ", optimizations)}");
                    await ApplyPerformanceOptimizationsAsync(stats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying performance optimizations");
            }
        }

        /// <summary>
        /// Apply specific performance optimizations
        /// </summary>
        private async Task ApplyPerformanceOptimizationsAsync(PerformanceStatistics stats)
        {
            try
            {
                lock (_optimizationLock)
                {
                    if (_isOptimizationActive)
                    {
                        return; // Already optimizing
                    }
                    _isOptimizationActive = true;
                }

                var optimizationsApplied = new List<string>();

                // CPU Optimization
                if (stats.AverageCpuUsagePercent > MaxCpuUsagePercent)
                {
                    await OptimizeCpuUsageAsync();
                    optimizationsApplied.Add("CPU throttling");
                }

                // Memory Optimization
                if (stats.AverageMemoryUsageMB > (MaxMemoryUsageBytes / (1024.0 * 1024.0)))
                {
                    await OptimizeMemoryUsageAsync();
                    optimizationsApplied.Add("Memory cleanup");
                }

                // Response Time Optimization
                if (stats.AverageResponseTimeMs > MaxResponseTimeMs)
                {
                    await OptimizeResponseTimeAsync();
                    optimizationsApplied.Add("Response optimization");
                }

                // General system optimization
                await ApplyGeneralOptimizationsAsync();
                optimizationsApplied.Add("General optimization");

                lock (_optimizationLock)
                {
                    _lastOptimization = DateTime.Now;
                    _optimizationCount++;
                    _isOptimizationActive = false;
                }

                _logger.LogInformation($"Performance optimizations applied: {string.Join(", ", optimizationsApplied)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance optimization");
                
                lock (_optimizationLock)
                {
                    _isOptimizationActive = false;
                }
            }
        }

        /// <summary>
        /// Optimize CPU usage by reducing update frequencies
        /// </summary>
        private async Task OptimizeCpuUsageAsync()
        {
            try
            {
                // Reduce update intervals when CPU usage is high
                var currentInterval = TimeSpan.FromSeconds(1);
                var optimizedInterval = TimeSpan.FromMilliseconds(1500); // Slightly slower updates

                await _networkMonitor.SetUpdateIntervalAsync(optimizedInterval);
                await _uiComponents.SetUIUpdateIntervalAsync(optimizedInterval);

                _logger.LogInformation($"CPU optimization: Increased update interval to {optimizedInterval.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize CPU usage");
            }
        }

        /// <summary>
        /// Optimize memory usage through garbage collection and cache cleanup
        /// </summary>
        private async Task OptimizeMemoryUsageAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Force garbage collection
                    GC.Collect(2, GCCollectionMode.Optimized, false);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Optimized, false);
                    
                    // Additional memory optimizations
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                });

                // Clear any cached data in services if they support it
                // This would be implemented in each service
                
                var memoryAfterGC = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                _logger.LogInformation($"Memory optimization: GC completed, current usage: {memoryAfterGC:F1}MB");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize memory usage");
            }
        }

        /// <summary>
        /// Optimize response times by reducing unnecessary operations
        /// </summary>
        private async Task OptimizeResponseTimeAsync()
        {
            try
            {
                // Implement response time optimizations
                await Task.Run(() =>
                {
                    // Set thread priorities for better responsiveness
                    try
                    {
                        Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set thread priority");
                    }
                });

                _logger.LogInformation("Response time optimization: Thread priorities adjusted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize response time");
            }
        }

        /// <summary>
        /// Apply general system optimizations
        /// </summary>
        private async Task ApplyGeneralOptimizationsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Set process priority for better system responsiveness
                    try
                    {
                        _currentProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set process priority");
                    }

                    // Configure GC for low-latency scenarios
                    try
                    {
                        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set GC latency mode");
                    }
                });

                _logger.LogDebug("General optimizations applied");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply general optimizations");
            }
        }

        #endregion

        #region Performance Statistics

        /// <summary>
        /// Get current performance statistics
        /// </summary>
        public PerformanceStatistics GetCurrentPerformanceStatistics()
        {
            lock (_optimizationLock)
            {
                var stats = new PerformanceStatistics
                {
                    Timestamp = DateTime.Now,
                    ProcessId = _currentProcess.Id,
                    OptimizationCount = _optimizationCount,
                    LastOptimizationTime = _lastOptimization
                };

                if (_cpuUsageHistory.Count > 0)
                {
                    stats.AverageCpuUsagePercent = _cpuUsageHistory.Average();
                    stats.MaxCpuUsagePercent = _cpuUsageHistory.Max();
                    stats.CurrentCpuUsagePercent = _cpuUsageHistory.LastOrDefault();
                }

                if (_memoryUsageHistory.Count > 0)
                {
                    stats.AverageMemoryUsageMB = _memoryUsageHistory.Average() / (1024.0 * 1024.0);
                    stats.MaxMemoryUsageMB = _memoryUsageHistory.Max() / (1024.0 * 1024.0);
                    stats.CurrentMemoryUsageMB = (_memoryUsageHistory.LastOrDefault()) / (1024.0 * 1024.0);
                }

                if (_responseTimeHistory.Count > 0)
                {
                    stats.AverageResponseTimeMs = _responseTimeHistory.Average();
                    stats.MaxResponseTimeMs = _responseTimeHistory.Max();
                    stats.CurrentResponseTimeMs = _responseTimeHistory.LastOrDefault();
                }

                // Performance compliance
                stats.IsCpuCompliant = stats.AverageCpuUsagePercent <= MaxCpuUsagePercent;
                stats.IsMemoryCompliant = stats.AverageMemoryUsageMB <= (MaxMemoryUsageBytes / (1024.0 * 1024.0));
                stats.IsResponseTimeCompliant = stats.AverageResponseTimeMs <= MaxResponseTimeMs;
                stats.IsFullyCompliant = stats.IsCpuCompliant && stats.IsMemoryCompliant && stats.IsResponseTimeCompliant;

                return stats;
            }
        }

        /// <summary>
        /// Get performance trends over time
        /// </summary>
        public PerformanceTrends GetPerformanceTrends()
        {
            lock (_optimizationLock)
            {
                var trends = new PerformanceTrends();

                if (_cpuUsageHistory.Count >= 2)
                {
                    var firstHalf = _cpuUsageHistory.Take(_cpuUsageHistory.Count / 2).Average();
                    var secondHalf = _cpuUsageHistory.Skip(_cpuUsageHistory.Count / 2).Average();
                    trends.CpuUsageTrend = secondHalf - firstHalf;
                }

                if (_memoryUsageHistory.Count >= 2)
                {
                    var firstHalf = _memoryUsageHistory.Take(_memoryUsageHistory.Count / 2).Average();
                    var secondHalf = _memoryUsageHistory.Skip(_memoryUsageHistory.Count / 2).Average();
                    trends.MemoryUsageTrend = (secondHalf - firstHalf) / (1024.0 * 1024.0); // MB
                }

                if (_responseTimeHistory.Count >= 2)
                {
                    var firstHalf = _responseTimeHistory.Take(_responseTimeHistory.Count / 2).Average();
                    var secondHalf = _responseTimeHistory.Skip(_responseTimeHistory.Count / 2).Average();
                    trends.ResponseTimeTrend = secondHalf - firstHalf;
                }

                // Overall trend assessment
                trends.OverallTrend = (trends.CpuUsageTrend + trends.MemoryUsageTrend + (trends.ResponseTimeTrend / 10)) / 3;
                trends.TrendAssessment = trends.OverallTrend switch
                {
                    > 2.0 => "Degrading",
                    > 0.5 => "Slight Increase",
                    < -2.0 => "Improving",
                    < -0.5 => "Slight Decrease",
                    _ => "Stable"
                };

                return trends;
            }
        }

        /// <summary>
        /// Check if performance targets are being met
        /// </summary>
        public bool ArePerformanceTargetsMet()
        {
            var stats = GetCurrentPerformanceStatistics();
            return stats.IsFullyCompliant;
        }

        /// <summary>
        /// Get performance recommendations
        /// </summary>
        public List<PerformanceRecommendation> GetPerformanceRecommendations()
        {
            var recommendations = new List<PerformanceRecommendation>();
            var stats = GetCurrentPerformanceStatistics();

            if (!stats.IsCpuCompliant)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = "CPU",
                    Priority = RecommendationPriority.High,
                    Description = "CPU usage is above 1% target",
                    Action = "Increase update intervals or reduce monitoring frequency",
                    ExpectedImprovement = $"Reduce CPU from {stats.AverageCpuUsagePercent:F1}% to <1%"
                });
            }

            if (!stats.IsMemoryCompliant)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = "Memory",
                    Priority = RecommendationPriority.High,
                    Description = "Memory usage is above 50MB target",
                    Action = "Enable more aggressive garbage collection or reduce cache sizes",
                    ExpectedImprovement = $"Reduce memory from {stats.AverageMemoryUsageMB:F1}MB to <50MB"
                });
            }

            if (!stats.IsResponseTimeCompliant)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = "ResponseTime",
                    Priority = RecommendationPriority.Medium,
                    Description = "Response times are above 100ms target",
                    Action = "Optimize async operations or reduce UI update frequency",
                    ExpectedImprovement = $"Reduce response time from {stats.AverageResponseTimeMs:F1}ms to <100ms"
                });
            }

            var trends = GetPerformanceTrends();
            if (trends.OverallTrend > 1.0)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = "Trend",
                    Priority = RecommendationPriority.Medium,
                    Description = "Performance is degrading over time",
                    Action = "Consider restarting the application or clearing caches",
                    ExpectedImprovement = "Stabilize performance metrics"
                });
            }

            return recommendations;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force immediate performance optimization
        /// </summary>
        public async Task ForceOptimizationAsync()
        {
            try
            {
                _logger.LogInformation("Forcing immediate performance optimization...");
                
                var stats = GetCurrentPerformanceStatistics();
                await ApplyPerformanceOptimizationsAsync(stats);
                
                _logger.LogInformation("Forced optimization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force optimization");
                throw;
            }
        }

        /// <summary>
        /// Generate performance report
        /// </summary>
        public PerformanceReport GeneratePerformanceReport()
        {
            var stats = GetCurrentPerformanceStatistics();
            var trends = GetPerformanceTrends();
            var recommendations = GetPerformanceRecommendations();

            return new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                Statistics = stats,
                Trends = trends,
                Recommendations = recommendations,
                ComplianceStatus = stats.IsFullyCompliant ? "COMPLIANT" : "NON-COMPLIANT",
                OverallScore = CalculatePerformanceScore(stats)
            };
        }

        /// <summary>
        /// Calculate overall performance score (0-100)
        /// </summary>
        private double CalculatePerformanceScore(PerformanceStatistics stats)
        {
            var cpuScore = Math.Max(0, 100 - (stats.AverageCpuUsagePercent / MaxCpuUsagePercent * 100));
            var memoryScore = Math.Max(0, 100 - (stats.AverageMemoryUsageMB / (MaxMemoryUsageBytes / (1024.0 * 1024.0)) * 100));
            var responseScore = Math.Max(0, 100 - (stats.AverageResponseTimeMs / MaxResponseTimeMs * 100));

            return (cpuScore + memoryScore + responseScore) / 3;
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _cpuCounter?.Dispose();
                        _memoryCounter?.Dispose();
                        _currentProcess?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing PerformanceOptimizationService");
                    }
                }
                
                base.Dispose(disposing);
                _disposed = true;
            }
        }

        #endregion
    }

    #region Performance Data Types

    /// <summary>
    /// Current performance statistics
    /// </summary>
    public class PerformanceStatistics
    {
        public DateTime Timestamp { get; set; }
        public int ProcessId { get; set; }
        
        // CPU metrics
        public double CurrentCpuUsagePercent { get; set; }
        public double AverageCpuUsagePercent { get; set; }
        public double MaxCpuUsagePercent { get; set; }
        
        // Memory metrics  
        public double CurrentMemoryUsageMB { get; set; }
        public double AverageMemoryUsageMB { get; set; }
        public double MaxMemoryUsageMB { get; set; }
        
        // Response time metrics
        public double CurrentResponseTimeMs { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double MaxResponseTimeMs { get; set; }
        
        // Compliance status
        public bool IsCpuCompliant { get; set; }
        public bool IsMemoryCompliant { get; set; }
        public bool IsResponseTimeCompliant { get; set; }
        public bool IsFullyCompliant { get; set; }
        
        // Optimization tracking
        public int OptimizationCount { get; set; }
        public DateTime LastOptimizationTime { get; set; }
    }

    /// <summary>
    /// Performance trends over time
    /// </summary>
    public class PerformanceTrends
    {
        public double CpuUsageTrend { get; set; }
        public double MemoryUsageTrend { get; set; }
        public double ResponseTimeTrend { get; set; }
        public double OverallTrend { get; set; }
        public string TrendAssessment { get; set; } = "Unknown";
    }

    /// <summary>
    /// Performance optimization recommendation
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Type { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ExpectedImprovement { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complete performance report
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public PerformanceStatistics Statistics { get; set; } = new();
        public PerformanceTrends Trends { get; set; } = new();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
        public string ComplianceStatus { get; set; } = string.Empty;
        public double OverallScore { get; set; }
    }

    /// <summary>
    /// Recommendation priority levels
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}
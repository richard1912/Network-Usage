using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetworkUsage.Contracts;

namespace NetworkUsage.Services
{
    /// <summary>
    /// Real-time network traffic monitoring service implementing INetworkMonitor
    /// Provides network adapter detection, traffic measurement, and speed calculation
    /// Performance target: <100ms response times, <1% CPU usage
    /// </summary>
    public class NetworkMonitorService : INetworkMonitor, IDisposable
    {
        private readonly ILogger<NetworkMonitorService> _logger;
        private readonly object _lock = new object();
        
        // Monitoring state
        private bool _isMonitoring = false;
        private NetworkAdapter? _activeAdapter = null;
        private NetworkTrafficData? _lastTrafficData = null;
        private Timer? _monitoringTimer = null;
        private TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
        private CancellationTokenSource? _cancellationTokenSource = null;

        // Adapter management
        private readonly Dictionary<string, NetworkAdapter> _availableAdapters = new();
        private readonly Dictionary<string, NetworkInterface> _systemInterfaces = new();
        private NetworkInterface? _activeSystemInterface = null;

        // Performance tracking
        private DateTime _lastUpdateTime = DateTime.Now;
        private readonly Queue<long> _performanceMetrics = new();
        private const int MaxPerformanceMetrics = 100;

        public NetworkMonitorService(ILogger<NetworkMonitorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("NetworkMonitorService initialized");
            
            // Initialize adapter discovery
            _ = Task.Run(DiscoverAvailableAdaptersAsync);
        }

        #region INetworkMonitor Events

        public event EventHandler<NetworkTrafficData>? TrafficDataUpdated;
        public event EventHandler<NetworkAdapter>? ActiveAdapterChanged;

        #endregion

        #region INetworkMonitor Properties

        public bool IsMonitoring 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isMonitoring; 
                } 
            } 
        }

        #endregion

        #region INetworkMonitor Methods

        /// <summary>
        /// Start monitoring network traffic on the primary active adapter
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                lock (_lock)
                {
                    if (_isMonitoring)
                    {
                        _logger.LogWarning("Monitoring is already active");
                        return;
                    }
                }

                _logger.LogInformation("Starting network monitoring...");

                // Ensure we have adapters available
                await DiscoverAvailableAdaptersAsync();

                // Select primary adapter if none is active
                if (_activeAdapter == null)
                {
                    await SelectPrimaryAdapterAsync();
                }

                if (_activeAdapter == null)
                {
                    throw new InvalidOperationException("No network adapters available for monitoring");
                }

                // Initialize monitoring state
                lock (_lock)
                {
                    _isMonitoring = true;
                    _cancellationTokenSource = new CancellationTokenSource();
                    _lastTrafficData = new NetworkTrafficData
                    {
                        AdapterName = _activeAdapter.Name,
                        AdapterType = _activeAdapter.Type,
                        Timestamp = DateTime.Now
                    };
                }

                // Start monitoring timer
                _monitoringTimer = new Timer(MonitoringCallback, null, TimeSpan.Zero, _updateInterval);

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation($"Network monitoring started successfully in {elapsedMs:F1}ms on adapter: {_activeAdapter.Name}");
                
                // Record performance metric
                RecordPerformanceMetric(elapsedMs);
                
                // Fire initial traffic data event
                TrafficDataUpdated?.Invoke(this, _lastTrafficData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start network monitoring");
                
                // Cleanup on failure
                lock (_lock)
                {
                    _isMonitoring = false;
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                
                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
                
                throw;
            }
        }

        /// <summary>
        /// Stop all network monitoring and cleanup resources
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Stopping network monitoring...");

                Timer? timerToDispose = null;
                CancellationTokenSource? cancellationSource = null;

                lock (_lock)
                {
                    if (!_isMonitoring)
                    {
                        _logger.LogWarning("Monitoring is not currently active");
                        return;
                    }

                    _isMonitoring = false;
                    
                    // Capture references for disposal outside lock
                    timerToDispose = _monitoringTimer;
                    cancellationSource = _cancellationTokenSource;
                    
                    _monitoringTimer = null;
                    _cancellationTokenSource = null;
                }

                // Dispose outside lock to avoid deadlocks
                cancellationSource?.Cancel();
                timerToDispose?.Dispose();
                cancellationSource?.Dispose();

                // Fire final traffic data with zero speeds
                if (_lastTrafficData != null)
                {
                    var finalData = new NetworkTrafficData(
                        _lastTrafficData.BytesReceived,
                        _lastTrafficData.BytesSent,
                        0, // Zero receive speed
                        0, // Zero send speed
                        _lastTrafficData.AdapterName,
                        _lastTrafficData.AdapterType
                    );

                    TrafficDataUpdated?.Invoke(this, finalData);
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation($"Network monitoring stopped successfully in {elapsedMs:F1}ms");
                
                RecordPerformanceMetric(elapsedMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while stopping network monitoring");
                throw;
            }
        }

        /// <summary>
        /// Get current network traffic snapshot without starting continuous monitoring
        /// </summary>
        public async Task<NetworkTrafficData> GetCurrentTrafficAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                if (_activeAdapter == null)
                {
                    await DiscoverAvailableAdaptersAsync();
                    await SelectPrimaryAdapterAsync();
                }

                if (_activeAdapter == null || _activeSystemInterface == null)
                {
                    throw new InvalidOperationException("No active network adapter available");
                }

                // Get current statistics from system interface
                var stats = _activeSystemInterface.GetIPStatistics();
                
                var trafficData = new NetworkTrafficData(
                    stats.BytesReceived,
                    stats.BytesSent,
                    0, // Speed calculation requires previous measurement
                    0, // Speed calculation requires previous measurement
                    _activeAdapter.Name,
                    _activeAdapter.Type
                );

                // If we have a previous measurement, calculate speeds
                if (_lastTrafficData != null && _isMonitoring)
                {
                    trafficData = NetworkTrafficData.CreateFromDelta(_lastTrafficData, trafficData);
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                if (elapsedMs > 100)
                {
                    _logger.LogWarning($"GetCurrentTrafficAsync took {elapsedMs:F1}ms (should be <100ms)");
                }

                return trafficData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current traffic data");
                throw;
            }
        }

        /// <summary>
        /// Get list of all available network adapters on the system
        /// </summary>
        public async Task<IEnumerable<NetworkAdapter>> GetAvailableAdaptersAsync()
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                await DiscoverAvailableAdaptersAsync();

                lock (_lock)
                {
                    var adapters = _availableAdapters.Values.ToList();
                    
                    // Sort by priority (Ethernet first, then by speed)
                    adapters.Sort();
                    
                    var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    RecordPerformanceMetric(elapsedMs);

                    if (elapsedMs > 500)
                    {
                        _logger.LogWarning($"GetAvailableAdaptersAsync took {elapsedMs:F1}ms (should be <500ms)");
                    }

                    _logger.LogDebug($"Retrieved {adapters.Count} available adapters in {elapsedMs:F1}ms");
                    return adapters;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available adapters");
                throw;
            }
        }

        /// <summary>
        /// Set which adapter to monitor (overrides automatic primary adapter selection)
        /// </summary>
        public async Task SetActiveAdapterAsync(string adapterId)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
                throw new ArgumentException("Adapter ID cannot be null or empty", nameof(adapterId));

            var startTime = DateTime.UtcNow;
            
            try
            {
                await DiscoverAvailableAdaptersAsync();

                NetworkAdapter? newActiveAdapter;
                NetworkAdapter? previousAdapter;

                lock (_lock)
                {
                    if (!_availableAdapters.TryGetValue(adapterId, out newActiveAdapter))
                    {
                        throw new ArgumentException($"Adapter with ID '{adapterId}' not found or not operational");
                    }

                    if (newActiveAdapter.Status != OperationalStatus.Up)
                    {
                        throw new ArgumentException($"Adapter '{adapterId}' is not operational (Status: {newActiveAdapter.Status})");
                    }

                    previousAdapter = _activeAdapter;
                    
                    // Update active adapter
                    if (previousAdapter != null)
                    {
                        previousAdapter.IsActive = false;
                    }
                    
                    newActiveAdapter.MarkAsActive();
                    _activeAdapter = newActiveAdapter;
                    
                    // Update system interface reference
                    if (_systemInterfaces.TryGetValue(adapterId, out var systemInterface))
                    {
                        _activeSystemInterface = systemInterface;
                    }
                }

                // Reset traffic data for new adapter
                if (_isMonitoring)
                {
                    _lastTrafficData?.ResetToInitialState(newActiveAdapter.Name, newActiveAdapter.Type);
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"Active adapter changed from '{previousAdapter?.Name}' to '{newActiveAdapter.Name}' in {elapsedMs:F1}ms");

                // Fire adapter changed event
                ActiveAdapterChanged?.Invoke(this, newActiveAdapter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set active adapter to '{adapterId}'");
                throw;
            }
        }

        /// <summary>
        /// Get the currently monitored network adapter
        /// </summary>
        public NetworkAdapter GetActiveAdapter()
        {
            lock (_lock)
            {
                if (_activeAdapter == null)
                {
                    throw new InvalidOperationException("No active network adapter is currently set");
                }
                return _activeAdapter.Clone(); // Return copy for thread safety
            }
        }

        /// <summary>
        /// Configure monitoring update interval
        /// </summary>
        public async Task SetUpdateIntervalAsync(TimeSpan interval)
        {
            if (interval < TimeSpan.FromMilliseconds(500) || interval > TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), 
                    "Update interval must be between 500ms and 10 seconds");
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                bool wasMonitoring = false;

                lock (_lock)
                {
                    if (_updateInterval == interval)
                    {
                        return; // No change needed
                    }

                    _updateInterval = interval;
                    wasMonitoring = _isMonitoring;
                }

                // Update timer if monitoring is active
                if (wasMonitoring && _monitoringTimer != null)
                {
                    _monitoringTimer.Change(TimeSpan.Zero, interval);
                }

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordPerformanceMetric(elapsedMs);

                _logger.LogInformation($"Update interval changed to {interval.TotalMilliseconds}ms in {elapsedMs:F1}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set update interval to {interval}");
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Discover and update available network adapters
        /// </summary>
        private async Task DiscoverAvailableAdaptersAsync()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                var discoveredAdapters = new Dictionary<string, NetworkAdapter>();
                var discoveredInterfaces = new Dictionary<string, NetworkInterface>();

                foreach (var networkInterface in interfaces)
                {
                    // Filter out loopback and virtual adapters
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.Name.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                        networkInterface.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var adapter = CreateNetworkAdapterFromInterface(networkInterface);
                    discoveredAdapters[adapter.Id] = adapter;
                    discoveredInterfaces[adapter.Id] = networkInterface;
                }

                lock (_lock)
                {
                    _availableAdapters.Clear();
                    _systemInterfaces.Clear();
                    
                    foreach (var kvp in discoveredAdapters)
                    {
                        _availableAdapters[kvp.Key] = kvp.Value;
                    }
                    
                    foreach (var kvp in discoveredInterfaces)
                    {
                        _systemInterfaces[kvp.Key] = kvp.Value;
                    }
                }

                _logger.LogDebug($"Discovered {discoveredAdapters.Count} network adapters");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover network adapters");
                throw;
            }
        }

        /// <summary>
        /// Create NetworkAdapter from .NET NetworkInterface
        /// </summary>
        private static NetworkAdapter CreateNetworkAdapterFromInterface(NetworkInterface networkInterface)
        {
            var adapter = new NetworkAdapter(
                networkInterface.Id,
                networkInterface.Name,
                networkInterface.Description,
                networkInterface.NetworkInterfaceType,
                networkInterface.OperationalStatus,
                networkInterface.Speed
            );

            // Get IP address if available
            var ipProperties = networkInterface.GetIPProperties();
            var ipv4Address = ipProperties.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            
            if (ipv4Address != null)
            {
                adapter.IPv4Address = ipv4Address.Address.ToString();
            }

            // Get MAC address
            var macAddress = networkInterface.GetPhysicalAddress();
            if (macAddress != null && macAddress.GetAddressBytes().Length > 0)
            {
                adapter.MacAddress = string.Join(":", macAddress.GetAddressBytes().Select(b => b.ToString("X2")));
            }

            return adapter;
        }

        /// <summary>
        /// Select the primary adapter based on priority
        /// </summary>
        private async Task SelectPrimaryAdapterAsync()
        {
            try
            {
                await DiscoverAvailableAdaptersAsync();

                NetworkAdapter? primaryAdapter = null;

                lock (_lock)
                {
                    // Find the highest priority operational adapter
                    primaryAdapter = _availableAdapters.Values
                        .Where(a => a.Status == OperationalStatus.Up)
                        .OrderByDescending(a => a.GetPriority())
                        .FirstOrDefault();
                }

                if (primaryAdapter != null)
                {
                    await SetActiveAdapterAsync(primaryAdapter.Id);
                }
                else
                {
                    _logger.LogWarning("No operational network adapters found for primary selection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select primary adapter");
                throw;
            }
        }

        /// <summary>
        /// Timer callback for monitoring network traffic
        /// </summary>
        private async void MonitoringCallback(object? state)
        {
            if (!_isMonitoring || _cancellationTokenSource?.Token.IsCancellationRequested == true)
                return;

            try
            {
                var currentTraffic = await GetCurrentTrafficAsync();
                
                lock (_lock)
                {
                    _lastTrafficData = currentTraffic;
                }

                // Fire traffic updated event
                TrafficDataUpdated?.Invoke(this, currentTraffic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring callback");
                
                // Consider stopping monitoring on repeated failures
                if (!_cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    await StopMonitoringAsync();
                }
            }
        }

        /// <summary>
        /// Record performance metric for monitoring
        /// </summary>
        private void RecordPerformanceMetric(double elapsedMs)
        {
            lock (_performanceMetrics)
            {
                _performanceMetrics.Enqueue((long)elapsedMs);
                
                if (_performanceMetrics.Count > MaxPerformanceMetrics)
                {
                    _performanceMetrics.Dequeue();
                }
            }
        }

        /// <summary>
        /// Get average performance metrics for monitoring
        /// </summary>
        public double GetAveragePerformanceMs()
        {
            lock (_performanceMetrics)
            {
                return _performanceMetrics.Count > 0 ? _performanceMetrics.Average() : 0;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Stop monitoring if active
                        if (_isMonitoring)
                        {
                            StopMonitoringAsync().Wait(TimeSpan.FromSeconds(5));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing NetworkMonitorService");
                    }
                    finally
                    {
                        _monitoringTimer?.Dispose();
                        _cancellationTokenSource?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Contract for real-time network traffic monitoring functionality
    /// Responsible for: Network adapter detection, traffic measurement, speed calculation
    /// </summary>
    public interface INetworkMonitor
    {
        /// <summary>
        /// Event fired when new network traffic data is available
        /// Frequency: Based on UpdateInterval in configuration (default: 1 second)
        /// </summary>
        event EventHandler<NetworkTrafficData> TrafficDataUpdated;

        /// <summary>
        /// Event fired when the active network adapter changes
        /// Triggers: Adapter disconnect, new adapter detected, manual adapter switch
        /// </summary>
        event EventHandler<NetworkAdapter> ActiveAdapterChanged;

        /// <summary>
        /// Start monitoring network traffic on the primary active adapter
        /// Must: Identify primary adapter, begin polling, fire initial TrafficDataUpdated event
        /// Throws: InvalidOperationException if no adapters available
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stop all network monitoring and cleanup resources
        /// Must: Stop polling timer, clear events, dispose resources
        /// Should: Fire final TrafficDataUpdated with zero speeds
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Get current network traffic snapshot without starting continuous monitoring
        /// Returns: Current NetworkTrafficData for active adapter
        /// Throws: InvalidOperationException if no active adapter
        /// Performance: Must complete within 100ms per FR-013
        /// </summary>
        Task<NetworkTrafficData> GetCurrentTrafficAsync();

        /// <summary>
        /// Get list of all available network adapters on the system
        /// Returns: Collection of NetworkAdapter objects with current status
        /// Must: Filter out virtual/loopback adapters, sort by preference (Ethernet, then Wireless)
        /// Performance: Must complete within 500ms
        /// </summary>
        Task<IEnumerable<NetworkAdapter>> GetAvailableAdaptersAsync();

        /// <summary>
        /// Set which adapter to monitor (overrides automatic primary adapter selection)
        /// Parameters: adapterId - Unique identifier of adapter to monitor
        /// Must: Validate adapter exists and is operational, switch monitoring target
        /// Throws: ArgumentException if adapter not found or not operational
        /// </summary>
        Task SetActiveAdapterAsync(string adapterId);

        /// <summary>
        /// Get the currently monitored network adapter
        /// Returns: NetworkAdapter currently being monitored, null if none active
        /// Must: Return immediately from cached value
        /// </summary>
        NetworkAdapter GetActiveAdapter();

        /// <summary>
        /// Check if monitoring is currently active
        /// Returns: True if StartMonitoringAsync called and StopMonitoringAsync not called
        /// Must: Return immediately from cached state
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Configure monitoring update interval
        /// Parameters: interval - Time between traffic measurements (500ms to 10s per validation rules)
        /// Must: Update polling frequency without stopping/restarting monitoring
        /// Throws: ArgumentOutOfRangeException if interval outside valid range
        /// </summary>
        Task SetUpdateIntervalAsync(TimeSpan interval);
    }

    /// <summary>
    /// Event arguments for network monitoring events
    /// </summary>
    public class NetworkTrafficEventArgs : EventArgs
    {
        public NetworkTrafficData TrafficData { get; set; }
        public NetworkAdapter Adapter { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

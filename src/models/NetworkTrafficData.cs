using System;
using System.Net.NetworkInformation;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Real-time network usage metrics for display and calculation purposes
    /// Based on data-model.md specification with validation rules and state transitions
    /// </summary>
    public class NetworkTrafficData : IEquatable<NetworkTrafficData>
    {
        private long _bytesReceived;
        private long _bytesSent;
        private double _receiveSpeed;
        private double _sendSpeed;
        private string _adapterName = string.Empty;

        /// <summary>
        /// Total bytes received since monitoring started
        /// Must be >= 0 per validation rules
        /// </summary>
        public long BytesReceived 
        { 
            get => _bytesReceived;
            set 
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BytesReceived must be >= 0");
                _bytesReceived = value;
            }
        }

        /// <summary>
        /// Total bytes sent since monitoring started
        /// Must be >= 0 per validation rules
        /// </summary>
        public long BytesSent 
        { 
            get => _bytesSent;
            set 
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "BytesSent must be >= 0");
                _bytesSent = value;
            }
        }

        /// <summary>
        /// Current download speed in bytes per second
        /// Must be >= 0 per validation rules
        /// </summary>
        public double ReceiveSpeed 
        { 
            get => _receiveSpeed;
            set 
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "ReceiveSpeed must be >= 0");
                _receiveSpeed = value;
            }
        }

        /// <summary>
        /// Current upload speed in bytes per second
        /// Must be >= 0 per validation rules
        /// </summary>
        public double SendSpeed 
        { 
            get => _sendSpeed;
            set 
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "SendSpeed must be >= 0");
                _sendSpeed = value;
            }
        }

        /// <summary>
        /// When this measurement was taken
        /// Must not be in the future per validation rules
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Name of the monitored network adapter
        /// Must not be null or empty per validation rules
        /// </summary>
        public string AdapterName 
        { 
            get => _adapterName;
            set 
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("AdapterName must not be null or empty", nameof(value));
                _adapterName = value;
            }
        }

        /// <summary>
        /// Type of network connection (Ethernet, Wireless, etc.)
        /// </summary>
        public NetworkInterfaceType AdapterType { get; set; } = NetworkInterfaceType.Unknown;

        /// <summary>
        /// Default constructor - creates initial state with all numeric values = 0
        /// </summary>
        public NetworkTrafficData()
        {
            // Initial state: All numeric values = 0, Timestamp = DateTime.Now (per data-model.md)
            _bytesReceived = 0;
            _bytesSent = 0;
            _receiveSpeed = 0;
            _sendSpeed = 0;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Constructor with all values
        /// </summary>
        public NetworkTrafficData(long bytesReceived, long bytesSent, double receiveSpeed, double sendSpeed, 
            string adapterName, NetworkInterfaceType adapterType = NetworkInterfaceType.Unknown)
        {
            BytesReceived = bytesReceived; // Uses property for validation
            BytesSent = bytesSent;
            ReceiveSpeed = receiveSpeed;
            SendSpeed = sendSpeed;
            AdapterName = adapterName;
            AdapterType = adapterType;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Calculate speeds based on byte difference and time delta (for update state transition)
        /// </summary>
        public static NetworkTrafficData CreateFromDelta(NetworkTrafficData previous, NetworkTrafficData current)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (current.Timestamp <= previous.Timestamp)
                throw new ArgumentException("Current timestamp must be after previous timestamp");

            var timeDeltaSeconds = (current.Timestamp - previous.Timestamp).TotalSeconds;
            if (timeDeltaSeconds <= 0) timeDeltaSeconds = 1; // Prevent division by zero

            var receiveSpeed = Math.Max(0, (current.BytesReceived - previous.BytesReceived) / timeDeltaSeconds);
            var sendSpeed = Math.Max(0, (current.BytesSent - previous.BytesSent) / timeDeltaSeconds);

            return new NetworkTrafficData(
                current.BytesReceived,
                current.BytesSent,
                receiveSpeed,
                sendSpeed,
                current.AdapterName,
                current.AdapterType
            )
            {
                Timestamp = current.Timestamp
            };
        }

        /// <summary>
        /// Reset to initial state (when adapter changes per state transition)
        /// </summary>
        public void ResetToInitialState(string newAdapterName, NetworkInterfaceType newAdapterType = NetworkInterfaceType.Unknown)
        {
            if (string.IsNullOrEmpty(newAdapterName))
                throw new ArgumentException("AdapterName must not be null or empty", nameof(newAdapterName));

            _bytesReceived = 0;
            _bytesSent = 0;
            _receiveSpeed = 0;
            _sendSpeed = 0;
            _adapterName = newAdapterName;
            AdapterType = newAdapterType;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Validate that the traffic data meets all validation rules from data-model.md
        /// </summary>
        public bool IsValid()
        {
            return BytesReceived >= 0 &&
                   BytesSent >= 0 &&
                   ReceiveSpeed >= 0 &&
                   SendSpeed >= 0 &&
                   Timestamp <= DateTime.Now.AddSeconds(1) && // Allow small time tolerance
                   !string.IsNullOrEmpty(AdapterName);
        }

        #region IEquatable<NetworkTrafficData> Implementation

        public bool Equals(NetworkTrafficData? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            return BytesReceived == other.BytesReceived &&
                   BytesSent == other.BytesSent &&
                   Math.Abs(ReceiveSpeed - other.ReceiveSpeed) < 0.001 && // Small tolerance for double comparison
                   Math.Abs(SendSpeed - other.SendSpeed) < 0.001 &&
                   AdapterName == other.AdapterName &&
                   AdapterType == other.AdapterType &&
                   Math.Abs((Timestamp - other.Timestamp).TotalMilliseconds) < 1000; // 1 second tolerance
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as NetworkTrafficData);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                BytesReceived,
                BytesSent,
                Math.Round(ReceiveSpeed, 3),
                Math.Round(SendSpeed, 3),
                AdapterName,
                AdapterType,
                Timestamp.Date // Use date for more stable hash
            );
        }

        public static bool operator ==(NetworkTrafficData? left, NetworkTrafficData? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(NetworkTrafficData? left, NetworkTrafficData? right)
        {
            return !(left == right);
        }

        #endregion

        /// <summary>
        /// String representation for debugging and logging
        /// </summary>
        public override string ToString()
        {
            return $"NetworkTrafficData: {AdapterName} ({AdapterType}) - " +
                   $"Received: {BytesReceived:N0} bytes ({ReceiveSpeed:F1} B/s), " +
                   $"Sent: {BytesSent:N0} bytes ({SendSpeed:F1} B/s) - {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Create a copy of this traffic data for thread-safe operations
        /// </summary>
        public NetworkTrafficData Clone()
        {
            return new NetworkTrafficData(BytesReceived, BytesSent, ReceiveSpeed, SendSpeed, AdapterName, AdapterType)
            {
                Timestamp = Timestamp
            };
        }
    }
}
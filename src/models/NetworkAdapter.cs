using System;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace NetworkUsage.Contracts
{
    /// <summary>
    /// Represents a monitored network interface with its current status and capabilities
    /// Based on data-model.md specification with validation rules and state transitions
    /// </summary>
    public class NetworkAdapter : IEquatable<NetworkAdapter>, IComparable<NetworkAdapter>
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private long _speed;
        private string _ipv4Address = string.Empty;
        private string _macAddress = string.Empty;

        /// <summary>
        /// Unique identifier for the adapter
        /// Must be unique across all adapters per validation rules
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Id must not be null or empty", nameof(value));
                _id = value.Trim();
            }
        }

        /// <summary>
        /// Display name of the adapter
        /// Must not be null or empty per validation rules
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name must not be null or empty", nameof(value));
                _name = value.Trim();
            }
        }

        /// <summary>
        /// Full description of the adapter
        /// Must not be null or empty per validation rules
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Description must not be null or empty", nameof(value));
                _description = value.Trim();
            }
        }

        /// <summary>
        /// Connection type (Ethernet, Wireless, etc.)
        /// </summary>
        public NetworkInterfaceType Type { get; set; } = NetworkInterfaceType.Unknown;

        /// <summary>
        /// Current operational status (Up, Down, Testing, etc.)
        /// </summary>
        public OperationalStatus Status { get; set; } = OperationalStatus.Unknown;

        /// <summary>
        /// Maximum speed capability in bits per second
        /// Must be > 0 when Status is Up per validation rules
        /// </summary>
        public long Speed
        {
            get => _speed;
            set
            {
                if (Status == OperationalStatus.Up && value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Speed must be > 0 when Status is Up");
                _speed = Math.Max(0, value); // Ensure non-negative
            }
        }

        /// <summary>
        /// Whether this adapter is currently being monitored
        /// Only one adapter should be active at a time
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// When last network activity was detected
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.Now;

        /// <summary>
        /// Current IPv4 address (if available)
        /// Must be valid IP format when not null per validation rules
        /// </summary>
        public string IPv4Address
        {
            get => _ipv4Address;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ValidateIPv4Address(value);
                }
                _ipv4Address = value?.Trim() ?? string.Empty;
            }
        }

        /// <summary>
        /// Physical MAC address
        /// Must be valid MAC format when not null per validation rules
        /// </summary>
        public string MacAddress
        {
            get => _macAddress;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    ValidateMacAddress(value);
                }
                _macAddress = value?.Trim().ToUpperInvariant() ?? string.Empty;
            }
        }

        /// <summary>
        /// Default constructor - creates adapter in Unknown state
        /// </summary>
        public NetworkAdapter()
        {
            _id = Guid.NewGuid().ToString();
            _name = "Unknown Adapter";
            _description = "Unknown Network Adapter";
            Type = NetworkInterfaceType.Unknown;
            Status = OperationalStatus.Unknown;
            _speed = 0;
            IsActive = false;
            LastActivity = DateTime.Now;
        }

        /// <summary>
        /// Constructor with all values
        /// </summary>
        public NetworkAdapter(string id, string name, string description, NetworkInterfaceType type,
            OperationalStatus status, long speed, bool isActive = false, string ipv4Address = "", string macAddress = "")
        {
            Id = id; // Uses property for validation
            Name = name;
            Description = description;
            Type = type;
            Status = status;
            Speed = speed; // Will validate based on status
            IsActive = isActive;
            IPv4Address = ipv4Address;
            MacAddress = macAddress;
            LastActivity = DateTime.Now;
        }

        #region State Transitions (from data-model.md)

        /// <summary>
        /// Transition from Detected → Available state
        /// </summary>
        public void MarkAsAvailable()
        {
            if (Status == OperationalStatus.Unknown)
            {
                Status = OperationalStatus.Down; // Available but not necessarily up
            }
        }

        /// <summary>
        /// Transition from Available → Active state
        /// </summary>
        public void MarkAsActive()
        {
            IsActive = true;
            LastActivity = DateTime.Now;
            if (Status == OperationalStatus.Down)
            {
                Status = OperationalStatus.Up;
            }
        }

        /// <summary>
        /// Transition from Active → Unavailable state
        /// </summary>
        public void MarkAsUnavailable()
        {
            IsActive = false;
            Status = OperationalStatus.Down;
        }

        /// <summary>
        /// Transition from Unavailable → Available state (connection restored)
        /// </summary>
        public void RestoreConnection()
        {
            Status = OperationalStatus.Up;
            LastActivity = DateTime.Now;
        }

        #endregion

        #region Validation Methods

        private static void ValidateIPv4Address(string ipAddress)
        {
            var ipRegex = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            if (!ipRegex.IsMatch(ipAddress))
            {
                throw new ArgumentException($"Invalid IPv4 address format: {ipAddress}", nameof(ipAddress));
            }
        }

        private static void ValidateMacAddress(string macAddress)
        {
            var macRegex = new Regex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
            if (!macRegex.IsMatch(macAddress))
            {
                throw new ArgumentException($"Invalid MAC address format: {macAddress}", nameof(macAddress));
            }
        }

        /// <summary>
        /// Validate all adapter properties according to data-model.md rules
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Check required properties
                if (string.IsNullOrWhiteSpace(Id) || 
                    string.IsNullOrWhiteSpace(Name) || 
                    string.IsNullOrWhiteSpace(Description))
                    return false;

                // Validate speed when up
                if (Status == OperationalStatus.Up && Speed <= 0)
                    return false;

                // Validate IP and MAC if provided
                if (!string.IsNullOrEmpty(IPv4Address))
                    ValidateIPv4Address(IPv4Address);

                if (!string.IsNullOrEmpty(MacAddress))
                    ValidateMacAddress(MacAddress);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Adapter Priority and Comparison

        /// <summary>
        /// Get adapter priority for automatic selection
        /// Higher values = higher priority
        /// Ethernet > Wireless > Others, then by speed
        /// </summary>
        public int GetPriority()
        {
            if (Status != OperationalStatus.Up)
                return 0; // Unavailable adapters have lowest priority

            int typePriority = Type switch
            {
                NetworkInterfaceType.Ethernet => 1000,
                NetworkInterfaceType.GigabitEthernet => 1100,
                NetworkInterfaceType.Ethernet3Megabit => 900,
                NetworkInterfaceType.FastEthernetT => 950,
                NetworkInterfaceType.FastEthernetFx => 950,
                NetworkInterfaceType.Wireless80211 => 500,
                NetworkInterfaceType.Ppp => 200,
                NetworkInterfaceType.Loopback => 1, // Very low priority
                _ => 100
            };

            // Add speed bonus (normalized to avoid overflow)
            int speedBonus = (int)Math.Min(Speed / 1_000_000, 999); // Speed in Mbps, max 999
            
            return typePriority + speedBonus;
        }

        /// <summary>
        /// Compare adapters by priority for sorting
        /// </summary>
        public int CompareTo(NetworkAdapter? other)
        {
            if (other == null) return 1;
            
            // Sort by priority (descending), then by name (ascending)
            int priorityComparison = other.GetPriority().CompareTo(GetPriority());
            return priorityComparison != 0 ? priorityComparison : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region IEquatable<NetworkAdapter> Implementation (Identity-based)

        public bool Equals(NetworkAdapter? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Identity-based equality - same ID means same adapter
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as NetworkAdapter);
        }

        public override int GetHashCode()
        {
            // Hash based on ID for identity-based equality
            return Id.GetHashCode();
        }

        public static bool operator ==(NetworkAdapter? left, NetworkAdapter? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(NetworkAdapter? left, NetworkAdapter? right)
        {
            return !(left == right);
        }

        #endregion

        /// <summary>
        /// Create a copy of this adapter for thread-safe operations
        /// </summary>
        public NetworkAdapter Clone()
        {
            return new NetworkAdapter(Id, Name, Description, Type, Status, Speed, IsActive, IPv4Address, MacAddress)
            {
                LastActivity = LastActivity
            };
        }

        /// <summary>
        /// Get a user-friendly status description
        /// </summary>
        public string GetStatusDescription()
        {
            return Status switch
            {
                OperationalStatus.Up => IsActive ? "Active - Monitoring" : "Connected",
                OperationalStatus.Down => "Disconnected",
                OperationalStatus.Testing => "Testing Connection",
                OperationalStatus.Dormant => "Dormant",
                OperationalStatus.NotPresent => "Not Present",
                OperationalStatus.LowerLayerDown => "Hardware Issue",
                _ => "Unknown Status"
            };
        }

        /// <summary>
        /// Get formatted speed string
        /// </summary>
        public string GetSpeedDescription()
        {
            if (Speed <= 0) return "Unknown Speed";
            
            return Speed switch
            {
                >= 10_000_000_000 => $"{Speed / 1_000_000_000.0:F1} Gbps",
                >= 1_000_000_000 => $"{Speed / 1_000_000_000.0:F1} Gbps",
                >= 1_000_000 => $"{Speed / 1_000_000:F0} Mbps",
                >= 1_000 => $"{Speed / 1_000:F0} Kbps",
                _ => $"{Speed} bps"
            };
        }

        /// <summary>
        /// String representation for debugging and logging
        /// </summary>
        public override string ToString()
        {
            var activeStatus = IsActive ? " [ACTIVE]" : "";
            var ipInfo = !string.IsNullOrEmpty(IPv4Address) ? $" IP:{IPv4Address}" : "";
            
            return $"NetworkAdapter: {Name} ({Type}) - {GetStatusDescription()}{activeStatus} - " +
                   $"{GetSpeedDescription()}{ipInfo} - {Id}";
        }
    }
}
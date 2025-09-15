# Data Model: Windows 11 Taskbar Network Traffic Monitor

**Date**: 2025-09-15  
**Phase**: Phase 1 Design  

## Core Entities

### NetworkTrafficData
Real-time network usage metrics for display and calculation purposes.

**Fields**:
- `BytesReceived`: long - Total bytes received since monitoring started
- `BytesSent`: long - Total bytes sent since monitoring started  
- `ReceiveSpeed`: double - Current download speed in bytes per second
- `SendSpeed`: double - Current upload speed in bytes per second
- `Timestamp`: DateTime - When this measurement was taken
- `AdapterName`: string - Name of the monitored network adapter
- `AdapterType`: NetworkInterfaceType - Type of network connection (Ethernet, Wireless, etc.)

**Validation Rules**:
- BytesReceived and BytesSent must be >= 0
- ReceiveSpeed and SendSpeed must be >= 0
- Timestamp must not be in the future
- AdapterName must not be null or empty

**State Transitions**:
- Initial state: All numeric values = 0, Timestamp = DateTime.Now
- Update state: Calculate speeds based on byte difference and time delta
- Reset state: Return to initial state when adapter changes

### DisplayConfiguration  
User preferences and system settings for UI display behavior.

**Fields**:
- `UpdateInterval`: TimeSpan - How often to refresh network data (default: 1 second)
- `AutoScaleUnits`: bool - Whether to auto-scale B/s → KB/s → MB/s → GB/s (default: true)
- `CurrentTheme`: WindowsTheme - Light, Dark, or HighContrast
- `ShowInSystemTray`: bool - Whether to show icon in system tray (default: true)
- `ToolTipFormat`: string - Format string for tray icon tooltip
- `ResponseTimeoutMs`: int - Maximum time to wait for UI responses (default: 100ms)

**Validation Rules**:
- UpdateInterval must be between 500ms and 10 seconds
- ResponseTimeoutMs must be between 50ms and 1000ms
- ToolTipFormat must be a valid .NET format string

**Default Values**:
- UpdateInterval: 1 second
- AutoScaleUnits: true
- ShowInSystemTray: true
- ToolTipFormat: "↓{0} ↑{1}"
- ResponseTimeoutMs: 100

### NetworkAdapter
Represents a monitored network interface with its current status and capabilities.

**Fields**:
- `Id`: string - Unique identifier for the adapter
- `Name`: string - Display name of the adapter
- `Description`: string - Full description of the adapter
- `Type`: NetworkInterfaceType - Connection type (Ethernet, Wireless, etc.)
- `Status`: OperationalStatus - Current operational status (Up, Down, Testing, etc.)
- `Speed`: long - Maximum speed capability in bits per second
- `IsActive`: bool - Whether this adapter is currently being monitored
- `LastActivity`: DateTime - When last network activity was detected
- `IPv4Address`: string - Current IPv4 address (if available)
- `MacAddress`: string - Physical MAC address

**Validation Rules**:
- Id must be unique across all adapters
- Name and Description must not be null or empty
- Speed must be > 0 when Status is Up
- IPv4Address must be valid IP format when not null
- MacAddress must be valid MAC format when not null

**State Transitions**:
- Detected → Available: Adapter found during system scan
- Available → Active: Selected as primary monitoring target
- Active → Unavailable: Connection lost or adapter disabled
- Unavailable → Available: Connection restored

## Value Objects

### SpeedReading
Immutable representation of a speed measurement with automatic unit scaling.

**Fields**:
- `BytesPerSecond`: double - Raw speed in bytes per second
- `DisplayValue`: double - Scaled value for display (e.g., 1.5 for 1.5 MB/s)
- `DisplayUnit`: SpeedUnit - Unit for display (Bytes, KB, MB, GB)
- `FormattedString`: string - Ready-to-display string (e.g., "1.5 MB/s")

**Methods**:
- `FromBytesPerSecond(double bytes)`: Creates SpeedReading with auto-scaling
- `ToString()`: Returns FormattedString
- `CompareTo(SpeedReading other)`: Compares based on BytesPerSecond

### WindowsTheme (Enumeration)
Available Windows theme options.

**Values**:
- `Light`: Standard light theme
- `Dark`: Standard dark theme  
- `HighContrast`: High contrast accessibility theme
- `Auto`: Automatically detect and follow system theme

### SpeedUnit (Enumeration)
Units for network speed display.

**Values**:
- `Bytes`: Bytes per second (B/s)
- `Kilobytes`: Kilobytes per second (KB/s)
- `Megabytes`: Megabytes per second (MB/s)
- `Gigabytes`: Gigabytes per second (GB/s)

## Data Relationships

### Primary Relationships
- NetworkAdapter (1) → NetworkTrafficData (many): One adapter produces multiple traffic measurements over time
- DisplayConfiguration (1) → NetworkTrafficData (many): Configuration affects how traffic data is displayed
- NetworkTrafficData (1) → SpeedReading (2): Each traffic measurement contains upload and download speed readings

### Derived Relationships
- Current Active Adapter: Single NetworkAdapter where IsActive = true
- Latest Traffic Data: Most recent NetworkTrafficData for active adapter
- Theme-Specific Configuration: DisplayConfiguration values adjusted based on CurrentTheme

## Data Flow Patterns

### Real-time Monitoring Flow
1. NetworkAdapter.IsActive = true triggers monitoring
2. Background service samples NetworkAdapter every UpdateInterval
3. New NetworkTrafficData created with calculated speeds
4. SpeedReading objects created for display formatting
5. UI components receive formatted data for display

### Theme Change Flow
1. System theme change detected
2. DisplayConfiguration.CurrentTheme updated
3. UI resources reloaded based on new theme
4. All display formatting updated to match theme

### Adapter Change Flow  
1. Current NetworkAdapter.Status changes to Down/Unavailable
2. System scans for new primary adapter
3. New NetworkAdapter.IsActive = true, old adapter IsActive = false
4. NetworkTrafficData reset to initial state for new adapter
5. Monitoring continues with new adapter

## Persistence Strategy
**Real-time Only**: No persistent storage required per FR-011
- All data exists only in memory during application runtime
- Configuration settings stored in Windows registry/user settings
- Application state resets on restart (intentional design)

## Performance Considerations
- NetworkTrafficData objects created at 1-second intervals (86,400 per day maximum)
- SpeedReading objects are lightweight value types
- DisplayConfiguration cached and only updated on theme/settings changes
- NetworkAdapter list refreshed only when connection changes detected

## Thread Safety
- NetworkTrafficData: Read-only after creation (immutable measurement snapshots)
- DisplayConfiguration: Thread-safe property access with lock mechanisms
- NetworkAdapter: Status updates synchronized through background service
- SpeedReading: Immutable value objects, inherently thread-safe

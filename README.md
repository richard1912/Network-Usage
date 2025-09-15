# Network Usage Monitor

A real-time network traffic monitoring application that integrates seamlessly into Windows 11 taskbar with minimalistic display and modern GUI for detailed statistics.

## Features

- **Real-time Network Monitoring**: Monitor network traffic with automatic adapter detection
- **Windows 11 Integration**: Seamless taskbar integration with modern Fluent Design
- **Automatic Theme Adaptation**: Follows Windows 11 light/dark/high contrast themes
- **Performance Optimized**: <1% CPU usage, <50MB RAM, <100ms response times
- **Multi-Adapter Support**: Automatic switching between Ethernet, WiFi, and other adapters
- **Modern GUI**: Detailed statistics window with Windows 11 styling
- **CLI Interface**: Complete command-line interface for automation and scripting

## Requirements

- **Operating System**: Windows 11 (with Windows 10 compatibility)
- **Runtime**: .NET 8.0 or higher
- **Permissions**: Standard user permissions (no admin required)
- **Hardware**: Any network adapter (Ethernet, WiFi, etc.)

## Installation

### Option 1: Download Release
1. Go to [Releases](../../releases) page
2. Download the latest `NetworkUsage-v1.0.0.zip`
3. Extract to your preferred location (e.g., `C:\Program Files\NetworkUsage\`)
4. Run `NetworkUsage.exe`

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/richard1912/Network-Usage.git
cd Network-Usage

# Build the application
dotnet build NetworkUsage.sln --configuration Release

# Run the application
dotnet run --project NetworkUsage.csproj
```

### Option 3: Install from Package Manager
```powershell
# Using winget (when published)
winget install NetworkUsage

# Using Chocolatey (when published)
choco install networkusage
```

## Quick Start

### 1. Basic Usage
```bash
# Start the application normally (GUI mode)
NetworkUsage.exe

# Start minimized to system tray
NetworkUsage.exe --minimized

# Run in CLI mode
NetworkUsage.exe --cli
```

### 2. System Tray Operation
- **Left Click**: Show/hide main window
- **Right Click**: Context menu (Show Statistics, Settings, Exit)
- **Hover**: View current network speeds in tooltip
- **Minimize**: Application minimizes to system tray

### 3. Main Window Interface
- **Current Status Tab**: Real-time download/upload speeds and adapter info
- **Network Adapters Tab**: View and switch between available adapters
- **Settings Tab**: Configure update intervals, themes, and display formats

## Usage Scenarios

### Scenario 1: Real-time Network Monitoring
```bash
# Start monitoring
NetworkUsage.exe

# The application will:
# 1. Automatically detect your primary network adapter
# 2. Show real-time speeds in the system tray tooltip
# 3. Update every second with current network activity
# 4. Auto-scale units (B/s → KB/s → MB/s → GB/s)
```

### Scenario 2: CLI Monitoring
```bash
# Monitor network traffic via CLI
NetworkUsage.exe network-monitor monitor --format text --interval 1000

# List available adapters
NetworkUsage.exe network-monitor list-adapters

# Set active adapter
NetworkUsage.exe network-monitor set-adapter --name "Ethernet"

# Get current status
NetworkUsage.exe network-monitor status --format json
```

### Scenario 3: Custom Display Format
```bash
# Change tooltip format via GUI
# Settings Tab → Tooltip Format → Enter: "📥{0} 📤{1}"

# Or via CLI
NetworkUsage.exe taskbar format --set "📥{0} 📤{1}"
```

### Scenario 4: Theme Customization
```bash
# Set theme via CLI
NetworkUsage.exe ui theme --set dark

# Or via taskbar CLI
NetworkUsage.exe taskbar theme --set light

# Auto-detect system theme (default)
NetworkUsage.exe ui theme --set auto
```

## Configuration

### Application Settings (`appsettings.json`)
```json
{
  "Display": {
    "UpdateInterval": "00:00:01",
    "AutoScaleUnits": true,
    "CurrentTheme": "Auto",
    "ShowInSystemTray": true,
    "ToolTipFormat": "↓{0} ↑{1}",
    "ResponseTimeoutMs": 100
  },
  "NetworkMonitoring": {
    "AutoSelectPrimaryAdapter": true,
    "FilterVirtualAdapters": true,
    "MonitoringIntervalMs": 1000
  },
  "Performance": {
    "MaxCpuUsagePercent": 1.0,
    "MaxMemoryUsageMB": 50,
    "MaxResponseTimeMs": 100
  }
}
```

### Command Line Arguments
- `--minimized` or `--tray`: Start minimized to system tray
- `--cli`: Run in command-line interface mode
- `--config [path]`: Use custom configuration file
- `--log-level [level]`: Set logging level (Debug, Info, Warning, Error)

## CLI Commands Reference

### Network Monitor Commands
```bash
# Start monitoring with JSON output
network-monitor monitor --format json --interval 2000 --output traffic.log

# List all network adapters
network-monitor list-adapters --format text

# Set active adapter by name or ID
network-monitor set-adapter --name "Wi-Fi" 
network-monitor set-adapter --adapter "adapter-id-12345"

# Get monitoring status
network-monitor status --format json

# Stop monitoring
network-monitor stop
```

### Taskbar Commands
```bash
# Show system tray icon with dark theme
taskbar show --theme dark

# Hide system tray icon
taskbar hide

# Update with test data
taskbar update --download 1500000 --upload 750000 --adapter "Test Adapter"

# Change tooltip format
taskbar format --set "Down: {0} | Up: {1}"

# Apply theme
taskbar theme --set light

# Check status
taskbar status --format json
```

### UI Commands
```bash
# Show statistics window
ui display-stats --theme auto --position 100,100

# Hide statistics window
ui hide-stats

# Update with test data
ui update --download 5000000 --upload 2000000

# Set theme
ui theme --set dark

# Position window
ui position --set 200,150,800,600

# Initialize UI components
ui initialize --theme auto

# Get UI status
ui status --format json
```

## Architecture

### Core Components
- **NetworkMonitorService**: Real-time network traffic monitoring
- **TaskbarIntegrationService**: Windows 11 system tray integration
- **UIComponentsService**: Modern WPF GUI with detailed statistics
- **PerformanceOptimizationService**: Resource usage optimization
- **WindowsThemeDetectionService**: Automatic theme detection and switching

### Data Models
- **NetworkTrafficData**: Network speed measurements and adapter information
- **DisplayConfiguration**: User preferences and display settings
- **NetworkAdapter**: Network interface information and status
- **SpeedReading**: Auto-scaling speed display with unit conversion

### Architecture Principles
- **Library-First Design**: Each component is a standalone library with CLI interface
- **Test-Driven Development**: Comprehensive test coverage with TDD methodology
- **Dependency Injection**: Proper service registration and lifetime management
- **Performance First**: All operations designed to meet strict performance targets

## Performance Specifications

| Metric | Target | Monitoring |
|--------|---------|-----------|
| CPU Usage | <1% | Continuous monitoring with automatic optimization |
| Memory Usage | <50MB | Real-time tracking with garbage collection optimization |
| Response Time | <100ms | Performance counters for all operations |
| UI Updates | <50ms | Optimized for smooth real-time display |
| Theme Changes | <200ms | Efficient theme switching with caching |

## Testing

### Run All Tests
```bash
# Run all tests
dotnet test NetworkUsage.Tests.csproj

# Run specific test categories
dotnet test --filter "TestCategory=Contract"
dotnet test --filter "TestCategory=Integration"
dotnet test --filter "TestCategory=Performance"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
- **Contract Tests**: Verify interface contracts are properly implemented
- **Integration Tests**: Test cross-component functionality and real-world scenarios
- **Unit Tests**: Test individual components in isolation
- **Performance Tests**: Validate response times and resource usage

## Development

### Building the Project
```bash
# Restore dependencies
dotnet restore NetworkUsage.sln

# Build debug version
dotnet build NetworkUsage.sln --configuration Debug

# Build release version
dotnet build NetworkUsage.sln --configuration Release

# Run with development settings
dotnet run --project NetworkUsage.csproj --configuration Debug
```

### Development Environment Setup
1. Install .NET 8.0 SDK
2. Install Visual Studio 2022 or VS Code with C# extension
3. Clone the repository
4. Open `NetworkUsage.sln` in your IDE
5. Build and run

### Code Style and Quality
- **EditorConfig**: Automated code formatting
- **Code Analysis**: Enabled with Microsoft analyzers
- **Nullable Reference Types**: Enabled for better null safety
- **Warnings as Errors**: Strict code quality enforcement

## Troubleshooting

### Common Issues

#### Application Won't Start
```
Error: No network adapters available
Solution: Ensure you have active network connections (Ethernet or WiFi)
```

#### High CPU Usage
```
Issue: CPU usage above 1%
Solution: Increase update interval in Settings → Monitoring Settings
```

#### Theme Not Switching
```
Issue: Theme doesn't follow Windows theme
Solution: Set theme to "Auto" in Settings → Display Settings
```

#### System Tray Icon Missing
```
Issue: Icon doesn't appear in system tray
Solution: Check Settings → Display Settings → "Show icon in system tray"
```

### Debug Information
```bash
# Enable debug logging
NetworkUsage.exe --log-level Debug

# Check performance metrics
NetworkUsage.exe network-monitor status --format json

# Verify theme detection
NetworkUsage.exe ui status --format json
```

### Log Files
- **Location**: `%TEMP%\NetworkUsage\logs\`
- **Rotation**: Daily rotation with 7-day retention
- **Levels**: Debug, Information, Warning, Error, Critical

## Contributing

### Development Guidelines
1. Follow Test-Driven Development (TDD) methodology
2. Maintain >90% test coverage
3. Ensure all performance targets are met
4. Follow Windows 11 design guidelines
5. Use proper dependency injection patterns

### Pull Request Process
1. Create feature branch from `develop`
2. Write tests first (TDD)
3. Implement feature to make tests pass
4. Ensure all tests pass
5. Submit pull request to `develop` branch

### Code Review Checklist
- [ ] Tests written first and pass
- [ ] Performance requirements met
- [ ] Windows 11 design compliance
- [ ] Error handling implemented
- [ ] Documentation updated

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **GitHub Issues**: [Report bugs or request features](../../issues)
- **Documentation**: [Wiki pages](../../wiki)
- **Performance Reports**: Available via CLI `status` commands

## Version History

### v1.0.0 (Current)
- ✅ Real-time network monitoring
- ✅ Windows 11 taskbar integration
- ✅ Automatic theme adaptation
- ✅ Modern GUI with detailed statistics
- ✅ Command-line interface
- ✅ Performance optimization
- ✅ Multi-adapter support

## Acknowledgments

- Built using Test-Driven Development methodology
- Windows 11 Fluent Design compliance
- .NET 8 and WPF technologies
- Microsoft.Extensions libraries for dependency injection and logging
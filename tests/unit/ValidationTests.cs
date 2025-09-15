using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Net.NetworkInformation;

namespace NetworkUsage.Tests.Unit
{
    /// <summary>
    /// Unit tests for validation logic across all data models
    /// Tests individual component validation in isolation
    /// Verifies all validation rules from data-model.md specification
    /// </summary>
    [TestClass]
    public class ValidationTests : UnitTestBase
    {
        #region NetworkTrafficData Validation Tests

        [TestMethod]
        public void NetworkTrafficData_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var trafficData = new NetworkTrafficData(
                bytesReceived: 1000000,
                bytesSent: 500000,
                receiveSpeed: 1500.0,
                sendSpeed: 750.0,
                adapterName: "Test Adapter",
                adapterType: NetworkInterfaceType.Ethernet
            );

            // Act
            var isValid = trafficData.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Valid traffic data should pass validation");
        }

        [TestMethod]
        public void NetworkTrafficData_WithNegativeBytesReceived_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.BytesReceived = -1000; // Invalid: negative value
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithNegativeBytesSent_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.BytesSent = -500; // Invalid: negative value
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithNegativeReceiveSpeed_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.ReceiveSpeed = -1500.0; // Invalid: negative speed
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithNegativeSendSpeed_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.SendSpeed = -750.0; // Invalid: negative speed
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithEmptyAdapterName_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.AdapterName = string.Empty; // Invalid: empty adapter name
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithNullAdapterName_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var trafficData = new NetworkTrafficData();
                trafficData.AdapterName = null!; // Invalid: null adapter name
            });
        }

        [TestMethod]
        public void NetworkTrafficData_WithFutureTimestamp_ShouldFailValidation()
        {
            // Arrange
            var trafficData = new NetworkTrafficData
            {
                AdapterName = "Test Adapter",
                Timestamp = DateTime.Now.AddMinutes(5) // Invalid: future timestamp
            };

            // Act
            var isValid = trafficData.IsValid();

            // Assert
            Assert.IsFalse(isValid, "Future timestamps should fail validation");
        }

        [TestMethod]
        public void NetworkTrafficData_CreateFromDelta_ShouldCalculateSpeedsCorrectly()
        {
            // Arrange
            var previousData = new NetworkTrafficData(
                bytesReceived: 1000000,
                bytesSent: 500000,
                receiveSpeed: 0,
                sendSpeed: 0,
                adapterName: "Test Adapter"
            )
            {
                Timestamp = DateTime.Now.AddSeconds(-2)
            };

            var currentData = new NetworkTrafficData(
                bytesReceived: 1003000, // +3000 bytes in 2 seconds
                bytesSent: 501500,      // +1500 bytes in 2 seconds
                receiveSpeed: 0,
                sendSpeed: 0,
                adapterName: "Test Adapter"
            )
            {
                Timestamp = DateTime.Now
            };

            // Act
            var deltaData = NetworkTrafficData.CreateFromDelta(previousData, currentData);

            // Assert
            Assert.AreEqual(1500.0, deltaData.ReceiveSpeed, 0.1, "Receive speed should be 1500 B/s (3000 bytes / 2 seconds)");
            Assert.AreEqual(750.0, deltaData.SendSpeed, 0.1, "Send speed should be 750 B/s (1500 bytes / 2 seconds)");
            Assert.AreEqual(currentData.BytesReceived, deltaData.BytesReceived, "Total bytes received should match current");
            Assert.AreEqual(currentData.BytesSent, deltaData.BytesSent, "Total bytes sent should match current");
        }

        #endregion

        #region DisplayConfiguration Validation Tests

        [TestMethod]
        public void DisplayConfiguration_WithValidSettings_ShouldPassValidation()
        {
            // Arrange
            var config = new DisplayConfiguration(
                updateInterval: TimeSpan.FromSeconds(1),
                autoScaleUnits: true,
                currentTheme: WindowsTheme.Light,
                showInSystemTray: true,
                toolTipFormat: "↓{0} ↑{1}",
                responseTimeoutMs: 100
            );

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Valid configuration should pass validation");
        }

        [TestMethod]
        public void DisplayConfiguration_WithTooShortUpdateInterval_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var config = new DisplayConfiguration();
                config.UpdateInterval = TimeSpan.FromMilliseconds(400); // Invalid: < 500ms
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithTooLongUpdateInterval_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var config = new DisplayConfiguration();
                config.UpdateInterval = TimeSpan.FromSeconds(15); // Invalid: > 10 seconds
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithTooShortResponseTimeout_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var config = new DisplayConfiguration();
                config.ResponseTimeoutMs = 25; // Invalid: < 50ms
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithTooLongResponseTimeout_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var config = new DisplayConfiguration();
                config.ResponseTimeoutMs = 1500; // Invalid: > 1000ms
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithInvalidToolTipFormat_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var config = new DisplayConfiguration();
                config.ToolTipFormat = "Only one placeholder {0}"; // Invalid: missing {1}
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithTooManyPlaceholders_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var config = new DisplayConfiguration();
                config.ToolTipFormat = "{0} {1} {2}"; // Invalid: too many placeholders
            });
        }

        [TestMethod]
        public void DisplayConfiguration_WithInvalidFormatString_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var config = new DisplayConfiguration();
                config.ToolTipFormat = "Invalid format {0:zzz} {1}"; // Invalid: bad format specifier
            });
        }

        [TestMethod]
        public void DisplayConfiguration_DefaultValues_ShouldMatchSpecification()
        {
            // Arrange
            var config = new DisplayConfiguration();

            // Act & Assert - Verify all default values from data-model.md
            Assert.AreEqual(TimeSpan.FromSeconds(1), config.UpdateInterval, "Default update interval should be 1 second");
            Assert.AreEqual(true, config.AutoScaleUnits, "Default auto-scale should be true");
            Assert.AreEqual(WindowsTheme.Auto, config.CurrentTheme, "Default theme should be Auto");
            Assert.AreEqual(true, config.ShowInSystemTray, "Default system tray should be true");
            Assert.AreEqual("↓{0} ↑{1}", config.ToolTipFormat, "Default tooltip format should be arrow format");
            Assert.AreEqual(100, config.ResponseTimeoutMs, "Default response timeout should be 100ms");
        }

        #endregion

        #region NetworkAdapter Validation Tests

        [TestMethod]
        public void NetworkAdapter_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var adapter = new NetworkAdapter(
                id: "test-adapter-1",
                name: "Test Ethernet Adapter",
                description: "Intel(R) Ethernet Controller",
                type: NetworkInterfaceType.Ethernet,
                status: OperationalStatus.Up,
                speed: 1000000000, // 1 Gbps
                isActive: true,
                ipv4Address: "192.168.1.100",
                macAddress: "00:11:22:33:44:55"
            );

            // Act
            var isValid = adapter.IsValid();

            // Assert
            Assert.IsTrue(isValid, "Valid adapter should pass validation");
        }

        [TestMethod]
        public void NetworkAdapter_WithEmptyId_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var adapter = new NetworkAdapter();
                adapter.Id = string.Empty; // Invalid: empty ID
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithEmptyName_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var adapter = new NetworkAdapter();
                adapter.Name = string.Empty; // Invalid: empty name
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithEmptyDescription_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var adapter = new NetworkAdapter();
                adapter.Description = string.Empty; // Invalid: empty description
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithZeroSpeedWhenUp_ShouldThrowException()
        {
            // Arrange
            var adapter = new NetworkAdapter
            {
                Id = "test-adapter",
                Name = "Test Adapter",
                Description = "Test Description",
                Status = OperationalStatus.Up
            };

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                adapter.Speed = 0; // Invalid: zero speed when status is Up
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithInvalidIPv4Address_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var adapter = new NetworkAdapter();
                adapter.IPv4Address = "999.999.999.999"; // Invalid: IP address out of range
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithInvalidMacAddress_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var adapter = new NetworkAdapter();
                adapter.MacAddress = "invalid-mac-address"; // Invalid: bad MAC format
            });
        }

        [TestMethod]
        public void NetworkAdapter_WithValidIPv4Address_ShouldAcceptAddress()
        {
            // Arrange
            var adapter = new NetworkAdapter();
            var validIPs = new[] { "192.168.1.1", "10.0.0.1", "172.16.0.1", "127.0.0.1" };

            // Act & Assert
            foreach (var ip in validIPs)
            {
                adapter.IPv4Address = ip; // Should not throw
                Assert.AreEqual(ip, adapter.IPv4Address, $"Should accept valid IP: {ip}");
            }
        }

        [TestMethod]
        public void NetworkAdapter_WithValidMacAddress_ShouldAcceptAddress()
        {
            // Arrange
            var adapter = new NetworkAdapter();
            var validMACs = new[] { "00:11:22:33:44:55", "AA:BB:CC:DD:EE:FF", "00-11-22-33-44-55" };

            // Act & Assert
            foreach (var mac in validMACs)
            {
                adapter.MacAddress = mac; // Should not throw
                Assert.IsTrue(adapter.MacAddress.Contains(':') || adapter.MacAddress.Contains('-'), 
                    $"Should accept valid MAC: {mac}");
            }
        }

        [TestMethod]
        public void NetworkAdapter_IdentityBasedEquality_ShouldWorkCorrectly()
        {
            // Arrange
            var adapter1 = new NetworkAdapter("adapter-1", "Adapter One", "Description", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 1000000000);
            var adapter2 = new NetworkAdapter("adapter-1", "Different Name", "Different Description", NetworkInterfaceType.Wireless80211, OperationalStatus.Down, 500000000);
            var adapter3 = new NetworkAdapter("adapter-2", "Adapter One", "Description", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 1000000000);

            // Act & Assert - Identity-based equality (same ID = equal)
            VerifyEntitySemantics(adapter1, adapter2, adapter3, a => a.Id);
        }

        [TestMethod]
        public void NetworkAdapter_PriorityCalculation_ShouldPrioritizeCorrectly()
        {
            // Arrange
            var ethernetAdapter = new NetworkAdapter("eth1", "Ethernet", "Ethernet Adapter", NetworkInterfaceType.Ethernet, OperationalStatus.Up, 1000000000);
            var wifiAdapter = new NetworkAdapter("wifi1", "WiFi", "WiFi Adapter", NetworkInterfaceType.Wireless80211, OperationalStatus.Up, 600000000);
            var downAdapter = new NetworkAdapter("down1", "Down", "Down Adapter", NetworkInterfaceType.Ethernet, OperationalStatus.Down, 1000000000);

            // Act
            var ethernetPriority = ethernetAdapter.GetPriority();
            var wifiPriority = wifiAdapter.GetPriority();
            var downPriority = downAdapter.GetPriority();

            // Assert
            Assert.IsTrue(ethernetPriority > wifiPriority, "Ethernet should have higher priority than WiFi");
            Assert.IsTrue(wifiPriority > downPriority, "WiFi (up) should have higher priority than down adapter");
            Assert.AreEqual(0, downPriority, "Down adapters should have zero priority");
        }

        #endregion

        #region SpeedReading Validation Tests

        [TestMethod]
        public void SpeedReading_WithNegativeSpeed_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                SpeedReading.FromBytesPerSecond(-1000); // Invalid: negative speed
            });
        }

        [TestMethod]
        public void SpeedReading_WithZeroSpeed_ShouldCreateValidReading()
        {
            // Arrange & Act
            var reading = SpeedReading.FromBytesPerSecond(0);

            // Assert
            Assert.AreEqual(0, reading.BytesPerSecond, "Zero speed should be valid");
            Assert.AreEqual(SpeedUnit.Bytes, reading.DisplayUnit, "Zero speed should use Bytes unit");
            Assert.AreEqual("0 B/s", reading.FormattedString, "Zero speed should format correctly");
        }

        [TestMethod]
        public void SpeedReading_AutoScaling_ShouldScaleCorrectly()
        {
            // Arrange & Act & Assert
            var bytesReading = SpeedReading.FromBytesPerSecond(500);
            Assert.AreEqual(SpeedUnit.Bytes, bytesReading.DisplayUnit, "500 B/s should use Bytes unit");

            var kilobytesReading = SpeedReading.FromBytesPerSecond(1536); // 1.5 KB/s
            Assert.AreEqual(SpeedUnit.Kilobytes, kilobytesReading.DisplayUnit, "1536 B/s should use Kilobytes unit");

            var megabytesReading = SpeedReading.FromBytesPerSecond(1572864); // 1.5 MB/s
            Assert.AreEqual(SpeedUnit.Megabytes, megabytesReading.DisplayUnit, "1572864 B/s should use Megabytes unit");

            var gigabytesReading = SpeedReading.FromBytesPerSecond(1610612736); // 1.5 GB/s
            Assert.AreEqual(SpeedUnit.Gigabytes, gigabytesReading.DisplayUnit, "1610612736 B/s should use Gigabytes unit");
        }

        [TestMethod]
        public void SpeedReading_ValueSemantics_ShouldWorkCorrectly()
        {
            // Arrange
            var reading1 = SpeedReading.FromBytesPerSecond(1500000); // 1.5 MB/s
            var reading2 = SpeedReading.FromBytesPerSecond(1500000); // Same value
            var reading3 = SpeedReading.FromBytesPerSecond(2000000); // Different value

            // Act & Assert - Value-based equality
            VerifyValueObjectSemantics(reading1, reading2, reading3);
        }

        [TestMethod]
        public void SpeedReading_ArithmeticOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var speed1 = SpeedReading.FromBytesPerSecond(1000000); // 1 MB/s
            var speed2 = SpeedReading.FromBytesPerSecond(500000);  // 0.5 MB/s

            // Act
            var sum = speed1 + speed2;
            var difference = speed1 - speed2;
            var doubled = speed1 * 2;
            var halved = speed1 / 2;

            // Assert
            Assert.AreEqual(1500000, sum.BytesPerSecond, 0.1, "Addition should work correctly");
            Assert.AreEqual(500000, difference.BytesPerSecond, 0.1, "Subtraction should work correctly");
            Assert.AreEqual(2000000, doubled.BytesPerSecond, 0.1, "Multiplication should work correctly");
            Assert.AreEqual(500000, halved.BytesPerSecond, 0.1, "Division should work correctly");
        }

        [TestMethod]
        public void SpeedReading_DivisionByZero_ShouldThrowException()
        {
            // Arrange
            var speed = SpeedReading.FromBytesPerSecond(1000000);

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var result = speed / 0; // Invalid: division by zero
            });
        }

        [TestMethod]
        public void SpeedReading_MultiplicationByNegative_ShouldThrowException()
        {
            // Arrange
            var speed = SpeedReading.FromBytesPerSecond(1000000);

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                var result = speed * -1; // Invalid: negative multiplier
            });
        }

        #endregion

        #region NetworkAdapter State Transition Tests

        [TestMethod]
        public void NetworkAdapter_StateTransitions_ShouldFollowSpecifiedFlow()
        {
            // Arrange
            var adapter = new NetworkAdapter
            {
                Id = "test-adapter",
                Name = "Test Adapter",
                Description = "Test Description",
                Status = OperationalStatus.Unknown // Initial detected state
            };

            // Act & Assert - Detected → Available
            adapter.MarkAsAvailable();
            Assert.AreEqual(OperationalStatus.Down, adapter.Status, "Available state should set status to Down");

            // Available → Active
            adapter.MarkAsActive();
            Assert.IsTrue(adapter.IsActive, "Should be marked as active");
            Assert.AreEqual(OperationalStatus.Up, adapter.Status, "Active state should set status to Up");

            // Active → Unavailable
            adapter.MarkAsUnavailable();
            Assert.IsFalse(adapter.IsActive, "Should no longer be active");
            Assert.AreEqual(OperationalStatus.Down, adapter.Status, "Unavailable state should set status to Down");

            // Unavailable → Available (restored)
            adapter.RestoreConnection();
            Assert.AreEqual(OperationalStatus.Up, adapter.Status, "Restored connection should set status to Up");
        }

        #endregion

        #region NetworkTrafficData State Transition Tests

        [TestMethod]
        public void NetworkTrafficData_InitialState_ShouldHaveZeroValues()
        {
            // Arrange & Act
            var trafficData = new NetworkTrafficData();

            // Assert - Initial state: All numeric values = 0, Timestamp = DateTime.Now
            Assert.AreEqual(0L, trafficData.BytesReceived, "Initial bytes received should be 0");
            Assert.AreEqual(0L, trafficData.BytesSent, "Initial bytes sent should be 0");
            Assert.AreEqual(0.0, trafficData.ReceiveSpeed, "Initial receive speed should be 0");
            Assert.AreEqual(0.0, trafficData.SendSpeed, "Initial send speed should be 0");
            Assert.IsTrue(Math.Abs((trafficData.Timestamp - DateTime.Now).TotalSeconds) < 2, 
                "Initial timestamp should be close to current time");
        }

        [TestMethod]
        public void NetworkTrafficData_ResetState_ShouldReturnToInitialState()
        {
            // Arrange
            var trafficData = new NetworkTrafficData(
                bytesReceived: 1000000,
                bytesSent: 500000,
                receiveSpeed: 1500.0,
                sendSpeed: 750.0,
                adapterName: "Old Adapter"
            );

            // Act - Reset to initial state for new adapter
            trafficData.ResetToInitialState("New Adapter", NetworkInterfaceType.Wireless80211);

            // Assert
            Assert.AreEqual(0L, trafficData.BytesReceived, "Reset should zero bytes received");
            Assert.AreEqual(0L, trafficData.BytesSent, "Reset should zero bytes sent");
            Assert.AreEqual(0.0, trafficData.ReceiveSpeed, "Reset should zero receive speed");
            Assert.AreEqual(0.0, trafficData.SendSpeed, "Reset should zero send speed");
            Assert.AreEqual("New Adapter", trafficData.AdapterName, "Reset should update adapter name");
            Assert.AreEqual(NetworkInterfaceType.Wireless80211, trafficData.AdapterType, "Reset should update adapter type");
        }

        #endregion

        #region Enum Extension Method Tests

        [TestMethod]
        public void EnumExtensions_WindowsThemeParsing_ShouldHandleAllVariants()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(WindowsTheme.Light, EnumExtensions.ParseWindowsTheme("light"));
            Assert.AreEqual(WindowsTheme.Light, EnumExtensions.ParseWindowsTheme("LIGHT"));
            Assert.AreEqual(WindowsTheme.Dark, EnumExtensions.ParseWindowsTheme("dark"));
            Assert.AreEqual(WindowsTheme.Dark, EnumExtensions.ParseWindowsTheme("Dark"));
            Assert.AreEqual(WindowsTheme.HighContrast, EnumExtensions.ParseWindowsTheme("highcontrast"));
            Assert.AreEqual(WindowsTheme.HighContrast, EnumExtensions.ParseWindowsTheme("high-contrast"));
            Assert.AreEqual(WindowsTheme.Auto, EnumExtensions.ParseWindowsTheme("auto"));
            Assert.AreEqual(WindowsTheme.Auto, EnumExtensions.ParseWindowsTheme("invalid")); // Fallback
            Assert.AreEqual(WindowsTheme.Auto, EnumExtensions.ParseWindowsTheme("")); // Empty fallback
        }

        [TestMethod]
        public void EnumExtensions_SpeedUnitParsing_ShouldHandleAllVariants()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(SpeedUnit.Bytes, EnumExtensions.ParseSpeedUnit("b"));
            Assert.AreEqual(SpeedUnit.Bytes, EnumExtensions.ParseSpeedUnit("bytes"));
            Assert.AreEqual(SpeedUnit.Bytes, EnumExtensions.ParseSpeedUnit("B/s"));
            Assert.AreEqual(SpeedUnit.Kilobytes, EnumExtensions.ParseSpeedUnit("kb"));
            Assert.AreEqual(SpeedUnit.Kilobytes, EnumExtensions.ParseSpeedUnit("KB/s"));
            Assert.AreEqual(SpeedUnit.Megabytes, EnumExtensions.ParseSpeedUnit("mb"));
            Assert.AreEqual(SpeedUnit.Megabytes, EnumExtensions.ParseSpeedUnit("MB/s"));
            Assert.AreEqual(SpeedUnit.Gigabytes, EnumExtensions.ParseSpeedUnit("gb"));
            Assert.AreEqual(SpeedUnit.Gigabytes, EnumExtensions.ParseSpeedUnit("GB/s"));
            Assert.AreEqual(SpeedUnit.Bytes, EnumExtensions.ParseSpeedUnit("invalid")); // Fallback
        }

        [TestMethod]
        public void EnumExtensions_IsDarkTheme_ShouldIdentifyDarkThemes()
        {
            // Arrange & Act & Assert
            Assert.IsFalse(WindowsTheme.Light.IsDarkTheme(), "Light should not be dark theme");
            Assert.IsTrue(WindowsTheme.Dark.IsDarkTheme(), "Dark should be dark theme");
            Assert.IsTrue(WindowsTheme.HighContrast.IsDarkTheme(), "High contrast should be dark theme");
            Assert.IsFalse(WindowsTheme.Auto.IsDarkTheme(), "Auto should not be dark theme");
        }

        [TestMethod]
        public void EnumExtensions_IsAppropriateUnit_ShouldSelectCorrectUnits()
        {
            // Arrange & Act & Assert
            Assert.IsTrue(SpeedUnit.Bytes.IsAppropriateUnit(500), "Bytes unit should be appropriate for 500 B/s");
            Assert.IsTrue(SpeedUnit.Kilobytes.IsAppropriateUnit(1536), "KB unit should be appropriate for 1536 B/s (1.5 KB/s)");
            Assert.IsTrue(SpeedUnit.Megabytes.IsAppropriateUnit(1572864), "MB unit should be appropriate for 1572864 B/s (1.5 MB/s)");
            Assert.IsTrue(SpeedUnit.Gigabytes.IsAppropriateUnit(1610612736), "GB unit should be appropriate for 1610612736 B/s (1.5 GB/s)");
            
            Assert.IsFalse(SpeedUnit.Megabytes.IsAppropriateUnit(500), "MB unit should not be appropriate for 500 B/s");
            Assert.IsFalse(SpeedUnit.Bytes.IsAppropriateUnit(1572864), "Bytes unit should not be appropriate for MB-scale speeds");
        }

        #endregion

        #region Cross-Field Validation Tests

        [TestMethod]
        public void NetworkTrafficData_TimestampConsistency_ShouldBeValidated()
        {
            // Arrange
            var previousData = new NetworkTrafficData { Timestamp = DateTime.Now.AddSeconds(-2) };
            var currentData = new NetworkTrafficData { Timestamp = DateTime.Now.AddSeconds(-3) }; // Invalid: earlier than previous

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                NetworkTrafficData.CreateFromDelta(previousData, currentData);
            });
        }

        [TestMethod]
        public void DisplayConfiguration_ThemeAdjustment_ShouldModifySettingsCorrectly()
        {
            // Arrange
            var config = new DisplayConfiguration
            {
                CurrentTheme = WindowsTheme.Light,
                ResponseTimeoutMs = 100
            };

            // Act
            var adjusted = config.GetThemeAdjustedConfiguration(WindowsTheme.HighContrast);

            // Assert
            Assert.AreEqual(WindowsTheme.HighContrast, adjusted.CurrentTheme, "Theme should be updated");
            Assert.IsTrue(adjusted.ResponseTimeoutMs >= 150, "High contrast should increase timeout for accessibility");
        }

        [TestMethod]
        public void SpeedReading_Comparison_ShouldOrderCorrectly()
        {
            // Arrange
            var slow = SpeedReading.FromBytesPerSecond(1000);    // 1 KB/s
            var medium = SpeedReading.FromBytesPerSecond(1500000); // 1.5 MB/s
            var fast = SpeedReading.FromBytesPerSecond(2000000000); // 2 GB/s

            // Act & Assert
            Assert.IsTrue(slow < medium, "Slow should be less than medium");
            Assert.IsTrue(medium < fast, "Medium should be less than fast");
            Assert.IsTrue(slow < fast, "Slow should be less than fast");
            
            Assert.IsTrue(fast > medium, "Fast should be greater than medium");
            Assert.IsTrue(medium > slow, "Medium should be greater than slow");
            
            Assert.IsTrue(slow <= medium, "Less than or equal should work");
            Assert.IsTrue(fast >= medium, "Greater than or equal should work");
        }

        #endregion

        #region Edge Case Tests

        [TestMethod]
        public void NetworkTrafficData_Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var original = new NetworkTrafficData(1000, 500, 1500.0, 750.0, "Test Adapter");

            // Act
            var clone = original.Clone();
            clone.BytesReceived = 2000; // Modify clone

            // Assert
            Assert.AreEqual(1000, original.BytesReceived, "Original should not be affected by clone modification");
            Assert.AreEqual(2000, clone.BytesReceived, "Clone should have modified value");
            Assert.AreNotSame(original, clone, "Clone should be different object");
        }

        [TestMethod]
        public void DisplayConfiguration_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var config = new DisplayConfiguration();
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Act - Test concurrent access
            for (int i = 0; i < 10; i++)
            {
                int taskIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            config.UpdateInterval = TimeSpan.FromMilliseconds(500 + (taskIndex * 100));
                            config.AutoScaleUnits = j % 2 == 0;
                            config.CurrentTheme = (WindowsTheme)(j % 4);
                            
                            var interval = config.UpdateInterval;
                            var autoScale = config.AutoScaleUnits;
                            var theme = config.CurrentTheme;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            // Assert
            Assert.AreEqual(0, exceptions.Count, $"Concurrent access should not cause exceptions. Found: {string.Join(", ", exceptions.Select(e => e.Message))}");
            Assert.IsTrue(config.IsValid(), "Configuration should remain valid after concurrent access");
        }

        #endregion
    }
}

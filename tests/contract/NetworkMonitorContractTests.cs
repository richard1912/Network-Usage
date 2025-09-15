using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Contract
{
    /// <summary>
    /// Contract tests for INetworkMonitor interface
    /// These tests verify the interface contract is properly defined
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class NetworkMonitorContractTests : ContractTestBase
    {
        private readonly Type _interfaceType = typeof(INetworkMonitor);

        [TestMethod]
        public void INetworkMonitor_ShouldBeAnInterface()
        {
            // Act & Assert
            Assert.IsTrue(_interfaceType.IsInterface, "INetworkMonitor should be an interface");
        }

        [TestMethod]
        public void INetworkMonitor_ShouldHaveRequiredEvents()
        {
            // Arrange
            var expectedEvents = new[]
            {
                "TrafficDataUpdated",
                "ActiveAdapterChanged"
            };

            // Act & Assert
            VerifyInterfaceEvents(_interfaceType, expectedEvents);
        }

        [TestMethod]
        public void INetworkMonitor_ShouldHaveRequiredMethods()
        {
            // Arrange
            var expectedMethods = new[]
            {
                "StartMonitoringAsync",
                "StopMonitoringAsync", 
                "GetCurrentTrafficAsync",
                "GetAvailableAdaptersAsync",
                "SetActiveAdapterAsync",
                "GetActiveAdapter",
                "SetUpdateIntervalAsync"
            };

            // Act & Assert
            VerifyInterfaceContract(_interfaceType, expectedMethods);
        }

        [TestMethod]
        public void INetworkMonitor_ShouldHaveRequiredProperties()
        {
            // Arrange
            var expectedProperties = new[]
            {
                "IsMonitoring"
            };

            // Act & Assert
            VerifyInterfaceProperties(_interfaceType, expectedProperties);
        }

        [TestMethod]
        public void StartMonitoringAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "StartMonitoringAsync", typeof(Task));
        }

        [TestMethod]
        public void StopMonitoringAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "StopMonitoringAsync", typeof(Task));
        }

        [TestMethod]
        public void GetCurrentTrafficAsync_ShouldReturnTaskOfNetworkTrafficData()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "GetCurrentTrafficAsync", typeof(Task<NetworkTrafficData>));
        }

        [TestMethod]
        public void GetAvailableAdaptersAsync_ShouldReturnTaskOfNetworkAdapterCollection()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "GetAvailableAdaptersAsync", typeof(Task<IEnumerable<NetworkAdapter>>));
        }

        [TestMethod]
        public void SetActiveAdapterAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(string) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "SetActiveAdapterAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void SetUpdateIntervalAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(TimeSpan) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "SetUpdateIntervalAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void GetActiveAdapter_ShouldReturnNetworkAdapter()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "GetActiveAdapter", typeof(NetworkAdapter));
        }

        [TestMethod]
        public void IsMonitoring_ShouldBeBooleanProperty()
        {
            // Arrange
            var property = _interfaceType.GetProperty("IsMonitoring");

            // Act & Assert
            Assert.IsNotNull(property, "IsMonitoring property should exist");
            Assert.AreEqual(typeof(bool), property.PropertyType, "IsMonitoring should be boolean");
            Assert.IsTrue(property.CanRead, "IsMonitoring should be readable");
        }

        [TestMethod]
        public void TrafficDataUpdated_ShouldHaveCorrectEventHandlerType()
        {
            // Arrange
            var eventInfo = _interfaceType.GetEvent("TrafficDataUpdated");

            // Act & Assert
            Assert.IsNotNull(eventInfo, "TrafficDataUpdated event should exist");
            Assert.AreEqual(typeof(EventHandler<NetworkTrafficData>), eventInfo.EventHandlerType,
                "TrafficDataUpdated should use EventHandler<NetworkTrafficData>");
        }

        [TestMethod]
        public void ActiveAdapterChanged_ShouldHaveCorrectEventHandlerType()
        {
            // Arrange
            var eventInfo = _interfaceType.GetEvent("ActiveAdapterChanged");

            // Act & Assert
            Assert.IsNotNull(eventInfo, "ActiveAdapterChanged event should exist");
            Assert.AreEqual(typeof(EventHandler<NetworkAdapter>), eventInfo.EventHandlerType,
                "ActiveAdapterChanged should use EventHandler<NetworkAdapter>");
        }

        [TestMethod]
        public void NetworkTrafficEventArgs_ShouldExistAndInheritFromEventArgs()
        {
            // Arrange
            var eventArgsType = typeof(NetworkTrafficEventArgs);

            // Act & Assert
            Assert.IsNotNull(eventArgsType, "NetworkTrafficEventArgs should exist");
            Assert.IsTrue(typeof(EventArgs).IsAssignableFrom(eventArgsType),
                "NetworkTrafficEventArgs should inherit from EventArgs");
        }

        /// <summary>
        /// This test verifies that no implementation exists yet - it should PASS initially
        /// Once we create implementations, we'll need to create a separate implementation test
        /// </summary>
        [TestMethod]
        public void INetworkMonitor_ShouldNotHaveImplementationsYet()
        {
            // Arrange
            var assembly = _interfaceType.Assembly;
            var implementationTypes = new List<Type>();

            // Act
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsInterface && !type.IsAbstract && _interfaceType.IsAssignableFrom(type))
                {
                    implementationTypes.Add(type);
                }
            }

            // Assert - This should PASS initially (no implementations)
            // When we start implementing, this test will fail and we can remove it
            Assert.AreEqual(0, implementationTypes.Count,
                $"Expected no implementations of INetworkMonitor yet, but found: {string.Join(", ", implementationTypes.Select(t => t.Name))}");
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Contract
{
    /// <summary>
    /// Contract tests for IUIComponents interface
    /// These tests verify the interface contract is properly defined
    /// CRITICAL: These tests MUST FAIL initially (no implementation exists yet)
    /// </summary>
    [TestClass]
    public class UIComponentsContractTests : ContractTestBase
    {
        private readonly Type _interfaceType = typeof(IUIComponents);

        [TestMethod]
        public void IUIComponents_ShouldBeAnInterface()
        {
            // Act & Assert
            Assert.IsTrue(_interfaceType.IsInterface, "IUIComponents should be an interface");
        }

        [TestMethod]
        public void IUIComponents_ShouldHaveRequiredEvents()
        {
            // Arrange
            var expectedEvents = new[]
            {
                "StatisticsWindowClosed",
                "UserInteraction"
            };

            // Act & Assert
            VerifyInterfaceEvents(_interfaceType, expectedEvents);
        }

        [TestMethod]
        public void IUIComponents_ShouldHaveRequiredMethods()
        {
            // Arrange
            var expectedMethods = new[]
            {
                "ShowDetailedStatsAsync",
                "HideDetailedStatsAsync",
                "UpdateStatisticsAsync",
                "ApplyThemeAsync",
                "UpdateAdapterListAsync",
                "ShowErrorAsync",
                "HandleInteractionAsync",
                "SetUIUpdateIntervalAsync",
                "PositionWindowAsync",
                "InitializeAsync",
                "ShutdownAsync"
            };

            // Act & Assert
            VerifyInterfaceContract(_interfaceType, expectedMethods);
        }

        [TestMethod]
        public void IUIComponents_ShouldHaveRequiredProperties()
        {
            // Arrange
            var expectedProperties = new[]
            {
                "IsStatisticsWindowVisible",
                "CurrentTheme"
            };

            // Act & Assert
            VerifyInterfaceProperties(_interfaceType, expectedProperties);
        }

        [TestMethod]
        public void ShowDetailedStatsAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "ShowDetailedStatsAsync", typeof(Task));
        }

        [TestMethod]
        public void HideDetailedStatsAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "HideDetailedStatsAsync", typeof(Task));
        }

        [TestMethod]
        public void UpdateStatisticsAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(NetworkTrafficData) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "UpdateStatisticsAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void UpdateStatisticsAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "UpdateStatisticsAsync", typeof(Task));
        }

        [TestMethod]
        public void ApplyThemeAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(WindowsTheme) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "ApplyThemeAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void ApplyThemeAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "ApplyThemeAsync", typeof(Task));
        }

        [TestMethod]
        public void UpdateAdapterListAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(IEnumerable<NetworkAdapter>) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "UpdateAdapterListAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void UpdateAdapterListAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "UpdateAdapterListAsync", typeof(Task));
        }

        [TestMethod]
        public void ShowErrorAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(string), typeof(Exception) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "ShowErrorAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void ShowErrorAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "ShowErrorAsync", typeof(Task));
        }

        [TestMethod]
        public void HandleInteractionAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(UIInteractionEventArgs) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "HandleInteractionAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void HandleInteractionAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "HandleInteractionAsync", typeof(Task));
        }

        [TestMethod]
        public void SetUIUpdateIntervalAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(TimeSpan) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "SetUIUpdateIntervalAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void SetUIUpdateIntervalAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "SetUIUpdateIntervalAsync", typeof(Task));
        }

        [TestMethod]
        public void PositionWindowAsync_ShouldHaveCorrectParameters()
        {
            // Arrange
            var expectedParameterTypes = new[] { typeof(Rectangle) };

            // Act & Assert
            VerifyMethodParameters(_interfaceType, "PositionWindowAsync", expectedParameterTypes);
        }

        [TestMethod]
        public void PositionWindowAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "PositionWindowAsync", typeof(Task));
        }

        [TestMethod]
        public void InitializeAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "InitializeAsync", typeof(Task));
        }

        [TestMethod]
        public void ShutdownAsync_ShouldReturnTask()
        {
            // Act & Assert
            VerifyMethodReturnType(_interfaceType, "ShutdownAsync", typeof(Task));
        }

        [TestMethod]
        public void IsStatisticsWindowVisible_ShouldBeBooleanProperty()
        {
            // Arrange
            var property = _interfaceType.GetProperty("IsStatisticsWindowVisible");

            // Act & Assert
            Assert.IsNotNull(property, "IsStatisticsWindowVisible property should exist");
            Assert.AreEqual(typeof(bool), property.PropertyType, "IsStatisticsWindowVisible should be boolean");
            Assert.IsTrue(property.CanRead, "IsStatisticsWindowVisible should be readable");
        }

        [TestMethod]
        public void CurrentTheme_ShouldBeWindowsThemeProperty()
        {
            // Arrange
            var property = _interfaceType.GetProperty("CurrentTheme");

            // Act & Assert
            Assert.IsNotNull(property, "CurrentTheme property should exist");
            Assert.AreEqual(typeof(WindowsTheme), property.PropertyType, "CurrentTheme should be WindowsTheme");
            Assert.IsTrue(property.CanRead, "CurrentTheme should be readable");
        }

        [TestMethod]
        public void StatisticsWindowClosed_ShouldHaveCorrectEventHandlerType()
        {
            // Arrange
            var eventInfo = _interfaceType.GetEvent("StatisticsWindowClosed");

            // Act & Assert
            Assert.IsNotNull(eventInfo, "StatisticsWindowClosed event should exist");
            Assert.AreEqual(typeof(EventHandler<StatisticsWindowEventArgs>), eventInfo.EventHandlerType,
                "StatisticsWindowClosed should use EventHandler<StatisticsWindowEventArgs>");
        }

        [TestMethod]
        public void UserInteraction_ShouldHaveCorrectEventHandlerType()
        {
            // Arrange
            var eventInfo = _interfaceType.GetEvent("UserInteraction");

            // Act & Assert
            Assert.IsNotNull(eventInfo, "UserInteraction event should exist");
            Assert.AreEqual(typeof(EventHandler<UIInteractionEventArgs>), eventInfo.EventHandlerType,
                "UserInteraction should use EventHandler<UIInteractionEventArgs>");
        }

        [TestMethod]
        public void StatisticsWindowEventArgs_ShouldExistAndInheritFromEventArgs()
        {
            // Arrange
            var eventArgsType = typeof(StatisticsWindowEventArgs);

            // Act & Assert
            Assert.IsNotNull(eventArgsType, "StatisticsWindowEventArgs should exist");
            Assert.IsTrue(typeof(EventArgs).IsAssignableFrom(eventArgsType),
                "StatisticsWindowEventArgs should inherit from EventArgs");
        }

        [TestMethod]
        public void UIInteractionEventArgs_ShouldExistAndInheritFromEventArgs()
        {
            // Arrange
            var eventArgsType = typeof(UIInteractionEventArgs);

            // Act & Assert
            Assert.IsNotNull(eventArgsType, "UIInteractionEventArgs should exist");
            Assert.IsTrue(typeof(EventArgs).IsAssignableFrom(eventArgsType),
                "UIInteractionEventArgs should inherit from EventArgs");
        }

        [TestMethod]
        public void UIConfiguration_ShouldExistAsClass()
        {
            // Arrange
            var configType = typeof(UIConfiguration);

            // Act & Assert
            Assert.IsNotNull(configType, "UIConfiguration should exist");
            Assert.IsTrue(configType.IsClass, "UIConfiguration should be a class");
        }

        [TestMethod]
        public void StatisticsWindowEventArgs_ShouldHaveRequiredProperties()
        {
            // Arrange
            var eventArgsType = typeof(StatisticsWindowEventArgs);
            var properties = eventArgsType.GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToArray();

            // Act & Assert
            Assert.IsTrue(propertyNames.Contains("WindowAction"), "StatisticsWindowEventArgs should have WindowAction property");
            Assert.IsTrue(propertyNames.Contains("Timestamp"), "StatisticsWindowEventArgs should have Timestamp property");
            Assert.IsTrue(propertyNames.Contains("WindowBounds"), "StatisticsWindowEventArgs should have WindowBounds property");

            // Verify property types
            var windowActionProperty = eventArgsType.GetProperty("WindowAction");
            Assert.AreEqual(typeof(string), windowActionProperty?.PropertyType, "WindowAction should be string type");

            var timestampProperty = eventArgsType.GetProperty("Timestamp");
            Assert.AreEqual(typeof(DateTime), timestampProperty?.PropertyType, "Timestamp should be DateTime type");

            var windowBoundsProperty = eventArgsType.GetProperty("WindowBounds");
            Assert.AreEqual(typeof(Rectangle), windowBoundsProperty?.PropertyType, "WindowBounds should be Rectangle type");
        }

        [TestMethod]
        public void UIInteractionEventArgs_ShouldHaveRequiredProperties()
        {
            // Arrange
            var eventArgsType = typeof(UIInteractionEventArgs);
            var properties = eventArgsType.GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToArray();

            // Act & Assert
            Assert.IsTrue(propertyNames.Contains("InteractionType"), "UIInteractionEventArgs should have InteractionType property");
            Assert.IsTrue(propertyNames.Contains("ElementName"), "UIInteractionEventArgs should have ElementName property");
            Assert.IsTrue(propertyNames.Contains("InteractionData"), "UIInteractionEventArgs should have InteractionData property");
            Assert.IsTrue(propertyNames.Contains("Timestamp"), "UIInteractionEventArgs should have Timestamp property");

            // Verify property types
            var interactionTypeProperty = eventArgsType.GetProperty("InteractionType");
            Assert.AreEqual(typeof(string), interactionTypeProperty?.PropertyType, "InteractionType should be string type");

            var elementNameProperty = eventArgsType.GetProperty("ElementName");
            Assert.AreEqual(typeof(string), elementNameProperty?.PropertyType, "ElementName should be string type");

            var interactionDataProperty = eventArgsType.GetProperty("InteractionData");
            Assert.AreEqual(typeof(object), interactionDataProperty?.PropertyType, "InteractionData should be object type");

            var timestampProperty = eventArgsType.GetProperty("Timestamp");
            Assert.AreEqual(typeof(DateTime), timestampProperty?.PropertyType, "Timestamp should be DateTime type");
        }

        [TestMethod]
        public void UIConfiguration_ShouldHaveRequiredProperties()
        {
            // Arrange
            var configType = typeof(UIConfiguration);
            var properties = configType.GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToArray();

            // Act & Assert
            Assert.IsTrue(propertyNames.Contains("UpdateInterval"), "UIConfiguration should have UpdateInterval property");
            Assert.IsTrue(propertyNames.Contains("PreferredTheme"), "UIConfiguration should have PreferredTheme property");
            Assert.IsTrue(propertyNames.Contains("ShowWindowOnStartup"), "UIConfiguration should have ShowWindowOnStartup property");
            Assert.IsTrue(propertyNames.Contains("PreferredWindowSize"), "UIConfiguration should have PreferredWindowSize property");
            Assert.IsTrue(propertyNames.Contains("PreferredWindowPosition"), "UIConfiguration should have PreferredWindowPosition property");
            Assert.IsTrue(propertyNames.Contains("DateTimeFormat"), "UIConfiguration should have DateTimeFormat property");
            Assert.IsTrue(propertyNames.Contains("EnableAnimations"), "UIConfiguration should have EnableAnimations property");

            // Verify key property types
            var updateIntervalProperty = configType.GetProperty("UpdateInterval");
            Assert.AreEqual(typeof(TimeSpan), updateIntervalProperty?.PropertyType, "UpdateInterval should be TimeSpan type");

            var preferredThemeProperty = configType.GetProperty("PreferredTheme");
            Assert.AreEqual(typeof(WindowsTheme), preferredThemeProperty?.PropertyType, "PreferredTheme should be WindowsTheme type");

            var showWindowOnStartupProperty = configType.GetProperty("ShowWindowOnStartup");
            Assert.AreEqual(typeof(bool), showWindowOnStartupProperty?.PropertyType, "ShowWindowOnStartup should be bool type");
        }

        /// <summary>
        /// This test verifies that no implementation exists yet - it should PASS initially
        /// Once we create implementations, we'll need to create a separate implementation test
        /// </summary>
        [TestMethod]
        public void IUIComponents_ShouldNotHaveImplementationsYet()
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
                $"Expected no implementations of IUIComponents yet, but found: {string.Join(", ", implementationTypes.Select(t => t.Name))}");
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkUsage.Contracts;
using System;
using System.Reflection;

namespace NetworkUsage.Tests.Contract
{
    /// <summary>
    /// Base class for contract tests that verify interface contracts are properly defined
    /// </summary>
    [TestClass]
    public abstract class ContractTestBase : TestBase
    {
        /// <summary>
        /// Verifies that an interface has the expected methods with correct signatures
        /// </summary>
        /// <param name="interfaceType">The interface type to verify</param>
        /// <param name="expectedMethods">Array of expected method names</param>
        protected void VerifyInterfaceContract(Type interfaceType, string[] expectedMethods)
        {
            Assert.IsTrue(interfaceType.IsInterface, $"{interfaceType.Name} should be an interface");
            
            var methods = interfaceType.GetMethods();
            var methodNames = methods.Select(m => m.Name).ToArray();
            
            foreach (var expectedMethod in expectedMethods)
            {
                Assert.IsTrue(methodNames.Contains(expectedMethod), 
                    $"Interface {interfaceType.Name} should contain method {expectedMethod}");
            }
        }

        /// <summary>
        /// Verifies that an interface has the expected properties
        /// </summary>
        /// <param name="interfaceType">The interface type to verify</param>
        /// <param name="expectedProperties">Array of expected property names</param>
        protected void VerifyInterfaceProperties(Type interfaceType, string[] expectedProperties)
        {
            var properties = interfaceType.GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToArray();
            
            foreach (var expectedProperty in expectedProperties)
            {
                Assert.IsTrue(propertyNames.Contains(expectedProperty), 
                    $"Interface {interfaceType.Name} should contain property {expectedProperty}");
            }
        }

        /// <summary>
        /// Verifies that an interface has the expected events
        /// </summary>
        /// <param name="interfaceType">The interface type to verify</param>
        /// <param name="expectedEvents">Array of expected event names</param>
        protected void VerifyInterfaceEvents(Type interfaceType, string[] expectedEvents)
        {
            var events = interfaceType.GetEvents();
            var eventNames = events.Select(e => e.Name).ToArray();
            
            foreach (var expectedEvent in expectedEvents)
            {
                Assert.IsTrue(eventNames.Contains(expectedEvent), 
                    $"Interface {interfaceType.Name} should contain event {expectedEvent}");
            }
        }

        /// <summary>
        /// Verifies that a method has the expected return type
        /// </summary>
        protected void VerifyMethodReturnType(Type interfaceType, string methodName, Type expectedReturnType)
        {
            var method = interfaceType.GetMethod(methodName);
            Assert.IsNotNull(method, $"Method {methodName} not found in interface {interfaceType.Name}");
            Assert.AreEqual(expectedReturnType, method.ReturnType, 
                $"Method {methodName} should return {expectedReturnType.Name}");
        }

        /// <summary>
        /// Verifies that a method has the expected parameter types
        /// </summary>
        protected void VerifyMethodParameters(Type interfaceType, string methodName, Type[] expectedParameterTypes)
        {
            var method = interfaceType.GetMethod(methodName);
            Assert.IsNotNull(method, $"Method {methodName} not found in interface {interfaceType.Name}");
            
            var parameters = method.GetParameters();
            Assert.AreEqual(expectedParameterTypes.Length, parameters.Length, 
                $"Method {methodName} should have {expectedParameterTypes.Length} parameters");
                
            for (int i = 0; i < expectedParameterTypes.Length; i++)
            {
                Assert.AreEqual(expectedParameterTypes[i], parameters[i].ParameterType, 
                    $"Parameter {i} of method {methodName} should be of type {expectedParameterTypes[i].Name}");
            }
        }
    }
}
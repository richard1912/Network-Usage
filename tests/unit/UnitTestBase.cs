using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;

namespace NetworkUsage.Tests.Unit
{
    /// <summary>
    /// Base class for unit tests that test individual components in isolation
    /// </summary>
    [TestClass]
    public abstract class UnitTestBase : TestBase
    {
        private readonly List<Mock> _mocks = new();

        [TestCleanup]
        public override void TestCleanup()
        {
            // Verify all mocks after each test
            foreach (var mock in _mocks)
            {
                mock.VerifyAll();
            }
            
            _mocks.Clear();
            base.TestCleanup();
        }

        /// <summary>
        /// Creates and registers a mock of the specified type
        /// </summary>
        /// <typeparam name="T">The type to mock</typeparam>
        /// <returns>The created mock</returns>
        protected Mock<T> CreateMock<T>() where T : class
        {
            var mock = new Mock<T>(MockBehavior.Strict);
            _mocks.Add(mock);
            return mock;
        }

        /// <summary>
        /// Creates a loose mock that doesn't require explicit setup for all calls
        /// </summary>
        /// <typeparam name="T">The type to mock</typeparam>
        /// <returns>The created mock</returns>
        protected Mock<T> CreateLooseMock<T>() where T : class
        {
            var mock = new Mock<T>(MockBehavior.Loose);
            _mocks.Add(mock);
            return mock;
        }

        /// <summary>
        /// Override to configure services for unit testing with mocks
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            // Unit tests should use mocks instead of real implementations
        }

        /// <summary>
        /// Verifies that a value object follows value semantics
        /// </summary>
        /// <typeparam name="T">The value object type</typeparam>
        /// <param name="value1">First instance</param>
        /// <param name="value2">Second instance with same values</param>
        /// <param name="value3">Third instance with different values</param>
        protected void VerifyValueObjectSemantics<T>(T value1, T value2, T value3) 
            where T : IEquatable<T>
        {
            // Test equality
            Assert.AreEqual(value1, value2, "Equal value objects should be equal");
            Assert.AreNotEqual(value1, value3, "Different value objects should not be equal");
            
            // Test GetHashCode consistency
            Assert.AreEqual(value1.GetHashCode(), value2.GetHashCode(), 
                "Equal value objects should have equal hash codes");
            
            // Test reflexive equality
            Assert.AreEqual(value1, value1, "Value object should equal itself");
            
            // Test null inequality
            Assert.AreNotEqual(value1, null, "Value object should not equal null");
        }

        /// <summary>
        /// Verifies that an entity follows entity semantics (identity-based equality)
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TId">The ID type</typeparam>
        /// <param name="entity1">First entity instance</param>
        /// <param name="entity2">Second entity instance with same ID</param>
        /// <param name="entity3">Third entity instance with different ID</param>
        /// <param name="getId">Function to extract the ID from an entity</param>
        protected void VerifyEntitySemantics<T, TId>(T entity1, T entity2, T entity3, Func<T, TId> getId)
            where T : IEquatable<T>
            where TId : IEquatable<TId>
        {
            // Verify IDs are as expected
            Assert.AreEqual(getId(entity1), getId(entity2), "Test entities should have same ID");
            Assert.AreNotEqual(getId(entity1), getId(entity3), "Test entities should have different IDs");
            
            // Test identity-based equality
            Assert.AreEqual(entity1, entity2, "Entities with same ID should be equal");
            Assert.AreNotEqual(entity1, entity3, "Entities with different IDs should not be equal");
            
            // Test GetHashCode consistency
            Assert.AreEqual(entity1.GetHashCode(), entity2.GetHashCode(), 
                "Entities with same ID should have equal hash codes");
        }
    }
}
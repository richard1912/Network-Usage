using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using NetworkUsage.Contracts;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NetworkUsage.Tests.Integration
{
    /// <summary>
    /// Base class for integration tests that verify cross-component functionality
    /// </summary>
    [TestClass]
    public abstract class IntegrationTestBase : TestBase
    {
        protected Stopwatch Stopwatch { get; private set; } = null!;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            Stopwatch = new Stopwatch();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            Stopwatch?.Stop();
            base.TestCleanup();
        }

        /// <summary>
        /// Override to configure services for integration testing
        /// </summary>
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            
            // Add mock implementations of interfaces for testing
            // Note: These will be replaced with real implementations once they exist
            // services.AddScoped<INetworkMonitor, MockNetworkMonitor>();
            // services.AddScoped<ITaskbarIntegration, MockTaskbarIntegration>();
            // services.AddScoped<IUIComponents, MockUIComponents>();
        }

        /// <summary>
        /// Measures the execution time of an async operation
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <returns>The execution time in milliseconds</returns>
        protected async Task<long> MeasureExecutionTimeAsync(Func<Task> operation)
        {
            Stopwatch.Restart();
            await operation();
            Stopwatch.Stop();
            return Stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Measures the execution time of a synchronous operation
        /// </summary>
        /// <param name="operation">The operation to measure</param>
        /// <returns>The execution time in milliseconds</returns>
        protected long MeasureExecutionTime(Action operation)
        {
            Stopwatch.Restart();
            operation();
            Stopwatch.Stop();
            return Stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Asserts that an operation completes within the specified time limit
        /// </summary>
        /// <param name="operation">The operation to test</param>
        /// <param name="maxTimeMs">Maximum allowed time in milliseconds</param>
        protected async Task AssertCompletesWithinTimeAsync(Func<Task> operation, long maxTimeMs)
        {
            var executionTime = await MeasureExecutionTimeAsync(operation);
            Assert.IsTrue(executionTime <= maxTimeMs, 
                $"Operation took {executionTime}ms but should complete within {maxTimeMs}ms");
        }

        /// <summary>
        /// Asserts that an operation completes within the specified time limit
        /// </summary>
        /// <param name="operation">The operation to test</param>
        /// <param name="maxTimeMs">Maximum allowed time in milliseconds</param>
        protected void AssertCompletesWithinTime(Action operation, long maxTimeMs)
        {
            var executionTime = MeasureExecutionTime(operation);
            Assert.IsTrue(executionTime <= maxTimeMs, 
                $"Operation took {executionTime}ms but should complete within {maxTimeMs}ms");
        }

        /// <summary>
        /// Waits for a condition to become true with timeout
        /// </summary>
        /// <param name="condition">The condition to wait for</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds</param>
        /// <param name="intervalMs">Interval between checks in milliseconds</param>
        protected async Task WaitForConditionAsync(Func<bool> condition, int timeoutMs = 5000, int intervalMs = 100)
        {
            var endTime = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            
            while (DateTime.UtcNow < endTime)
            {
                if (condition())
                    return;
                    
                await Task.Delay(intervalMs);
            }
            
            Assert.Fail($"Condition did not become true within {timeoutMs}ms");
        }
    }
}

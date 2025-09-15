using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace NetworkUsage.Tests
{
    /// <summary>
    /// Base class for all test classes, providing common setup and utilities
    /// </summary>
    [TestClass]
    public abstract class TestBase
    {
        protected IServiceProvider ServiceProvider { get; private set; } = null!;
        protected ILogger Logger { get; private set; } = null!;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            
            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            Logger = loggerFactory.CreateLogger(GetType().Name);
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Override this method to configure additional services for testing
        /// </summary>
        /// <param name="services">Service collection to configure</param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddConsole());
        }

        /// <summary>
        /// Helper method to get a service from the DI container
        /// </summary>
        protected T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Helper method to assert that an exception is thrown
        /// </summary>
        protected async Task AssertThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
                Assert.Fail($"Expected exception of type {typeof(T).Name} but none was thrown");
            }
            catch (T)
            {
                // Expected exception was thrown
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
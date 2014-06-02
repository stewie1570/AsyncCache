using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using FluentAssertions;

namespace Cache.Tests
{
    [TestClass]
    public class CacheTest
    {
        private Cache _cache;

        [TestInitialize]
        public void Setup()
        {
            _cache = new Cache();
        }

        [TestMethod]
        public async Task ShouldReturnResultFromDataSourceTask()
        {
            //Arrange
            //Act
            int result = await _cache.Get("some key", () => Task.FromResult(2));

            //Assert
            result.Should().Be(2);
        }

        [TestMethod]
        public async Task ShouldAwaitDataSourceTaskOnceAndReturnTheFirstDataSourceResult()
        {
            //Arrange
            int callCount = 0;

            //Act
            await _cache.Get("some key", () => { callCount++; return Task.FromResult(2); });
            var result = await _cache.Get("some key", () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(1);
            result.Should().Be(2);
        }

        [TestMethod]
        public async Task ShouldAwaitDataSourceTaskTwiceSinceKeyExpiredAndReturnTheSecondResult()
        {
            //Arrange
            var time = DateTime.Parse("01/01/2000 12:00 am");
            _cache = new Cache(() => time, TimeSpan.FromMinutes(1));
            int callCount = 0;

            //Act
            await _cache.Get("some key", () => { callCount++; return Task.FromResult(2); });
            time = DateTime.Parse("01/01/2000 12:01 am");
            var result = await _cache.Get("some key", () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }
    }
}

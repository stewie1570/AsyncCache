﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Cache.Tests
{
    [TestClass]
    public class AsyncCacheTest
    {
        private AsyncCache _cache;

        [TestInitialize]
        public void Setup()
        {
            _cache = new AsyncCache();
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
            _cache = new AsyncCache(() => time, TimeSpan.FromMinutes(1));
            int callCount = 0;

            //Act
            await _cache.Get("some key", () => { callCount++; return Task.FromResult(2); });
            time = DateTime.Parse("01/01/2000 12:01 am");
            var result = await _cache.Get("some key", () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }

        [TestMethod]
        public async Task ShouldAwaitDataSourceTaskTwiceSinceThereAreTwoKeys()
        {
            //Arrange
            int callCount = 0;

            //Act
            await _cache.Get("key 1", () => { callCount++; return Task.FromResult(2); });
            var result = await _cache.Get("key 2", () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }

        [TestMethod]
        public async Task ShouldLockToPreventRaceCondition()
        {
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();

            int? result1 = null;
            int? result2 = null;
            var get1 = _cache.Get("key1", () => tcs1.Task).ContinueWith(t => result1 = t.Result);
            var get2 = _cache.Get("key1", () => tcs2.Task).ContinueWith(t => result2 = t.Result);

            tcs1.SetResult(1);
            tcs2.SetResult(2);

            await Task.WhenAll(get1, get2);

            result1.Should().Be(1, "because this is the initial value inserted into the cache.");
            result2.Should().Be(1, "because the previous/parallel request should've already inserted 1");
        }
    }
}

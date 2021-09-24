using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Cache.Tests
{
    public class AsyncCacheTest
    {
        private AsyncCache cache;

        public AsyncCacheTest()
        {
            cache = new AsyncCache();
        }

        [Fact]
        public async Task ShouldReturnResultFromDataSourceTask()
        {
            //Arrange
            //Act
            int result = await cache.Get(key: "some key", dataSource: () => Task.FromResult(2));

            //Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task ShouldAwaitDataSourceTaskOnceAndReturnTheFirstDataSourceResult()
        {
            //Arrange
            int callCount = 0;

            //Act
            await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(2); });
            var result = await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(1);
            result.Should().Be(2);
        }

        [Fact]
        public async Task ShouldAwaitDataSourceTaskTwiceSinceCacheKeyWasCleared()
        {
            //Arrange
            int callCount = 0;

            //Act
            await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(2); });
            cache.Clear(key: "some key");
            var result = await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }

        [Fact]
        public async Task ClearShouldOnlyClearSpecifiedKey()
        {
            //Arrange
            int callCount = 0;

            //Act
            await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(2); });
            cache.Clear(key: "some other key");
            var result = await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(1);
            result.Should().Be(2);
        }

        [Fact]
        public async Task ShouldAwaitDataSourceTaskTwiceSinceKeyExpiredAndReturnTheSecondResult()
        {
            //Arrange
            var time = DateTime.Parse("01/01/2000 12:00 am");
            cache = new AsyncCache(() => time, TimeSpan.FromMinutes(1));
            int callCount = 0;

            //Act
            await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(2); });
            time = DateTime.Parse("01/01/2000 12:01 am");
            var result = await cache.Get(key: "some key", dataSource: () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }

        [Fact]
        public async Task ShouldAwaitDataSourceTaskTwiceSinceThereAreTwoKeys()
        {
            //Arrange
            int callCount = 0;

            //Act
            await cache.Get(key: "key 1", dataSource: () => { callCount++; return Task.FromResult(2); });
            var result = await cache.Get(key: "key 2", dataSource: () => { callCount++; return Task.FromResult(3); });

            //Assert
            callCount.Should().Be(2);
            result.Should().Be(3);
        }

        [Fact]
        public async Task ShouldLockToPreventRaceCondition()
        {
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();

            int? result1 = null;
            int? result2 = null;
            var get1 = cache.Get(key: "key1", dataSource: () => tcs1.Task).ContinueWith(t => result1 = t.Result);
            var get2 = cache.Get(key: "key1", dataSource: () => tcs2.Task).ContinueWith(t => result2 = t.Result);

            tcs1.SetResult(1);
            tcs2.SetResult(2);

            await Task.WhenAll(get1, get2);

            result1.Should().Be(1, "because this is the initial value inserted into the cache.");
            result2.Should().Be(1, "because the previous/parallel request should've already inserted 1");
        }
    }
}
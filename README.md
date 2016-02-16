#AsyncCache
[![Build](http://thinkquickbuild.cloudapp.net:8080/app/rest/builds/buildType:AsyncCache_Build/statusIcon)]
(http://thinkquickbuild.cloudapp.net:8080/project.html?projectId=AsyncCache&tab=projectOverview)
[![NuGet version](https://badge.fury.io/nu/AsyncCache.svg)](https://badge.fury.io/nu/AsyncCache)

Just a simple async cache implementation. Here is example usage from a unit test (below). Did I mention how simple it is?

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
        public async Task ShouldLockToPreventRaceCondition()
        {
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();

            int? result1 = null;
            int? result2 = null;
            var get1 = _cache.Get(key: "key1", dataSource: () => tcs1.Task).ContinueWith(t => result1 = t.Result);
            var get2 = _cache.Get(key: "key1", dataSource: () => tcs2.Task).ContinueWith(t => result2 = t.Result);

            tcs1.SetResult(1);
            tcs2.SetResult(2);

            await Task.WhenAll(get1, get2);

            result1.Should().Be(1, "because this is the initial value inserted into the cache.");
            result2.Should().Be(1, "because the previous/parallel request should've already inserted 1");
        }

#AsyncCache
[![NuGet version](https://badge.fury.io/nu/AsyncCache.svg)](https://badge.fury.io/nu/AsyncCache)
[![Build](http://thinkquickbuild.cloudapp.net:8080/app/rest/builds/buildType:AsyncCache_Build/statusIcon)]
(http://thinkquickbuild.cloudapp.net:8080/project.html?projectId=AsyncCache&tab=projectOverview)

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

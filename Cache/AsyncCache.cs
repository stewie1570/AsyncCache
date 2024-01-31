﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Cache
{
    public class AsyncCache
    {
        private ConcurrentDictionary<string, CacheItem> dictionary = new ConcurrentDictionary<string, CacheItem>();
        private ConcurrentDictionary<string, AsyncLock> locks = new ConcurrentDictionary<string, AsyncLock>();
        private Func<DateTime> timeProvider;
        private TimeSpan keyLifeTime;

        public AsyncCache() : this(TimeSpan.FromMinutes(5)) { }
        public AsyncCache(TimeSpan keyLifeTime) : this(() => DateTime.UtcNow, keyLifeTime) { }
        public AsyncCache(Func<DateTime> timeProvider, TimeSpan keyLifeTime)
        {
            this.timeProvider = timeProvider;
            this.keyLifeTime = keyLifeTime;
        }

        public async Task<T> Get<T>(string key, Func<Task<T>> dataSource)
        {
            dictionary.TryGetValue(key, out var cacheItem);
            if (cacheItem != null && timeProvider() < cacheItem.Expiration)
            {
                return (T)cacheItem.Item;
            }

            using (var releaser = await locks.GetOrAdd(key, s => new AsyncLock()).LockAsync())
            {
                var currentTime = timeProvider();

                if (!dictionary.ContainsKey(key) || currentTime >= dictionary[key].Expiration)
                {
                    dictionary[key] = new CacheItem
                    {
                        Item = await dataSource(),
                        Expiration = currentTime + keyLifeTime
                    };
                }
                return (T)dictionary[key].Item;
            }
        }

        public async Task Clear(string key)
        {
            using (var releaser = await locks.GetOrAdd(key, s => new AsyncLock()).LockAsync())
            {
                dictionary.TryRemove(key, out _);
            }
        }

        #region Helper classes

        public class CacheItem
        {
            public object Item { get; set; }
            public DateTime Expiration { get; set; }
        }

        #endregion
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cache
{
    public class AsyncCache
    {
        private Dictionary<string, CacheItem> _dictionary = new Dictionary<string, CacheItem>();
        private ConcurrentDictionary<string, AsyncLock> _locks = new ConcurrentDictionary<string, AsyncLock>();
        private Func<DateTime> _timeProvider;
        private TimeSpan _keyLifeTime;

        public AsyncCache() : this(() => DateTime.UtcNow, TimeSpan.FromMinutes(5)) { }
        public AsyncCache(Func<DateTime> timeProvider, TimeSpan keyLifeTime)
        {
            _timeProvider = timeProvider;
            _keyLifeTime = keyLifeTime;
        }

        public async Task<T> Get<T>(string key, Func<Task<T>> cacheDataSource)
        {
            using (var releaser = await _locks.GetOrAdd(key, s => new AsyncLock()).LockAsync())
            {
                var currentTime = _timeProvider();

                if (!_dictionary.ContainsKey(key) || currentTime >= _dictionary[key].Expiration)
                {
                    _dictionary[key] = new CacheItem
                    {
                        Item = await cacheDataSource(),
                        Expiration = currentTime + _keyLifeTime
                    };
                }
                return (T)_dictionary[key].Item;
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

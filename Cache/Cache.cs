using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cache
{
    public class Cache
    {
        private Dictionary<string, CacheItem> _dictionary = new Dictionary<string, CacheItem>();
        private Func<DateTime> _timeProvider;
        private TimeSpan _keyLifeTime;

        public Cache() : this(() => DateTime.UtcNow, TimeSpan.FromMinutes(5)) { }
        public Cache(Func<DateTime> timeProvider, TimeSpan keyLifeTime)
        {
            _timeProvider = timeProvider;
            _keyLifeTime = keyLifeTime;
        }

        //TODO: This is not thread-safe yet!!!
        public async Task<T> Get<T>(string key, Func<Task<T>> cacheDataSource)
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

        #region Helper classes

        public class CacheItem
        {
            public object Item { get; set; }
            public DateTime Expiration { get; set; }
        }

        #endregion
    }
}

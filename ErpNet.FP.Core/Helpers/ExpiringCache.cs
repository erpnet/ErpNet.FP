using System;
using System.Collections.Generic;

namespace ErpNet.FP.Core.Helpers
{
    public class ExpiringCache<TKey, TValue> where TValue : class
    {
        private readonly IDictionary<TKey, ExpiringCacheItem<TValue>> cacheDictionary = new Dictionary<TKey, ExpiringCacheItem<TValue>>();

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            cacheDictionary[key] = new ExpiringCacheItem<TValue>(value, expiresAfter);
        }

        public TValue? Get(TKey key)
        {
            if (!cacheDictionary.ContainsKey(key)) return default;
            var cached = cacheDictionary[key];
            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                cacheDictionary.Remove(key);
                return default;
            }
            return cached.Value;
        }
    }

    public class ExpiringCacheItem<T>
    {
        public ExpiringCacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
        }
        public T Value { get; }
        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
        internal TimeSpan ExpiresAfter { get; }
    }

}

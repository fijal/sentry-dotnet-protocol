using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    internal static class CollectionsExtensions
    {
        public static TValue GetOrCreate<TValue>(
            this ConcurrentDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
            => dictionary.GetOrAdd(key, _ => new TValue()) as TValue;

        public static ConcurrentQueue<T> EnqueueAll<T>(this ConcurrentQueue<T> target, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                target.Enqueue(value);
            }

            return target;
        }

        public static ConcurrentDictionary<TKey, TValue> SetItems<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> target,
            IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var keyValuePair in items)
            {
                target[keyValuePair.Key] = keyValuePair.Value;
            }
            return target;
        }

        public static ConcurrentBag<TValue> AddRange<TValue>(
            this ConcurrentBag<TValue> target,
            IEnumerable<TValue> items)
        {
            foreach (var item in items)
            {
                target.Add(item);
            }
            return target;
        }

        public static void TryCopyTo<TKey, TValue>(this IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
        {
            if (from == null || to == null)
            {
                return;
            }

            foreach (var kv in from)
            {
                if (!to.ContainsKey(kv.Key))
                {
                    to[kv.Key] = kv.Value;
                }
            }
        }
    }
}

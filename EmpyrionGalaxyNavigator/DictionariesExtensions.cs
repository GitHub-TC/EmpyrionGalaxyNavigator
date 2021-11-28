using System.Collections.Generic;

namespace EmpyrionGalaxyNavigator
{
    public static class DictionariesExtensions
    {
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            if (second == null) return first;
            if (first == null) return second;

            foreach (var item in second)
                if (!first.ContainsKey(item.Key))
                    first.Add(item.Key, item.Value);

            return first;
        }
    }
}

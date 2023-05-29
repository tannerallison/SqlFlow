namespace SqlFlow;

public static class CollectionExtensions
{
    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
        Func<TValue, TValue> updateValueFactory, Func<TValue> addValueFactory)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key] = updateValueFactory(dictionary[key]);
        }
        else
        {
            dictionary.Add(key, addValueFactory());
        }
    }

    public static void AddOrReplace<T>(this ICollection<T> collection, T item, Func<T, bool>? comparer = null)
    {
        comparer ??= x => x?.Equals(item) ?? false;
        var existing = collection.SingleOrDefault(h => comparer(h));
        if (existing != null)
        {
            collection.Remove(existing);
        }

        collection.Add(item);
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        var itemsToRemove = collection.Where(predicate).ToList();
        foreach (var itemToRemove in itemsToRemove)
        {
            collection.Remove(itemToRemove);
        }
    }

    public static bool None<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.Any();
    }
}

namespace SqlFlow;

public static class DictionaryExtensions
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
}

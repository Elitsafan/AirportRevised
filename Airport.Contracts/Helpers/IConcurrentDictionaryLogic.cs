namespace Airport.Contracts.Helpers
{
    public interface IConcurrentDictionaryLogic<TKey, TValue, T> : IConcurrentDictionary<TKey, TValue>
        where TKey : notnull
        where TValue : IEnumerable<T>
    {
    }
}

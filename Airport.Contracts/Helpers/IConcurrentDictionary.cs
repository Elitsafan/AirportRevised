namespace Airport.Contracts.Helpers
{
    public interface IConcurrentDictionary<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        ICollection<TKey> Keys { get; }
        ICollection<TValue> Values { get; }

        bool TryGetValue(TKey key, out TValue? value);
        Task<bool> TryAddAsync(TKey key, TValue value, CancellationToken ct = default);
        Task AddOrUpdateAsync(
            TKey key,
            Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, Task<TValue>> updateValueFactory,
            CancellationToken ct = default);
        Task<bool> TryRemoveAsync(TKey key, CancellationToken ct = default);
        Task ClearAsync(CancellationToken ct = default);
        Dictionary<TKey, TValue> ToDictionary();
        Task AddOrUpdateAsync(TKey key, TValue addValue, Func<TKey, TValue, Task<TValue>> updateValueFactory, CancellationToken ct = default);
        Task<bool> TryUpdateAsync(TKey key, TValue updateValue, TValue comparisonValue, CancellationToken ct = default);
    }
}

using System.Collections.Concurrent;

namespace Airport.Domain.Helpers
{
    public class ConcurrentDictionaryLogic<TKey, TValue, T> : IConcurrentDictionaryLogic<TKey, TValue, T>
        where TKey : notnull
        where TValue : IEnumerable<T>
    {
        #region Fields
        private readonly ConcurrentDictionary<TKey, TValue> _items;
        private readonly AsyncSemaphore _semaphore;
        #endregion

        public ConcurrentDictionaryLogic()
        {
            _items = new();
            _semaphore = new(1);
        }

        #region Properties
        public ICollection<TKey> Keys => _items.Keys;
        public ICollection<TValue> Values => _items.Values;
        #endregion

        public async Task<bool> TryAddAsync(TKey key, TValue value, CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            return _items.TryAdd(key, value);
        }

        public async Task AddOrUpdateAsync(
            TKey key,
            Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, Task<TValue>> updateValueFactory,
            CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            if (_items.TryGetValue(key, out TValue? oldValue))
                _items[key] = await updateValueFactory(key, oldValue);
            else
                _items.TryAdd(key, addValueFactory(key));
        }

        public async Task AddOrUpdateAsync(
            TKey key,
            TValue addValue,
            Func<TKey, TValue, Task<TValue>> updateValueFactory,
            CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            if (_items.TryGetValue(key, out TValue? oldValue))
                _items[key] = await updateValueFactory(key, oldValue);
            else
                _items.TryAdd(key, addValue);
        }

        public async Task<bool> TryUpdateAsync(
            TKey key,
            TValue updateValue,
            TValue comparisonValue,
            CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            return _items.TryUpdate(key, updateValue, comparisonValue);
        }

        public async Task<bool> TryRemoveAsync(TKey key, CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            return _items.TryRemove(key, out TValue? _);
        }

        public IReadOnlyList<T> GetValue(TKey key)
        {
            if (!_items.TryGetValue(key, out var value))
                throw new KeyNotFoundException();

            return value.ToList();
        }

        public async Task ClearAsync(CancellationToken ct = default)
        {
            using var _ = await _semaphore.EnterAsync(ct);

            foreach (var collection in _items.Values)
            {
                if (collection is null)
                    continue;

                if (collection is IDisposable disposableCollection)
                    disposableCollection.Dispose();

                foreach (var item in collection)
                    if (item is IDisposable disposableItem)
                        disposableItem.Dispose();
            }

            _items.Clear();
        }

        public Dictionary<TKey, TValue> ToDictionary() => _items.ToDictionary();

        public void Dispose()
        {
            _semaphore.Dispose();

            foreach (var collection in _items.Values)
            {
                if (collection is null)
                    continue;

                if (collection is IDisposable disposableCollection)
                    disposableCollection.Dispose();

                foreach (var item in collection)
                    if (item is IDisposable disposableItem)
                        disposableItem.Dispose();
            }

            _items.Clear();
        }
    }
}

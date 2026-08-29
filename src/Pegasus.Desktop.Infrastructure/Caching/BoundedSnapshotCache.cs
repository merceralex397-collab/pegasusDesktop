namespace Pegasus.Desktop.Infrastructure.Caching;

public enum SnapshotKind
{
    ReferenceData,
    Thumbnail,
    CompatibilityResponse
}

public sealed class BoundedSnapshotCache
{
    private sealed record CacheEntry(byte[] Value, SnapshotKind Kind, DateTimeOffset ExpiresAt);

    private readonly object _gate = new();
    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly int _maxEntries;
    private readonly TimeProvider _timeProvider;

    public BoundedSnapshotCache(int maxEntries, TimeProvider? timeProvider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);

        _maxEntries = maxEntries;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                RemoveExpired(_timeProvider.GetUtcNow());
                return _entries.Count;
            }
        }
    }

    public void Set(string key, SnapshotKind kind, byte[] value, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            RemoveExpired(now);
            if (!_entries.ContainsKey(key) && _entries.Count >= _maxEntries)
            {
                RemoveOldest();
            }

            _entries[key] = new CacheEntry(value.ToArray(), kind, now.Add(lifetime));
        }
    }

    public bool TryGet(string key, out SnapshotKind kind, out byte[]? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            RemoveExpired(now);
            if (!_entries.TryGetValue(key, out var entry))
            {
                kind = default;
                value = null;
                return false;
            }

            kind = entry.Kind;
            value = entry.Value.ToArray();
            return true;
        }
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            return _entries.Remove(key);
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var key in _entries
            .Where(pair => pair.Value.ExpiresAt <= now)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _entries.Remove(key);
        }
    }

    private void RemoveOldest()
    {
        var oldest = _entries.MinBy(pair => pair.Value.ExpiresAt);
        if (oldest.Key is not null)
        {
            _entries.Remove(oldest.Key);
        }
    }
}

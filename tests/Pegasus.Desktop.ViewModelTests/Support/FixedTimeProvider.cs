namespace Pegasus.Desktop.ViewModelTests.Support;

/// <summary>
/// The shared deterministic clock for desktop-side tests. The initial instant
/// is deliberately fixed so tests do not depend on the workstation clock.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset? utcNow = null)
    {
        _utcNow = utcNow ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
}

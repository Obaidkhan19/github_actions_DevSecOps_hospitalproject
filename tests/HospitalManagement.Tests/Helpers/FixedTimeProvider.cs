namespace HospitalManagement.Tests.Helpers;

/// <summary>A clock that always shows the same moment, so tests do not depend on today's date.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

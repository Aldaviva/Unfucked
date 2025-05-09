using NodaTime;

namespace Unfucked.DateTime;

public readonly struct Milliseconds(long milliseconds) {

    private readonly long milliseconds = milliseconds;

    public static explicit operator Milliseconds(long milliseconds) => new(milliseconds);
    public static implicit operator TimeSpan(Milliseconds s) => TimeSpan.FromMilliseconds(s.milliseconds);
    public static implicit operator Period(Milliseconds s) => Period.FromMilliseconds(s.milliseconds);
    public static implicit operator Duration(Milliseconds s) => Duration.FromMilliseconds(s.milliseconds);

}

public readonly struct Seconds(long seconds) {

    private readonly long seconds = seconds;

    public static explicit operator Seconds(long seconds) => new(seconds);
    public static implicit operator TimeSpan(Seconds s) => TimeSpan.FromSeconds(s.seconds);
    public static implicit operator Period(Seconds s) => Period.FromSeconds(s.seconds);
    public static implicit operator Duration(Seconds s) => Duration.FromSeconds(s.seconds);

}

public readonly struct Minutes(long minutes) {

    private readonly long minutes = minutes;

    public static explicit operator Minutes(long minutes) => new(minutes);
    public static implicit operator TimeSpan(Minutes m) => TimeSpan.FromMinutes(m.minutes);
    public static implicit operator Period(Minutes m) => Period.FromMinutes(m.minutes);
    public static implicit operator Duration(Minutes m) => Duration.FromMinutes(m.minutes);

}
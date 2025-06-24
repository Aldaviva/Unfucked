using NodaTime;

namespace Unfucked.DateTime;

public readonly struct Milliseconds(long milliseconds) {

    private readonly long milliseconds = milliseconds;

    public static Milliseconds Of(long milliseconds) => new(milliseconds);

    public static explicit operator Milliseconds(long milliseconds) => new(milliseconds);
    public static implicit operator TimeSpan(Milliseconds ms) => TimeSpan.FromMilliseconds(ms.milliseconds);
    public static implicit operator Period(Milliseconds ms) => Period.FromMilliseconds(ms.milliseconds);
    public static implicit operator Duration(Milliseconds ms) => Duration.FromMilliseconds(ms.milliseconds);

}

public readonly struct Seconds(long seconds) {

    private readonly long seconds = seconds;

    public static Seconds Of(long seconds) => new(seconds);

    public static explicit operator Seconds(long seconds) => new(seconds);
    public static implicit operator TimeSpan(Seconds s) => TimeSpan.FromSeconds(s.seconds);
    public static implicit operator Period(Seconds s) => Period.FromSeconds(s.seconds);
    public static implicit operator Duration(Seconds s) => Duration.FromSeconds(s.seconds);

}

public readonly struct Minutes(long minutes) {

    private readonly long minutes = minutes;

    public static Minutes Of(long minutes) => new(minutes);

    public static explicit operator Minutes(long minutes) => new(minutes);
    public static implicit operator TimeSpan(Minutes m) => TimeSpan.FromMinutes(m.minutes);
    public static implicit operator Period(Minutes m) => Period.FromMinutes(m.minutes);
    public static implicit operator Duration(Minutes m) => Duration.FromMinutes(m.minutes);

}

public readonly struct Hours(long hours) {

    private readonly long hours = hours;

    public static Hours Of(long hours) => new(hours);

    public static explicit operator Hours(long minutes) => new(minutes);
    public static implicit operator TimeSpan(Hours h) => TimeSpan.FromHours(h.hours);
    public static implicit operator Period(Hours h) => Period.FromHours(h.hours);
    public static implicit operator Duration(Hours h) => Duration.FromHours(h.hours);

}

public readonly struct Days(int days) {

    private readonly int days = days;

    public static Days Of(int days) => new(days);

    public static explicit operator Days(int days) => new(days);
    public static implicit operator TimeSpan(Days d) => TimeSpan.FromDays(d.days);
    public static implicit operator Period(Days d) => Period.FromDays(d.days);
    public static implicit operator Duration(Days d) => Duration.FromDays(d.days);

}
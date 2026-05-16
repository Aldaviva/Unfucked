🧰 Unfucked.DateTime
===

[![NuGet](https://img.shields.io/nuget/v/Unfucked.DateTime?logo=nuget&label=package&color=informational)](https://www.nuget.org/packages/Unfucked.DateTime) [![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/Unfucked/dotnetpackage.yml?branch=master&logo=github&label=build)](https://github.com/Aldaviva/Unfucked/actions/workflows/dotnetpackage.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:Unfucked/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B&label=tests)](https://aldaviva.testspace.com/spaces/285777) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/Unfucked?logo=coveralls&label=test%20coverage)](https://coveralls.io/github/Aldaviva/Unfucked?branch=master)

*Fix egregiously missing functionality in [Noda Time](https://www.nuget.org/packages/NodaTime). Inspired by [Joda-Time](https://www.joda.org/joda-time/).*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Installation](#installation)
- [Usage](#usage)
- [All Unfucked libraries](#all-unfucked-libraries)

<!-- /MarkdownTOC -->

[![Unfuck your house](https://raw.githubusercontent.com/Aldaviva/Unfucked/master/.github/images/frame.jpg)](https://loadingreadyrun.com/videos/view/2484/Crapshots-Ep270-The-Home-Show)

## Installation
```sh
dotnet package add Unfucked.DateTime
```
```cs
using Unfucked;
```

## Usage
- Absolute value of a `Duration`
    ```cs
    ZonedDateTime a, b;
    Duration abs = (a - b).Abs();
    ```
- Get midnight from a `ZonedDateTime` as a `ZonedDateTime`, `Period`, or `Duration`
    ```cs
    ZonedDateTime now = SystemClock.Instance.InTzdbSystemDefaultZone().GetCurrentZonedDateTime();
    ZonedDateTime midnight = now.StartOfDay;
    Duration sinceMidnight = now.LocalDateTime.TimeOfDay.DurationSinceStartOfDay;
    Period sinceMidnight2 = now.LocalDateTime.TimeOfDay.PeriodSinceStartOfDay;
    ```
- Convert time zone offset to hours
    ```cs
    double hours = DateTimeZoneProviders.Tzdb["America/Los_Angeles"].GetUtcOffset(SystemClock.Instance.GetCurrentInstant()).Hours;
    ```
- Compare two `OffsetDateTimes` to see which one happens first
    ```cs
    OffsetDateTime a, b;
    bool isBefore = a.IsBefore(b);
    isBefore = !b.IsAfter(a);
    ```
- More succinct and interchangeable way to define standard `TimeSpan`, `Period`, and `Duration` instances by force casting integral numbers to or constructing `Milliseconds`, `Seconds`, `Minutes`, `Hours`, and `Days`, all of which implicitly convert to `TimeSpan`, `Period`, and `Duration`
    ```cs
    TimeSpan t = (Milliseconds) 3;
    TimeSpan t = (Seconds) 1;
    TimeSpan t = (Minutes) 4;
    Period t   = (Hours) 1;
    Duration t = (Days) 5;
    ```
- Convenience accessors to make Noda Time's confusing API more discoverable and understandable. If you somehow really need an alternative instance of `IClock` to write unit tests, you can do this the original way.
    ```cs
    var clock = DateTimeZoneProviders.Tzdb["America/Los_Angeles"].Clock;

    var now = ZonedDateTime.Now;
    var now = ZonedDateTime.NowUtc;
    var now = OffsetDateTime.Now;
    var now = OffsetDateTime.NowUtc;
    var now = Instant.Now;
    var now = LocalDateTime.Now;
    var now = LocalTime.Now;
    var now = LocalDate.Now;
    ```
- Automatically periodically update the IANA time zone database (tzdb) that is included with Noda Time by fetching it from Noda Time's servers. This allows your program to get time zone updates without having to update a NuGet package, recompile, deploy a new version, or restart your process.
    ```cs
    IDateTimeZoneProvider tzdb = DateTimeZoneProviders.CreateOnlineUpdatingTzdbProvider(TimeSpan.FromDays(1));
    ZonedDateTime now = tzdb["America/Los_Angeles"].Clock.GetCurrentZonedDateTime();
    ```

## All Unfucked libraries
- [Unfucked](https://github.com/Aldaviva/Unfucked/tree/master/Unfucked)
- [Unfucked.Caching](https://github.com/Aldaviva/Unfucked/tree/master/Caching)
- [Unfucked.Compression](https://github.com/Aldaviva/Unfucked/tree/master/Compression)
- **Unfucked.DateTime**
- [Unfucked.DI](https://github.com/Aldaviva/Unfucked/tree/master/DI)
- [Unfucked.HTTP](https://github.com/Aldaviva/Unfucked/tree/master/HTTP)
- [Unfucked.ICS](https://github.com/Aldaviva/Unfucked/tree/master/ICS)
- [Unfucked.OBS](https://github.com/Aldaviva/Unfucked/tree/master/OBS)
- [Unfucked.PGP](https://github.com/Aldaviva/Unfucked/tree/master/PGP)
- [Unfucked.STUN](https://github.com/Aldaviva/Unfucked/tree/master/STUN)
- [Unfucked.Twitch](https://github.com/Aldaviva/Unfucked/tree/master/Twitch)
- [Unfucked.Windows](https://github.com/Aldaviva/Unfucked/tree/master/Windows)
using NodaTime;
using NodaTime.TimeZones;
using System.Collections.ObjectModel;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Unfucked.DateTime;

// https://nodatime.org/3.3.x/userguide/tzdb
/// <summary>
/// An alternative to <see cref="DateTimeZoneProviders.Tzdb"/> that periodically automatically updates Noda Time's built-in tzdb version over the Internet from Noda Time servers, so you'll always have the latest time zone data without having to update a NuGet package, recompile, ship a new binary, and relaunch the process.
/// </summary>
public sealed class OnlineUpdatingTzdbDateTimeZoneProvider: IDateTimeZoneProvider, IDisposable {

    private static readonly Uri LATEST_TZDB_POINTER = new("https://nodatime.org/tzdb/latest.txt");

    private readonly Timer updateTimer;

    private readonly HttpClient httpClient = new(
#if NETCOREAPP2_1_OR_GREATER
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }
#endif
    );

    private IDateTimeZoneProvider innerProvider;
    private Uri?                  latestTzdb;

    /// <summary>Create a new tzdb provider that automatically self-updates over the internet. This instance should be stored in a static variable or DI container for the lifetime of the application.</summary>
    /// <param name="updateInterval">How often to check for tzdb updates.</param>
    /// <param name="fallback">Time zone provider to use if tzdb cannot be updated. Defaults to <see cref="DateTimeZoneProviders.Tzdb"/>.</param>
    public OnlineUpdatingTzdbDateTimeZoneProvider(TimeSpan updateInterval, IDateTimeZoneProvider? fallback = null) {
        innerProvider       =  fallback ?? DateTimeZoneProviders.Tzdb;
        updateTimer         =  new Timer { Interval = updateInterval.TotalMilliseconds, AutoReset = true, Enabled = false };
        updateTimer.Elapsed += UpdateTimeZoneData;
        UpdateTimeZoneData();
    }

    private async void UpdateTimeZoneData(object? sender = null, ElapsedEventArgs? e = null) {
        try {
            Uri latest = new(await httpClient.GetStringAsync(LATEST_TZDB_POINTER).ConfigureAwait(false));

            if (latestTzdb != latest && innerProvider.VersionId.Split([' '], 3).ElementAtOrDefault(1) != Path.GetFileNameWithoutExtension(latest.Segments.Last()).Substring(4)) {
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await
#endif
                using Stream tzdbResponse = await httpClient.GetStreamAsync(latest).ConfigureAwait(false);
                innerProvider = new DateTimeZoneCache(TzdbDateTimeZoneSource.FromStream(tzdbResponse));
            }
            latestTzdb = latest;
        } catch (Exception) {
            // ignore uncaught exceptions, the request will retry later
        } finally {
            try {
                updateTimer.Enabled = true;
            } catch (ObjectDisposedException) {}
        }
    }

    #region Delegated to inner provider

    /// <inheritdoc />
    public DateTimeZone GetSystemDefault() => innerProvider.GetSystemDefault();

    /// <inheritdoc />
    public DateTimeZone? GetZoneOrNull(string id) => innerProvider.GetZoneOrNull(id);

    /// <inheritdoc />
    public string VersionId => innerProvider.VersionId;

    /// <inheritdoc />
    public ReadOnlyCollection<string> Ids => innerProvider.Ids;

    /// <inheritdoc />
    public DateTimeZone this[string id] => innerProvider[id];

    #endregion

    /// <inheritdoc/>
    public void Dispose() {
        updateTimer.Elapsed -= UpdateTimeZoneData;
        updateTimer.Dispose();
        httpClient.Dispose();
    }

}
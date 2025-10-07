using Microsoft.Win32;
using System.Diagnostics.Eventing.Reader;

namespace Unfucked.Windows.Power;

/// <summary>
/// Emit events when Windows enters and leaves standby/sleep. More reliable than <see cref="SystemEvents.PowerModeChanged"/>, which sometimes ignores events.
/// </summary>
public interface IStandbyListener: IDisposable {

    /// <summary>
    /// Triggered right before the computer enters standby mode.
    /// </summary>
    event EventHandler StandingBy;

    /// <summary>
    /// Triggered right after the computer wakes up from standby mode.
    /// </summary>
    event EventHandler Resumed;

}

/// <inheritdoc />
public class EventLogStandbyListener: IStandbyListener {

    private const int StandByEventId = 42;
    private const int ResumeEventId  = 107;

    /// <inheritdoc />
    public event EventHandler? StandingBy;

    /// <inheritdoc />
    public event EventHandler? Resumed;

    private readonly EventLogWatcher logWatcher;

    public EventLogStandbyListener() {
        logWatcher = new EventLogWatcher(new EventLogQuery("System", PathType.LogName,
            $"*[System[Provider/@Name=\"Microsoft-Windows-Kernel-Power\" and (EventID={StandByEventId} or EventID={ResumeEventId})]]"));

        logWatcher.EventRecordWritten += onEventRecord;

        logWatcher.Enabled = true;
    }

    private void onEventRecord(object? sender, EventRecordWrittenEventArgs e) {
        if (e.EventException == null) {
            using EventRecord? record = e.EventRecord;
            switch (record?.Id) {
                case StandByEventId:
                    StandingBy?.Invoke(this, EventArgs.Empty);
                    break;
                case ResumeEventId:
                    Resumed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        } else {
            Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        logWatcher.EventRecordWritten -= onEventRecord;
        logWatcher.Dispose();
        GC.SuppressFinalize(this);
    }

}
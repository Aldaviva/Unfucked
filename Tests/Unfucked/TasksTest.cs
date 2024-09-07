using System.Diagnostics;

namespace Tests.Unfucked;

public class TasksTest: IDisposable {

    private readonly CancellationTokenSource cts = new();
    private readonly CancellationToken       ct;

    public TasksTest() {
        ct = cts.Token;
    }

    [Fact]
    public void LongDelayDoesNotThrow() {
        TimeSpan duration = TimeSpan.FromDays(999999);
        Action   thrower  = () => { Task.Delay(duration, ct); };
        thrower.Should().Throw<ArgumentOutOfRangeException>();
        _ = Tasks.Delay(duration, ct);
    }

    [Fact]
    public async Task LongDelay() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await Tasks.Delay(TimeSpan.FromMilliseconds(100), ct);
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(90);
    }

    [Fact]
    public async Task LongDelayWithTimeProvider() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await Tasks.Delay(TimeSpan.FromMilliseconds(100), TimeProvider.System, ct);
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(90);
    }

    [Fact]
    public async Task AnyWithResult() {
        Task<int>[] tasks = [
            Task.FromException<int>(new ApplicationException()),
            Task.FromCanceled<int>(new CancellationToken(true)),
            Task.FromResult(1),
            Task.FromResult(2)
        ];

        (await Tasks.WhenAny(tasks, task => task.Result == 2, ct)).Should().BeTrue();
        (await Tasks.WhenAny(tasks, task => task.Result == 3, ct)).Should().BeFalse();
    }

    [Fact]
    public async Task AnyDoesNotWaitForAll() {
        TaskCompletionSource<int> tcs = new();
        Task<int>[] tasks = [
            tcs.Task,
            Task.Run(async () => {
                await Task.Yield();
                return 1;
            }, ct)
        ];

        (await Tasks.WhenAny(tasks, task => task.Result == 1, ct)).Should().BeTrue();
    }

    [Fact]
    public async Task FirstOrDefault() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string? actual = await Tasks.FirstOrDefault([
            Task.FromCanceled<string>(new CancellationToken(true)),
            Task.FromException<string>(new ApplicationException()),
            Task.FromResult("a"),
            Task.Run(async () => {
                await Task.Delay(TimeSpan.FromHours(24), ct);
                return "b";
            }, ct)
        ], task => task.Result == "a", ct);

        actual.Should().Be("a");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task FirstOrDefaultNull() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int? actual = await Tasks.FirstOrDefaultValueType([
            Task.FromCanceled<int>(new CancellationToken(true)),
            Task.FromException<int>(new ApplicationException()),
            Task.FromResult(1),
            Task.Run(async () => {
                await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
                return 2;
            }, ct)
        ], task => task.Result == 3, ct);

        actual.Should().BeNull("no tasks passed the predicate");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task WaitForCancellationToken() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        cts.CancelAfter(50);

        await cts.Token.Wait();
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public async Task WaitFastPath() {
        CancellationToken canceled = new(true);
        await canceled.Wait();
    }

    /// <inheritdoc />
    public void Dispose() {
        cts.Cancel();
        cts.Dispose();
    }

}
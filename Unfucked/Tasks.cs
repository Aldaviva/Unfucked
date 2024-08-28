namespace Unfucked;

public static class Tasks {

    // ExceptionAdjustment: M:System.TimeSpan.FromMilliseconds(System.Double) -T:System.OverflowException
    private static readonly TimeSpan MaxShortDelay =
#if NET6_0_OR_GREATER
        TimeSpan.FromMilliseconds(uint.MaxValue - 1);
#else
        TimeSpan.FromMilliseconds(int.MaxValue);
#endif

    /// <summary>
    /// Like <see cref="Task.Delay(TimeSpan,CancellationToken)"/>, except this supports arbitrarily long delays.
    /// </summary>
    /// <param name="duration">how long to wait</param>
    /// <param name="cancellationToken">to stop waiting before <paramref name="duration"/> has elapsed</param>
    // ExceptionAdjustment: M:System.TimeSpan.Subtract(System.TimeSpan) -T:System.OverflowException
    public static Task Delay(TimeSpan duration, CancellationToken cancellationToken = default) {
        Task result = Task.CompletedTask;

        for (TimeSpan remaining = duration; remaining > TimeSpan.Zero; remaining = remaining.Subtract(MaxShortDelay)) {
            TimeSpan shortDelay = remaining > MaxShortDelay ? MaxShortDelay : remaining;
            result = result.ContinueWith(_ => Task.Delay(shortDelay, cancellationToken), cancellationToken,
                TaskContinuationOptions.LongRunning | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current).Unwrap();
        }

        return result;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Like <see cref="Task.Delay(TimeSpan,TimeProvider,CancellationToken)"/>, except this supports arbitrarily long delays.
    /// </summary>
    /// <param name="duration">how long to wait</param>
    /// <param name="cancellationToken">to stop waiting before <paramref name="duration"/> has elapsed</param>
    /// <param name="timeProvider">useful for mocking</param>
    // ExceptionAdjustment: M:System.TimeSpan.Subtract(System.TimeSpan) -T:System.OverflowException
    public static Task Delay(TimeSpan duration, TimeProvider timeProvider, CancellationToken cancellationToken = default) {
        timeProvider ??= TimeProvider.System;
        Task result = Task.CompletedTask;

        for (TimeSpan remaining = duration; remaining > TimeSpan.Zero; remaining = remaining.Subtract(MaxShortDelay)) {
            TimeSpan shortDelay = remaining > MaxShortDelay ? MaxShortDelay : remaining;
            result = result.ContinueWith(_ => Task.Delay(shortDelay, timeProvider, cancellationToken), cancellationToken,
                TaskContinuationOptions.LongRunning | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current).Unwrap();
        }

        return result;
    }
#endif

    public static async Task<bool> Any(IEnumerable<Task> childTasks, Predicate<Task> predicate, CancellationTokenSource? cts = default) {
        TaskCompletionSource<bool> predicatePassed = new();

        Task allChildrenDone = Task.WhenAll(childTasks.Select(childTask => childTask.ContinueWith(c => {
            if (predicate(c)) {
                predicatePassed.TrySetResult(true);
                cts?.Cancel();
            }
        }, TaskContinuationOptions.OnlyOnRanToCompletion)));

        await Task.WhenAny(predicatePassed.Task, allChildrenDone).ConfigureAwait(false);
        return predicatePassed.Task.Status == TaskStatus.RanToCompletion;
    }

    public static Task<bool> Any(Task childTask1, Task childTask2, Predicate<Task> predicate, CancellationTokenSource? cts = default) {
        return Any([childTask1, childTask2], predicate, cts);
    }

    public static Task<bool> Any(Predicate<Task> predicate, CancellationTokenSource? cts = default, params Task[] childTasks) {
        return Any(childTasks, predicate, cts);
    }

    public static async Task<T?> FirstOrDefault<T>(IEnumerable<Task<T>> childTasks, Predicate<Task<T>> predicate, CancellationTokenSource? cts = default) {
        TaskCompletionSource<T> predicatePassed = new();

        Task<T[]> allChildrenDone = Task.WhenAll(childTasks.Select(childTask => childTask.ContinueWith(c => {
            if (predicate(c)) {
                predicatePassed.TrySetResult(c.Result);
                cts?.Cancel();
            }

            return c.Result;
        }, TaskContinuationOptions.OnlyOnRanToCompletion)));

        await Task.WhenAny(predicatePassed.Task, allChildrenDone).ConfigureAwait(false);

        return predicatePassed.Task is { Status: TaskStatus.RanToCompletion, Result: { } result } ? result : default;
    }

    public static Task<T?> FirstOrDefault<T>(Task<T> childTask1, Task<T> childTask2, Predicate<Task<T>> predicate, CancellationTokenSource? cts = default) {
        return FirstOrDefault([childTask1, childTask2], predicate, cts);
    }

    public static Task<T?> FirstOrDefault<T>(Predicate<Task<T>> predicate, CancellationTokenSource? cts = default, params Task<T>[] childTasks) {
        return FirstOrDefault(childTasks, predicate, cts);
    }

    public static Task Wait(this CancellationToken token) {
        if (token.IsCancellationRequested) {
            return Task.CompletedTask;
        } else {
            TaskCompletionSource<bool>    completion   = new();
            CancellationTokenRegistration registration = default;
            registration = token.Register(_ => {
                registration.Dispose();
                completion.SetResult(true);
            }, false);
            return completion.Task;
        }
    }

}
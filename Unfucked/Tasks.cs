namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with the asynchronous Task Parallel Library.
/// </summary>
public static class Tasks {

    // ExceptionAdjustment: M:System.TimeSpan.FromMilliseconds(System.Double) -T:System.OverflowException
    private static readonly TimeSpan MaxShortDelay =
#if NET6_0_OR_GREATER
        TimeSpan.FromMilliseconds(uint.MaxValue - 1);
#else
        TimeSpan.FromMilliseconds(int.MaxValue);
#endif

    /// <summary>
    /// Like <see cref="Task.Delay(TimeSpan,CancellationToken)"/>, but with support for arbitrarily long delays.
    /// </summary>
    /// <param name="duration">How long to wait.</param>
    /// <param name="cancellationToken">To stop waiting before <paramref name="duration"/> has elapsed.</param>
    // ExceptionAdjustment: M:System.TimeSpan.Subtract(System.TimeSpan) -T:System.OverflowException
    [Pure]
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
    /// <param name="duration">How long to wait.</param>
    /// <param name="timeProvider">Useful for mocking.</param>
    /// <param name="cancellationToken">To stop waiting before <paramref name="duration"/> has elapsed.</param>
    // ExceptionAdjustment: M:System.TimeSpan.Subtract(System.TimeSpan) -T:System.OverflowException
    [Pure]
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

    /// <summary>
    /// <para>Resolve a <see cref="Task{T}"/> when any of a sequence of tasks resolve and their result value passes a predicate.</para>
    /// <para>This method returns <c>true</c> iff the predicate passed on any of the inner tasks. To instead get the result value from one of the passing inner tasks, see <seealso cref="FirstOrDefault{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Return type of the inner tasks</typeparam>
    /// <param name="innerTasks">Tasks to wait for.</param>
    /// <param name="predicate">Test for each inner task's result value. It should return <c>true</c> to make this method return <c>true</c>.</param>
    /// <param name="ct">If you want to stop waiting early.</param>
    /// <returns><c>true</c> if any of the <paramref name="innerTasks"/> completed successfully with a result that cause <paramref name="predicate"/> to return <c>true</c>, or <c>false</c> if none of them pass.</returns>
    [Pure]
    public static async Task<bool> WhenAny<T>(IEnumerable<Task<T>> innerTasks, Predicate<T> predicate, CancellationToken ct = default) {
        TaskCompletionSource<bool> predicatePassed = new();
        CancellationTokenSource    cts             = CancellationTokenSource.CreateLinkedTokenSource(ct);

        Task allInnerTasksDone = Task.WhenAll(innerTasks.Select(innerTask => innerTask.ContinueWith(c => {
            if (!cts.IsCancellationRequested && predicate(c.Result)) {
                predicatePassed.TrySetResult(true);
                cts.Cancel();
            }
        }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current)));

        await Task.WhenAny(predicatePassed.Task, allInnerTasksDone).ConfigureAwait(false);

        return predicatePassed.Task.Status == TaskStatus.RanToCompletion;
    }

    // public static Task<bool> Any(Task childTask1, Task childTask2, Predicate<Task> predicate, CancellationToken? ct = default) {
    //     return Any([childTask1, childTask2], predicate, ct);
    // }
    //
    // public static Task<bool> Any(Predicate<Task> predicate, CancellationToken? ct = default, params Task[] childTasks) {
    //     return Any(childTasks, predicate, ct);
    // }

    /// <summary>
    /// <para>Resolve a <see cref="Task{T}"/> when any of a sequence of tasks resolve and their result value passes a predicate.</para>
    /// <para>This method returns the result value of the first inner task to pass the predicate, or <c>null</c> if none of them passed the predicate after all of them finished.</para>
    /// </summary>
    /// <typeparam name="T">The result type of the tasks.</typeparam>
    /// <param name="innerTasks">Tasks to wait for.</param>
    /// <param name="predicate">Test for each inner task's result value. It should return <c>true</c> to make this method return the inner task's result value.</param>
    /// <param name="ct">If you want to stop waiting early.</param>
    /// <returns>The result value of the first <paramref name="innerTasks"/> to finish successfully and have its return value pass <paramref name="predicate"/>, or <c>null</c> if all of the <paramref name="innerTasks"/> finished either unsuccessfully or with result values that failed <paramref name="predicate"/>.</returns>
    [Pure]
    public static async Task<T?> FirstOrDefault<T>(IEnumerable<Task<T>> innerTasks, Predicate<T> predicate, CancellationToken ct = default) where T: class {
        TaskCompletionSource<T> predicatePassed = new();
        CancellationTokenSource cts             = CancellationTokenSource.CreateLinkedTokenSource(ct);

        Task<T[]> allInnerTasksDone = Task.WhenAll(innerTasks.Select(innerTask => innerTask.ContinueWith(c => {
            if (!cts.IsCancellationRequested && predicate(c.Result)) {
                predicatePassed.TrySetResult(c.Result);
                cts.Cancel();
            }

            return c.Result;
        }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current)));

        await Task.WhenAny(predicatePassed.Task, allInnerTasksDone).ConfigureAwait(false);

        return predicatePassed.Task is { Status: TaskStatus.RanToCompletion, Result: var result } ? result : null;
    }

    /// <inheritdoc cref="FirstOrDefault{T}" />
    public static async Task<T?> FirstOrDefaultStruct<T>(IEnumerable<Task<T>> childTasks, Predicate<T> predicate, CancellationToken ct = default) where T: struct {
        TaskCompletionSource<T> predicatePassed = new();
        CancellationTokenSource cts             = CancellationTokenSource.CreateLinkedTokenSource(ct);

        Task<T[]> allChildrenDone = Task.WhenAll(childTasks.Select(childTask => childTask.ContinueWith(c => {
            if (!cts.IsCancellationRequested && predicate(c.Result)) {
                predicatePassed.TrySetResult(c.Result);
                cts.Cancel();
            }

            return c.Result;
        }, cts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current)));

        await Task.WhenAny(predicatePassed.Task, allChildrenDone).ConfigureAwait(false);

        return predicatePassed.Task is { Status: TaskStatus.RanToCompletion, Result: var result } ? result : null;
    }

    /// <summary>
    /// <para>Asynchronously await the cancellation of a <see cref="CancellationToken"/>.</para>
    /// <para>This is better than calling <c>CancellationToken.WaitHandle.WaitOne()</c>, which blocks the current thread and prevents other asynchronous work like events from running, rather than asynchronously awaiting</para>
    /// </summary>
    /// <param name="token">cancellation token to wait to be canceled</param>
    public static Task Wait(this CancellationToken token) {
        if (token.IsCancellationRequested) {
            return Task.CompletedTask;
        } else {
            TaskCompletionSource<bool>    completion   = new();
            CancellationTokenRegistration registration = default;
            registration = token.Register(_ => {
                // ReSharper disable once AccessToModifiedClosure - callback won't run after outer scope finishes, because it waits for this, and this can't run twice because the registration is disposed
                registration.Dispose();
                completion.SetResult(true);
            }, false);
            return completion.Task;
        }
    }

    /// <summary>
    /// When the user presses Ctrl+C, keep this console program running instead of immediately killing it, but also cancel the token from the given source.
    /// </summary>
    /// <param name="cts">A source of a <see cref="CancellationToken"/>.</param>
    /// <returns>The same <see cref="CancellationTokenSource"/> instance as <paramref name="cts"/>, for chaining.</returns>
    public static CancellationTokenSource CancelOnCtrlC(this CancellationTokenSource cts) {
        Console.CancelKeyPress += (_, args) => {
            cts.Cancel();
            args.Cancel = true;
        };
        return cts;
    }

    /// <summary>
    /// Get the result of a task or, if it threw an exception, <c>null</c>, rather than rethrowing the inner exception. This allows fluent null-coalescing fallback chaining instead of a bunch of multi-line, temporary variable declaraing try/catch blocks which aren't expression-valued.
    /// </summary>
    /// <typeparam name="T">Type of result</typeparam>
    /// <param name="task">A task that return a result of type <typeparamref name="T"/> or throws an exception</param>
    /// <returns><paramref name="task"/>'s awaited return value, or <c>null</c> if <paramref name="task"/> threw an exception. This method doesn't throw exceptions (except <see cref="OutOfMemoryException"/>).</returns>
    [Pure]
    public static async Task<T?> ResultOrNullForException<T>(this Task<T> task) where T: class? {
        try {
            return await task.ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

    /// <inheritdoc cref="ResultOrNullForException{T}" />
    [Pure]
    public static async Task<T?> ResultOrNullStructForException<T>(this Task<T> task) where T: struct {
        try {
            return await task.ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

    /// <inheritdoc cref="ResultOrNullForException{T}" />
    [Pure]
    public static async Task<T?> ResultOrNullStructForException<T>(this Task<T?> task) where T: struct {
        try {
            return await task.ConfigureAwait(false);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return null;
        }
    }

}
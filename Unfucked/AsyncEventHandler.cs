namespace Unfucked;

/// <summary>Like <see cref="EventHandler"/>, but allows the delegate to return a <see cref="Task"/> to represent an async event handling operation.</summary>
/// <param name="sender">The object that fired the event.</param>
/// <returns>A <see cref="Task"/> that can be awaited.</returns>
public delegate Task AsyncEventHandler(object? sender);

/// <summary>Like <see cref="EventHandler{T}"/>, but allows the delegate to return a <see cref="Task"/> to represent an async event handling operation.</summary>
/// <typeparam name="TArgs">The type of arguments passed in the event.</typeparam>
/// <param name="sender">The object that fired the event.</param>
/// <param name="args">Arguments passed with the event.</param>
/// <returns>A <see cref="Task"/> that can be awaited.</returns>
public delegate Task AsyncEventHandler<in TArgs>(object? sender, TArgs args);

/// <summary>Like <see cref="EventHandler{T}"/>, but allows the delegate to return a <see cref="Task{TResult}"/> to represent an async event handling operation.</summary>
/// <typeparam name="TArgs">The type of arguments passed in the event.</typeparam>
/// <typeparam name="TResult">The result type that the returned <see cref="Task{TResult}"/> is fulfilled with.</typeparam>
/// <param name="sender">The object that fired the event.</param>
/// <param name="args">Arguments passed with the event.</param>
/// <returns>A <see cref="Task{TResult}"/> that can be awaited.</returns>
public delegate Task<TResult> AsyncEventHandler<in TArgs, TResult>(object? sender, TArgs args);
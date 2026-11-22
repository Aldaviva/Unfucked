using System.Collections.Concurrent;

namespace Unfucked.Dataflow;

/// <summary><see cref="IDataflowBlock"/> that takes arbitrarily many items until its source completes, and then emits all the items together at once in one big list (not as multiple individual items like <see cref="BufferBlock{T}"/>) and then completes itself. Useful if you need to join/aggregate/reduce all emitted items that requires access to all items at once, for example, finding the minimum item.</summary>
/// <typeparam name="T">Type of items consumed by this block</typeparam>
[PublicAPI]
public class CollectBlock<T>: InputOutputBlock<T, IReadOnlyCollection<T>> {

    /// <inheritdoc />
    protected sealed override ITargetBlock<T> Input { get; }

    /// <inheritdoc />
    protected override IReceivableSourceBlock<IReadOnlyCollection<T>> Output { get; }

    private readonly ConcurrentQueue<T> items = [];

    /// <summary>If <c>false</c> (default), this block will not emit a collection at all if it completes without having received any inputs; otherwise, when <c>true</c>, it will emit a collection with 0 elements when completing in this case.</summary>
    public bool CanProduceEmptyCollection { get; init; }

    /// <inheritdoc cref="CollectBlock{T}" />
    public CollectBlock(): this(new ExecutionDataflowBlockOptions()) {}

    /// <inheritdoc cref="CollectBlock{T}" />
    /// <param name="blockOptions">Options for this dataflow block.</param>
    public CollectBlock(ExecutionDataflowBlockOptions blockOptions) {
        Input = new ActionBlock<T>(items.Enqueue, blockOptions);

        BufferBlock<IReadOnlyCollection<T>> outputBuffer = new(blockOptions);
        Output = outputBuffer;

        Input.Completion.ContinueWith(_ => {
            if (!items.IsEmpty || CanProduceEmptyCollection) {
                outputBuffer.Post(items.ToList().AsReadOnly());
            }
            Output.Complete();
        });
    }

}
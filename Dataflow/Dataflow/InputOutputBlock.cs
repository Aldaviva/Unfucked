namespace Unfucked.Dataflow;

/// <summary><para>Abstract base class for <see cref="IPropagatorBlock{TInput,TOutput}"/>. Like <see cref="DataflowBlock.Encapsulate"/> and <see cref="IPropagatorBlock{TInput,TOutput}"/> but with fewer annoyances, less boilerplate, and more reusability.</para>
/// <para>This class assumes two inner blocks, an input (target) and an output (source). Override and hook up their receiving and completion in the constructor. See <see cref="CollectBlock{T}"/> for an example.</para></summary>
/// 
/// <typeparam name="TInput">Type of item received by this block, acting as a target.</typeparam>
/// <typeparam name="TOutput">Type of item being emitted by this block, acting as a source.</typeparam>
/// <seealso href="https://learn.microsoft.com/dotnet/standard/parallel-programming/walkthrough-creating-a-custom-dataflow-block-type#deriving-from-ipropagatorblock-to-define-the-sliding-window-dataflow-block"/>
public abstract class InputOutputBlock<TInput, TOutput>: IPropagatorBlock<TInput, TOutput>, IReceivableSourceBlock<TOutput> {

    /// <summary>Target that receives/consumes/subscribes to/listens for items going into this block.</summary>
    protected abstract ITargetBlock<TInput> Input { get; }

    /// <summary>Source that emits/produces/publishes/sends items out of this block.</summary>
    protected abstract IReceivableSourceBlock<TOutput> Output { get; }

    /// <inheritdoc />
    public Task Completion => Output.Completion;

    /// <inheritdoc />
    public void Complete() => Input.Complete();

    /// <inheritdoc />
    public void Fault(Exception exception) => Input.Fault(exception);

    /// <inheritdoc />
    public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions) => Output.LinkTo(target, linkOptions);

    TOutput? ISourceBlock<TOutput>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed) =>
        Output.ConsumeMessage(messageHeader, target, out messageConsumed);

    void ISourceBlock<TOutput>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) => Output.ReleaseReservation(messageHeader, target);

    bool ISourceBlock<TOutput>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target) => Output.ReserveMessage(messageHeader, target);

    DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput>? source, bool consumeToAccept) =>
        Input.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

    /// <inheritdoc />
    public bool TryReceive(Predicate<TOutput>? filter,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                               [MaybeNullWhen(false)]
#endif
                           out TOutput item) => Output.TryReceive(filter, out item!);

    #pragma warning disable CS8767 // nullability attributes are not available in older targets like .NET Framework

    /// <inheritdoc />
    public bool TryReceiveAll(
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            [NotNullWhen(true)]
#endif
        out IList<TOutput>? items) => Output.TryReceiveAll(out items);

    #pragma warning restore CS8767

}
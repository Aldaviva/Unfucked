using System.Threading.Tasks.Dataflow;
using Unfucked.Dataflow;

namespace Unfucked {

    /// <summary>
    /// Methods that make it easier to work with TPL Dataflow
    /// </summary>
    public static class Dataflows {

        private static readonly IDictionary<int, WeakReference<SourceAwareTargetBlock>> SOURCE_AWARE_TARGET_BLOCKS = new Dictionary<int, WeakReference<SourceAwareTargetBlock>>();

        /// <summary>Like <see cref="ISourceBlock{TOutput}.LinkTo"/>, but with the extra option <see cref="PatientDataflowLinkOptions.PropagatingCompletionWaitsForAllSources"/> to make the <paramref name="target"/> patiently wait for its last <paramref name="source"/> instead of its first to complete before the target itself completes, like <see cref="Task.WhenAll(Task[])"/> instead of <see cref="Task.WhenAny(Task[])"/>.</summary>
        /// <typeparam name="T">Type of items emitted by <paramref name="source"/></typeparam>
        /// <param name="source">Block that emits/publishes/produces/outputs items</param>
        /// <param name="target">Block that listens for/subscribes to/consumes/takes as input items</param>
        /// <param name="linkOptions">Options to control the link between the blocks; a superset of <see cref="DataflowLinkOptions"/> with the added option <see cref="PatientDataflowLinkOptions.PropagatingCompletionWaitsForAllSources"/>.</param>
        /// <returns>The link, which can be disposed to unlink the two blocks.</returns>
        public static IDisposable LinkTo<T>(this ISourceBlock<T> source, ITargetBlock<T> target, PatientDataflowLinkOptions linkOptions) {
            if (linkOptions is { PropagateCompletion: true, PropagatingCompletionWaitsForAllSources: true }) {
                SourceAwareTargetBlock<T> newWrapper = new(target);

                WeakReference<SourceAwareTargetBlock> weakWrapper =
                    SOURCE_AWARE_TARGET_BLOCKS.GetOrAdd(target.GetHashCode(), new WeakReference<SourceAwareTargetBlock>(newWrapper), out bool added);

                SourceAwareTargetBlock<T>? wrapper;
                if (added) {
                    wrapper = newWrapper;
                } else if (weakWrapper.TryGetTarget(out SourceAwareTargetBlock? strong)) {
                    wrapper = (SourceAwareTargetBlock<T>) strong;
                } else {
                    wrapper = newWrapper;
                    weakWrapper.SetTarget(newWrapper);
                }

                wrapper.AddSources();
                return new DisposableLink(wrapper, source.LinkTo(wrapper, (DataflowLinkOptions) linkOptions));
            } else {
                return source.LinkTo(target, (DataflowLinkOptions) linkOptions);
            }
        }

        internal abstract class SourceAwareTargetBlock {

            protected int IncompleteSources;

            internal void AddSources(int sourceCount = 1) => Interlocked.Add(ref IncompleteSources, sourceCount);

        }

        internal sealed class SourceAwareTargetBlock<T>(ITargetBlock<T> wrapped): SourceAwareTargetBlock, ITargetBlock<T> {

            public Task Completion => wrapped.Completion;

            public void Complete() {
                if (Interlocked.Decrement(ref IncompleteSources) == 0) {
                    wrapped.Complete();
                }
            }

            public void Fault(Exception exception) {
                Interlocked.Decrement(ref IncompleteSources);
                wrapped.Fault(exception);
            }

            public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T>? source, bool consumeToAccept) =>
                wrapped.OfferMessage(messageHeader, messageValue, source, consumeToAccept);

        }

        internal sealed class DisposableLink: IDisposable {

            private readonly WeakReference<SourceAwareTargetBlock> weakSourceCountAwareTargetBlock;
            private readonly IDisposable                           wrapped;

            public DisposableLink(SourceAwareTargetBlock sourceAwareTargetBlock, IDisposable wrapped) {
                this.wrapped                    = wrapped;
                weakSourceCountAwareTargetBlock = new WeakReference<SourceAwareTargetBlock>(sourceAwareTargetBlock);
            }

            public void Dispose() {
                if (weakSourceCountAwareTargetBlock.TryGetTarget(out SourceAwareTargetBlock? sourceCountAwareTargetBlock)) {
                    sourceCountAwareTargetBlock.AddSources(-1);
                }
                wrapped.Dispose();
            }

        }

    }

}

#pragma warning disable IDE0130 // Namespace does not match folder structure - keeps file structure tidy
namespace Unfucked.Dataflow {

#pragma warning restore IDE0130

    /// <summary>Superset of <see cref="DataflowLinkOptions"/> to control Dataflow block linking behavior. Adds the new option <see cref="PropagatingCompletionWaitsForAllSources"/>. Can be explicitly cast to <see cref="DataflowLinkOptions"/> when needed.</summary>
    public class PatientDataflowLinkOptions {

        private readonly DataflowLinkOptions inner = new();

        /// <summary><para>When <see cref="PropagateCompletion"/> is <c>true</c>, you can set this property to <c>true</c> to make the link's target block complete when ALL of its source blocks complete, not just when ANY one source block completes (like how <see cref="DataflowLinkOptions"/> works). Useful if a target has multiple sources and is completing too early. Like <see cref="Task.WhenAll(Task[])"/> instead of <see cref="Task.WhenAny(Task[])"/>.</para>
        /// <para>The default behavior is <c>false</c>. Has no effect when <see cref="PropagateCompletion"/> is <c>false</c>. When used, all links from all sources to a target must be established with <see cref="Dataflows.LinkTo"/> with the same values for <see cref="PropagateCompletion"/> and this property.</para></summary>
        public bool PropagatingCompletionWaitsForAllSources { get; set; } = false;

        /// <summary>Cast to subset of options <see cref="DataflowLinkOptions"/> if needed by a consumer that isn't <see cref="Dataflows.LinkTo"/>.</summary>
        /// <param name="src"></param>
        public static explicit operator DataflowLinkOptions(PatientDataflowLinkOptions src) => src.inner;

        #region Inherited

        /// <inheritdoc cref="DataflowLinkOptions.PropagateCompletion" />
        public bool PropagateCompletion {
            get => inner.PropagateCompletion;
            set => inner.PropagateCompletion = value;
        }

        /// <inheritdoc cref="DataflowLinkOptions.MaxMessages" />
        public int MaxMessages {
            get => inner.MaxMessages;
            set => inner.MaxMessages = value;
        }

        /// <inheritdoc cref="DataflowLinkOptions.Append" />
        public bool Append {
            get => inner.Append;
            set => inner.Append = value;
        }

        #endregion

    }

    /// <summary><para>Abstract base class for <see cref="IPropagatorBlock{TInput,TOutput}"/>. Like <see cref="DataflowBlock.Encapsulate"/> and <see cref="IPropagatorBlock{TInput,TOutput}"/> but with fewer annoyances, less boilerplate, and more reusability.</para>
    /// <para>This class assumes two inner blocks, an input (target) and an output (source). Override and hook up their receiving and completion in the constructor. See <see cref="JoinAllBlock{T}"/> for an example.</para></summary>
    /// 
    /// <typeparam name="TInput">Type of item received by this block, acting as a target.</typeparam>
    /// <typeparam name="TOutput">Type of item being emitted by this block, acting as a source.</typeparam>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-custom-dataflow-block-type#deriving-from-ipropagatorblock-to-define-the-sliding-window-dataflow-block"/>
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

    /// <summary><see cref="IDataflowBlock"/> that takes arbitrarily many items until its source completes, and then emits all the items together at once in one big list (not as multiple individual items like <see cref="BufferBlock{T}"/>) and then completes itself. Useful if you need to join/aggregate/reduce all emitted items that requires access to all items at once, for example, finding the minimum item.</summary>
    /// <typeparam name="T">Type of items produced by <see cref="Input"/></typeparam>
    public class JoinAllBlock<T>: InputOutputBlock<T, IReadOnlyCollection<T>> {

        /// <inheritdoc />
        protected sealed override ITargetBlock<T> Input { get; }

        /// <inheritdoc />
        protected override IReceivableSourceBlock<IReadOnlyCollection<T>> Output { get; }

        private readonly IList<T> items = [];

        /// <inheritdoc cref="JoinAllBlock{T}" />
        public JoinAllBlock() {
            Input = new ActionBlock<T>(items.Add);

            BufferBlock<IReadOnlyCollection<T>> outputBuffer = new();
            Output = outputBuffer;

            Input.Completion.ContinueWith(_ => {
                if (items.Count != 0) {
                    outputBuffer.Post(items.AsReadOnly());
                }
                Output.Complete();
            });
        }

    }

}
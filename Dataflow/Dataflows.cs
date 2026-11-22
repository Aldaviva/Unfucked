using System.Runtime.CompilerServices;
using Unfucked.Dataflow;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with TPL Dataflow
/// </summary>
public static class Dataflows {

    // generic type arguments should actually be <ITargetBlock<T>, SourceAwareTargetBlock<T>>, but C# doesn't have generic wildcards like Java
    private static readonly ConditionalWeakTable<IDataflowBlock, SourceAwareTargetBlock>
        SOURCE_AWARE_TARGET_BLOCKS = new();

    /// <summary>Like <see cref="ISourceBlock{TOutput}.LinkTo"/>, but with the extra option <see cref="PatientDataflowLinkOptions.PropagatingCompletionWaitsForAllSources"/> to make the <paramref name="target"/> patiently wait for its last <paramref name="source"/> instead of its first to complete before the target itself completes, like <see cref="Task.WhenAll(Task[])"/> instead of <see cref="Task.WhenAny(Task[])"/>.</summary>
    /// <typeparam name="T">Type of items emitted by <paramref name="source"/></typeparam>
    /// <param name="source">Block that emits/publishes/produces/outputs items</param>
    /// <param name="target">Block that listens for/subscribes to/consumes/takes as input items</param>
    /// <param name="linkOptions">Options to control the link between the blocks; a superset of <see cref="DataflowLinkOptions"/> with the added option <see cref="PatientDataflowLinkOptions.PropagatingCompletionWaitsForAllSources"/>.</param>
    /// <returns>The link, which can be disposed to unlink the two blocks.</returns>
    public static IDisposable LinkTo<T>(this ISourceBlock<T> source, ITargetBlock<T> target, PatientDataflowLinkOptions linkOptions) {
        if (linkOptions is { PropagateCompletion: true, PropagatingCompletionWaitsForAllSources: true }) {
            SourceAwareTargetBlock<T> wrapper = (SourceAwareTargetBlock<T>) SOURCE_AWARE_TARGET_BLOCKS.GetValue(target, static key => new SourceAwareTargetBlock<T>((ITargetBlock<T>) key));

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

    internal sealed class DisposableLink(SourceAwareTargetBlock sourceAwareTargetBlock, IDisposable wrapped): IDisposable {

        private readonly WeakReference<SourceAwareTargetBlock> weakSourceCountAwareTargetBlock = new(sourceAwareTargetBlock);

        public void Dispose() {
            if (weakSourceCountAwareTargetBlock.TryGetTarget(out SourceAwareTargetBlock? sourceCountAwareTargetBlock)) {
                sourceCountAwareTargetBlock.AddSources(-1);
            }
            wrapped.Dispose();
        }

    }

}
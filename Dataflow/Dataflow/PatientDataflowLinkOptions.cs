namespace Unfucked.Dataflow;

/// <summary>Superset of <see cref="DataflowLinkOptions"/> to control Dataflow block linking behavior. Adds the new option <see cref="PropagatingCompletionWaitsForAllSources"/>. Can be explicitly cast to <see cref="DataflowLinkOptions"/> when needed.</summary>
[PublicAPI]
public class PatientDataflowLinkOptions {

    private readonly DataflowLinkOptions inner = new();

    /// <summary><para>When <see cref="PropagateCompletion"/> is <c>true</c>, you can set this property to <c>true</c> to make the link's target block complete when ALL of its source blocks complete, not just when ANY one source block completes (like how <see cref="DataflowLinkOptions"/> works). Useful if a target has multiple sources and is completing too early. Like <see cref="Task.WhenAll(Task[])"/> instead of <see cref="Task.WhenAny(Task[])"/>.</para>
    /// <para>The default behavior is <c>false</c>. Has no effect when <see cref="PropagateCompletion"/> is <c>false</c>. When used, all links from all sources to a target must be established with <see cref="Dataflows.LinkTo"/> with the same values for <see cref="PropagateCompletion"/> and this property.</para></summary>
    public bool PropagatingCompletionWaitsForAllSources { get; set; }

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
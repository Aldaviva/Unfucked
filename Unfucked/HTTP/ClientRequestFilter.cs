namespace Unfucked.HTTP;

public interface ClientRequestFilter {

    ValueTask Filter(ref HttpRequestMessage request, CancellationToken cancellationToken);

}
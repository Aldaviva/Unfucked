namespace Unfucked.HTTP.Filters;

public interface ClientRequestFilter {

    ValueTask Filter(ref HttpRequestMessage request, CancellationToken cancellationToken);

}
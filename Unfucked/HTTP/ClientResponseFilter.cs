namespace Unfucked.HTTP;

public interface ClientResponseFilter {

    ValueTask Filter(ref HttpResponseMessage response, CancellationToken cancellationToken);

}
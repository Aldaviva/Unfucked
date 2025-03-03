namespace Unfucked.HTTP.Filters;

public interface ClientResponseFilter {

    ValueTask Filter(ref HttpResponseMessage response, CancellationToken cancellationToken);

}
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

public interface ClientResponseFilter: Registrable<int> {

    ValueTask Filter(ref HttpResponseMessage response, CancellationToken cancellationToken);

}
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

public interface ClientRequestFilter: Registrable<int> {

    ValueTask Filter(ref HttpRequestMessage request, CancellationToken cancellationToken);

}
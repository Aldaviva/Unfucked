using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

public interface ClientRequestFilter: Registrable<int> {

    Task<HttpRequestMessage> Filter(HttpRequestMessage request, CancellationToken cancellationToken);

}
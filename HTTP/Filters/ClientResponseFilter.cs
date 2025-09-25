using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

public interface ClientResponseFilter: Registrable<int> {

    Task<HttpResponseMessage> Filter(HttpResponseMessage response, FilterContext context, CancellationToken cancellationToken);

}
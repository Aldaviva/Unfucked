using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;

namespace Unfucked.HTTP.Filters;

/// <summary>
/// <para>Hook into the client handler chain of an incoming request before it is parsed and returned to the caller. This can inspect, modify, abort, or replace the response.</para>
/// <para>To use a filter, register it with an <see cref="IHttpClient"/>, <see cref="IWebTarget"/>, or <see cref="IClientConfig"/> using the <c>.Register</c> method, optionally passing a position in the filter chain to control the order of execution if multiple filters are registered.</para>
/// </summary>
public interface ClientResponseFilter: Registrable<int> {

    /// <summary>
    /// Hook into the client handler chain of an incoming response before it is parsed and returned to the caller. This can inspect, modify, abort, or replace the response.
    /// </summary>
    /// <param name="response">The original incoming response.</param>
    /// <param name="context">Contains information about the handler that sent the request and the configuration of the target, including properties.</param>
    /// <param name="cancellationToken">If the request is aborted during filtering.</param>
    /// <returns>The response that should continue to be handled in the chain. Usually this is the same as <paramref name="response"/>, but you can return a different <see cref="HttpResponseMessage"/> instance if you need to replay or redirect the request chain to a different destination.</returns>
    /// <exception cref="ProcessingException">An error occurred during filtering.</exception>
    ValueTask<HttpResponseMessage> Filter(HttpResponseMessage response, FilterContext context, CancellationToken cancellationToken);

}
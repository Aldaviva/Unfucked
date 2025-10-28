using Unfucked.HTTP.Config;
using Unfucked.HTTP.Exceptions;

namespace Unfucked.HTTP.Filters;

/// <summary>
/// <para>Hook into the client handler chain of an outgoing request before it is sent. This can inspect, modify, abort, or replace the request.</para>
/// <para>To use a filter, register it with an <see cref="IUnfuckedHttpClient"/>, <see cref="WebTarget"/>, or <see cref="IClientConfig"/> using the <c>.Register</c> method, optionally passing a position in the filter chain to control the order of execution if multiple filters are registered.</para>
/// </summary>
public interface ClientRequestFilter: Registrable<int> {

    /// <summary>
    /// Hook into the client handler chain of an outgoing request before it is sent. This can inspect, modify, abort, or replace the request.
    /// </summary>
    /// <param name="request">The original outgoing request.</param>
    /// <param name="context">Contains information about the handler that sent this request and the configuration of the target, including properties.</param>
    /// <param name="cancellationToken">If the request is aborted during filtering.</param>
    /// <returns>The request that should continue to be handled in the chain. Usually this is the same as <paramref name="request"/>, but you can return a different <see cref="HttpRequestMessage"/> instance if you need to replay or redirect the request chain to a different destination.</returns>
    /// <exception cref="ProcessingException">An error occurred during filtering.</exception>
    Task<HttpRequestMessage> Filter(HttpRequestMessage request, FilterContext context, CancellationToken cancellationToken);

}
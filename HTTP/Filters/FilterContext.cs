using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

/// <summary>Holder for the HTTP handler and its configuration, which the filter may want to refer to during processing</summary>
public readonly record struct FilterContext(IUnfuckedHttpHandler Handler, IClientConfig? Configuration);
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Filters;

public readonly record struct FilterContext(IUnfuckedHttpHandler Handler, IClientConfig? Configuration);
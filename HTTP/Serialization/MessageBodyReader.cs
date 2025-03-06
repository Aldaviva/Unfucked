using System.Text;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public interface MessageBodyReader {

    public bool CanRead<T>(string? mimeType, string? bodyPrefix);

    public Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, IHttpConfiguration? clientConfig, CancellationToken cancellationToken);

}
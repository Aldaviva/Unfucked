using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public sealed class XmlBodyReader: MessageBodyReader {

    private const string APPLICATION_XML_MEDIA_TYPE =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Xml;
#else
        "application/xml";
#endif

    public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) =>
        contentType?.MediaType == MediaTypeNames.Text.Xml ||
        contentType?.MediaType == APPLICATION_XML_MEDIA_TYPE ||
        (contentType?.MediaType?.EndsWith("+xml") ?? false) ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('<') ||
            bodyPrefix.Contains("<?xml", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!--") ||
            bodyPrefix.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)));

    public Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                           Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        try {
            return Task.FromResult(XmlExtensions.ReadObjectFromXmlAsync<T>(responseBody, responseEncoding));
        } catch (Exception exception) {
            return Task.FromException<T>(exception);
        }
    }

    public sealed class XmlDocumentReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(XmlDocument);

        /// <exception cref="XmlException"></exception>
        public Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                               Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            try {
                return Task.FromResult((T) (object) XmlExtensions.ReadDomFromXmlAsync(responseBody, responseEncoding));
            } catch (Exception exception) {
                return Task.FromException<T>(exception);
            }
        }

    }

    public sealed class XDocumentReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(XDocument);

        public async Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                                     Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await XmlExtensions.ReadLinqFromXmlAsync(responseBody, responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

    }

    public sealed class XPathReader: MessageBodyReader {

        public bool CanRead<T>(MediaTypeHeaderValue? contentType, string? bodyPrefix) => typeof(T) == typeof(XPathNavigator);

        /// <exception cref="XmlException"></exception>
        public Task<T> Read<T>(Stream responseBody, HttpStatusCode status, HttpResponseHeaders headers, HttpContentHeaders bodyHeaders, HttpRequestMessage request, IDisposable response,
                               Encoding? responseEncoding,
                               Configurable? clientConfig,
                               CancellationToken cancellationToken) {
            try {
                return Task.FromResult((T) (object) XmlExtensions.ReadXPathFromXmlAsync(responseBody, responseEncoding));
            } catch (Exception exception) {
                return Task.FromException<T>(exception);
            }
        }

    }

}
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

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) =>
        mimeType == MediaTypeNames.Text.Xml ||
        mimeType == APPLICATION_XML_MEDIA_TYPE ||
        (mimeType?.EndsWith("+xml") ?? false) ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('<') ||
            bodyPrefix.Contains("<?xml", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!--") ||
            bodyPrefix.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)));

    public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        return await response.Content.ReadObjectFromXmlAsync<T>(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public sealed class XmlDocumentReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XmlDocument);

        /// <exception cref="XmlException"></exception>
        public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await response.Content.ReadDomFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        }

    }

    public sealed class XDocumentReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XDocument);

        public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await response.Content.ReadLinqFromXmlAsync(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

    }

    public sealed class XPathReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XPathNavigator);

        /// <exception cref="XmlException"></exception>
        public async Task<T> Read<T>(HttpResponseMessage response, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await response.Content.ReadXPathFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        }

    }

}
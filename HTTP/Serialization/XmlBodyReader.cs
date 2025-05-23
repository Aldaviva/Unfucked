﻿using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Unfucked.HTTP.Config;

namespace Unfucked.HTTP.Serialization;

public class XmlBodyReader: MessageBodyReader {

    private const string ApplicationXmlMediaType =
#if NET6_0_OR_GREATER
        MediaTypeNames.Application.Xml;
#else
        "application/xml";
#endif

    public bool CanRead<T>(string? mimeType, string? bodyPrefix) =>
        mimeType == MediaTypeNames.Text.Xml ||
        mimeType == ApplicationXmlMediaType ||
        (mimeType?.EndsWith("+xml") ?? false) ||
        (bodyPrefix != null && (
            bodyPrefix.StartsWith('<') ||
            bodyPrefix.Contains("<?xml", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!--") ||
            bodyPrefix.Contains("xmlns", StringComparison.OrdinalIgnoreCase) ||
            bodyPrefix.Contains("<!doctype", StringComparison.OrdinalIgnoreCase)));

    public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
        return await responseBody.ReadObjectFromXmlAsync<T>(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public class XmlDocumentReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XmlDocument);

        public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await responseBody.ReadDomFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        }

    }

    public class XDocumentReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XDocument);

        public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await responseBody.ReadLinqFromXmlAsync(responseEncoding, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

    }

    public class XPathReader: MessageBodyReader {

        public bool CanRead<T>(string? mimeType, string? bodyPrefix) => typeof(T) == typeof(XPathNavigator);

        public async Task<T> Read<T>(HttpContent responseBody, Encoding? responseEncoding, Configurable? clientConfig, CancellationToken cancellationToken) {
            return (T) (object) await responseBody.ReadXPathFromXmlAsync(responseEncoding, cancellationToken).ConfigureAwait(false);
        }

    }

}
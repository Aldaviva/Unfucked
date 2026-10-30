using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Extensible Markup Language.
/// </summary>
public static class XmlExtensions {

    private static readonly IDictionary<Type, XmlSerializer> XML_SERIALIZER_CACHE = new Dictionary<Type, XmlSerializer>();

    /// <summary>
    /// Parse an XML LINQ document (not mapping to an object or DOM) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="xmlReaderSettings">Settings for the <see cref="XmlReader"/>.</param>
    /// <param name="xmlLoadOptions">Optional XML parsing configuration.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML LINQ document parsed from the HTTP response.</returns>
    [Pure]
    public static async Task<XDocument> ReadLinqFromXmlAsync(this HttpContent content, Encoding? encoding = null, XmlReaderSettings? xmlReaderSettings = null,
                                                             LoadOptions xmlLoadOptions = LoadOptions.None, CancellationToken cancellationToken = default) {
#if NET6_0_OR_GREATER
        await using Stream contentStream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#elif NETSTANDARD2_1_OR_GREATER
        await using Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#else
        using Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

        return await ReadLinqFromXmlAsync(contentStream, encoding, xmlReaderSettings, xmlLoadOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parse an XML LINQ document (not mapping to an object or DOM) from a stream.
    /// </summary>
    /// <param name="stream">A readable stream.</param>
    /// <param name="encoding">Character encoding of the stream.</param>
    /// <param name="xmlReaderSettings">Settings for the <see cref="XmlReader"/>.</param>
    /// <param name="xmlLoadOptions">Optional XML parsing configuration.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML LINQ document parsed from the stream.</returns>
    [Pure]
    public static async Task<XDocument> ReadLinqFromXmlAsync(Stream stream, Encoding? encoding = null, XmlReaderSettings? xmlReaderSettings = null, LoadOptions xmlLoadOptions = LoadOptions.None,
                                                             CancellationToken cancellationToken = default) {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        xmlReaderSettings       ??= new XmlReaderSettings();
        xmlReaderSettings.Async =   true;
#endif

        using StreamReader streamReader = new(stream, encoding ?? Strings.Utf8, encoding == null);
        using XmlReader    xmlReader    = XmlReader.Create(streamReader, xmlReaderSettings);

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return await XDocument.LoadAsync(xmlReader, xmlLoadOptions, cancellationToken).ConfigureAwait(false);
#else
        return XDocument.Load(xmlReader, xmlLoadOptions);
#endif
    }

    /// <summary>
    /// Parse an XML document and map it to an object (not as DOM or LINQ) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="defaultNamespace">Default XML namespace to use for all the XML elements.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <typeparam name="T">Type of the object to map the XML document to.</typeparam>
    /// <returns>An object that was mapped from the XML document.</returns>
    [Pure]
    public static async Task<T> ReadObjectFromXmlAsync<T>(this HttpContent content, Encoding? encoding = null, string? defaultNamespace = null, CancellationToken cancellationToken = default) {
        Task<Stream> readStreamTask = // stream is disposed automatically by StreamReader
#if NET6_0_OR_GREATER
            content.ReadAsStreamAsync(cancellationToken);
#else
            content.ReadAsStreamAsync();
#endif
        return ReadObjectFromXmlAsync<T>(await readStreamTask.ConfigureAwait(false), encoding, defaultNamespace);
    }

    /// <summary>
    /// Parse an XML document and map it to an object (not as DOM or LINQ) from a stream.
    /// </summary>
    /// <param name="stream">A readable stream.</param>
    /// <param name="encoding">Character encoding of the stream.</param>
    /// <param name="defaultNamespace">Default XML namespace to use for all the XML elements.</param>
    /// <typeparam name="T">Type of the object to map the XML document to.</typeparam>
    /// <returns>An object that was mapped from the XML document.</returns>
    [Pure]
    public static T ReadObjectFromXmlAsync<T>(Stream stream, Encoding? encoding = null, string? defaultNamespace = null) {
        XmlSerializer    xmlSerializer = XML_SERIALIZER_CACHE.GetOrAdd(typeof(T), static (_, ns) => new XmlSerializer(typeof(T), ns), out _, defaultNamespace);
        using TextReader textReader    = new StreamReader(stream, encoding ?? Strings.Utf8, encoding == null);
        return (T) xmlSerializer.Deserialize(textReader)!;
    }

    /// <summary>
    /// Parse an XML DOM document (not mapping it to an object or LINQ) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML DOM document parsed from the HTTP response.</returns>
    /// <exception cref="XmlException"></exception>
    [Pure]
    public static async Task<XmlDocument> ReadDomFromXmlAsync(this HttpContent content, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        Task<Stream> readStreamTask =
#if NET6_0_OR_GREATER
            content.ReadAsStreamAsync(cancellationToken);
#else
            content.ReadAsStreamAsync();
#endif
        return ReadDomFromXmlAsync(await readStreamTask.ConfigureAwait(false), encoding);
    }

    /// <summary>
    /// Parse an XML DOM document (not mapping it to an object or LINQ) from a stream.
    /// </summary>
    /// <param name="stream">A readable stream.</param>
    /// <param name="encoding">Character encoding of the stream.</param>
    /// <returns>An XML DOM document parsed from the stream.</returns>
    /// <exception cref="XmlException"></exception>
    [Pure]
    public static XmlDocument ReadDomFromXmlAsync(Stream stream, Encoding? encoding = null) {
        XmlDocument      doc        = new();
        using TextReader textReader = new StreamReader(stream, encoding ?? Strings.Utf8, encoding == null);
        doc.Load(textReader);
        return doc;
    }

    /// <summary>
    /// Parse an XML document and select XPath expressions on it from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XPath navigator on an XML document parsed from the HTTP response.</returns>
    /// <exception cref="XmlException"></exception>
    [Pure]
    public static async Task<XPathNavigator> ReadXPathFromXmlAsync(this HttpContent content, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        Task<Stream> readStreamTask =
#if NET6_0_OR_GREATER
            content.ReadAsStreamAsync(cancellationToken);
#else
            content.ReadAsStreamAsync();
#endif
        return ReadXPathFromXmlAsync(await readStreamTask.ConfigureAwait(false), encoding);
    }

    /// <summary>
    /// Parse an XML document and select XPath expressions on it from a stream.
    /// </summary>
    /// <param name="stream">A readable stream.</param>
    /// <param name="encoding">Character encoding of the stream.</param>
    /// <returns>An XPath navigator on an XML document parsed from the stream.</returns>
    /// <exception cref="XmlException"></exception>
    [Pure]
    public static XPathNavigator ReadXPathFromXmlAsync(Stream stream, Encoding? encoding = null) {
        using TextReader textReader = new StreamReader(stream, encoding ?? Strings.Utf8, encoding == null);
        return new XPathDocument(textReader).CreateNavigator();
    }

    /// <summary>
    /// Find all descendant elements of an XML element that have any of the specified tag names.
    /// </summary>
    /// <param name="ancestor">XML element within which to find descendant elements</param>
    /// <param name="names">Array of tag names to find</param>
    /// <returns>Sequence of XML elements that are descendants of <paramref name="ancestor"/> and whose name is one of the items of <paramref name="names"/></returns>
    [Pure]
    public static IEnumerable<XElement> Descendants(this XContainer ancestor, params IEnumerable<XName> names) => names.SelectMany(ancestor.Descendants);

}
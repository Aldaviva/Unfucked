using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Extensible Markup Language.
/// </summary>
public static class XML {

    /// <summary>
    /// Parse an XML LINQ document (not mapping to an object or DOM) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="xmlLoadOptions">Optional XML parsing configuration.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML LINQ document parsed from the HTTP response.</returns>
    public static async Task<XDocument> ReadLinqFromXmlAsync(this HttpContent content, LoadOptions xmlLoadOptions = LoadOptions.None, CancellationToken cancellationToken = default) {
#if NET6_0_OR_GREATER
        await using Stream contentStream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#elif NETSTANDARD2_1_OR_GREATER
        await using Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#else
        using Stream contentStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return await XDocument.LoadAsync(contentStream, xmlLoadOptions, cancellationToken).ConfigureAwait(false);
#else
        return XDocument.Load(contentStream, xmlLoadOptions);
#endif
    }

    private static readonly IDictionary<Type, XmlSerializer> XmlSerializerCache = new Dictionary<Type, XmlSerializer>();

    /// <summary>
    /// Parse an XML document and map it to an object (not as DOM or LINQ) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="defaultNamespace">Default XML namespace to use for all the XML elements.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <typeparam name="T">Type of the object to map the XML document to.</typeparam>
    /// <returns>An object that was mapped from the XML document.</returns>
    public static async Task<T> ReadObjectFromXmlAsync<T>(this HttpContent content, Encoding? encoding = null, string? defaultNamespace = null, CancellationToken cancellationToken = default) {
        XmlSerializer xmlSerializer = XmlSerializerCache.GetOrAdd(typeof(T), () => new XmlSerializer(typeof(T), defaultNamespace), out _);
        Task<Stream>  readStreamTask;
#if NET6_0_OR_GREATER
        readStreamTask = content.ReadAsStreamAsync(cancellationToken);
#else
        readStreamTask = content.ReadAsStreamAsync();
#endif
        using TextReader textReader = new StreamReader(await readStreamTask.ConfigureAwait(false), encoding ?? Encoding.UTF8);
        return (T) xmlSerializer.Deserialize(textReader)!;
    }

    /// <summary>
    /// Parse an XML DOM document (not mapping it to an object or LINQ) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="encoding">Character encoding of the HTTP response body.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML DOM document parsed from the HTTP response.</returns>
    public static async Task<XmlDocument> ReadDomFromXmlAsync(this HttpContent content, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        XmlDocument  doc = new();
        Task<Stream> readStreamTask;
#if NET6_0_OR_GREATER
        readStreamTask = content.ReadAsStreamAsync(cancellationToken);
#else
        readStreamTask = content.ReadAsStreamAsync();
#endif
        using TextReader textReader = new StreamReader(await readStreamTask.ConfigureAwait(false), encoding ?? Encoding.UTF8);
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
    public static async Task<XPathNavigator> ReadXPathFromXmlAsync(this HttpContent content, Encoding? encoding = null, CancellationToken cancellationToken = default) {
        Task<Stream> readStreamTask;
#if NET6_0_OR_GREATER
        readStreamTask = content.ReadAsStreamAsync(cancellationToken);
#else
        readStreamTask = content.ReadAsStreamAsync();
#endif
        using TextReader textReader = new StreamReader(await readStreamTask.ConfigureAwait(false), encoding ?? Encoding.UTF8);
        return new XPathDocument(textReader).CreateNavigator();
    }

    /// <summary>
    /// Find all descendant elements of an XML element that have any of the specified tag names.
    /// </summary>
    /// <param name="ancestor">XML element within which to find descendant elements</param>
    /// <param name="names">Array of tag names to find</param>
    /// <returns>Sequence of XML elements that are descendants of <paramref name="ancestor"/> and whose name is one of the items of <paramref name="names"/></returns>
    [Pure]
    public static IEnumerable<XElement> Descendants(this XContainer ancestor, params XName[] names) {
        return names.SelectMany(ancestor.Descendants);
    }

}
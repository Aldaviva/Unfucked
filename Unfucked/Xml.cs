using System.Xml.Linq;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with Extensible Markup Language.
/// </summary>
public static class Xml {

    /// <summary>
    /// Parse an XML DOM document (not mapping to an object) from an HTTP response body.
    /// </summary>
    /// <param name="content">HTTP response body.</param>
    /// <param name="xmlLoadOptions">Optional XML parsing configuration.</param>
    /// <param name="cancellationToken">If you want to cancel the operation before it completes.</param>
    /// <returns>An XML document parsed from the HTTP response.</returns>
    public static async Task<XDocument> ReadFromXmlAsync(this HttpContent content, LoadOptions xmlLoadOptions = LoadOptions.None, CancellationToken cancellationToken = default) {
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
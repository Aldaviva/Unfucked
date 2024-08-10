using System.Xml.Linq;

namespace Unfucked;

public static class Xml {

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

    [Pure]
    public static IEnumerable<XElement> Descendants(this XContainer parent, params XName[] alternateNames) {
        return alternateNames.SelectMany(parent.Descendants);
    }

}
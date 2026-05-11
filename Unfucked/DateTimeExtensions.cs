using System.Xml;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with BCL dates and times
/// </summary>
public static class DateTimeExtensions {

    /// <param name="input">a time span that may be positive, negative, or zero</param>
    extension(TimeSpan input) {

        /// <summary>
        /// Absolute value of a time span
        /// </summary>
        /// <returns>the nonnegative magnitude of <paramref name="input"/></returns>
        [Pure]
        public TimeSpan Abs => input.Duration();

        /// <summary>Convert an ISO 8601 time period string into a <see cref="TimeSpan"/>.</summary>
        /// <param name="iso8601Period">A duration represented in the <c>PnYnMnDTnHnMnS</c> format, such as <c>PT30M</c> for 30 minutes.</param>
        /// <returns>A <see cref="TimeSpan"/> that represents the same period as <paramref name="iso8601Period"/>.</returns>
        /// <seealso href="https://en.wikipedia.org/wiki/ISO_8601#Durations"/>
        /// <exception cref="FormatException"><paramref name="iso8601Period"/> is not a valid ISO 8601 time period</exception>
        [Pure]
        public static TimeSpan ParseIso8601(string iso8601Period) => XmlConvert.ToTimeSpan(iso8601Period);

    }

}
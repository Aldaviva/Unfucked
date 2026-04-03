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

    }

}
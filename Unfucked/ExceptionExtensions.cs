using System.Text;

namespace Unfucked;

/// <summary>
/// Methods that make it easier to work with exceptions.
/// </summary>
public static class ExceptionExtensions {

    /// <param name="exception">Outermost exception. Its message will be included too.</param>
    extension(Exception exception) {

        /// <summary>
        /// Return a textual list of messages of this <see cref="Exception"/> and all of its causes (inner exceptions).
        /// </summary>
        /// <param name="includeClassNames"><c>true</c> to prepend each exception's message with its simple class name, or false to exclude the type and only include the message.</param>
        /// <returns>Semicolon-delimited (or colons, if <paramref name="includeClassNames"/> is <c>false</c>) list of exception <see cref="Exception.Message"/>s for each exception in the cause chain, starting with <paramref name="exception"/>.</returns>
        public string MessageChain(bool includeClassNames = true) {
            StringBuilder messageChain = new();
            foreach (Exception ex in exception.CauseChain.Prepend(exception)) {
                if (messageChain.Length != 0) {
                    messageChain.Append(includeClassNames ? ';' : ':').Append(' ');
                }

                if (includeClassNames) {
                    messageChain.Append(ex.GetType().Name).Append(':').Append(' ');
                }

                messageChain.Append(ex.Message);
            }
            return messageChain.ToString();
        }

    }

    /// <param name="exception">Starting exception. It will not be included in the output, so if you want it in the chain too, call <see cref="Enumerable.Prepend"/>.</param>
    extension(Exception exception) {

        /// <summary>
        /// <para>Get an <see cref="IEnumerable{T}"/> of the causes (inner exceptions) of this <see cref="Exception"/>.</para>
        /// <para>Only includes the first cause of <see cref="AggregateException"/>s.</para>
        /// </summary>
        /// <returns>Sequence of the <see cref="Exception.InnerException"/>s of the exceptions recursively, starting with the cause of <paramref name="exception"/>.</returns>
        public IEnumerable<Exception> CauseChain {
            get {
                while (exception.InnerException is {} cause && !ReferenceEquals(exception, cause)) {
                    exception = cause;
                    yield return cause;
                }
            }
        }

    }

    /// <param name="exception">An <see cref="IOException"/>, possibly thrown by <see cref="FileStream(string, FileMode)"/>.</param>
    extension(IOException exception) {

        /// <summary>
        /// <para>Determine if an <see cref="IOException"/> was caused by a file already existing on Windows.</para>
        /// <para>This can be caused if you try to create a file that already exists, for example, with <see cref="FileStream(string, FileMode)"/> with the <c>mode</c> argument set to <see cref="FileMode.CreateNew"/>.</para>
        /// <para>There is no way to detect this failure case on other operating systems, so this returns <c>false</c> on all OSes except Windows.</para>
        /// </summary>
        /// <returns><c>true</c> if the operating system is Windows and <paramref name="exception"/> was caused by a file already existing; <c>false</c> otherwise.</returns>
        public bool IsCausedByExistingWindowsFile =>
            Environment.OSVersion.Platform == PlatformID.Win32NT && (ushort) exception.HResult == 80;

    }

}
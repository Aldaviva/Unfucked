using System.Text;

namespace Unfucked;

public static class ExceptionExtensions {

    public static string MessageChain(this Exception exception, bool includeClassNames = true) {
        StringBuilder messageChain = new();
        foreach (Exception ex in exception.CauseChain().Prepend(exception)) {
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

    public static IEnumerable<Exception> CauseChain(this Exception exception) {
        while (exception.InnerException is { } cause && !ReferenceEquals(exception, cause)) {
            exception = cause;
            yield return cause;
        }
    }

}
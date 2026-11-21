using System.ComponentModel;
using System.Reflection;

namespace Unfucked.GitHub;

[EditorBrowsable(EditorBrowsableState.Advanced)]
internal static class Extensions {

    private static readonly Type StringExtensions = typeof(Octokit.GitHubClient).Assembly.GetType($"{nameof(Octokit)}.StringExtensions")!;

    private delegate Uri UriFormatter(string pattern, params object[] args);

    private static readonly UriFormatter FORMAT_URI = (UriFormatter) StringExtensions
        .GetMethod("FormatUri", BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(object[])], null)!
        .CreateDelegate(typeof(UriFormatter));

    public static Uri FormatUri(this string pattern, params object[] args) => FORMAT_URI(pattern, args);

    private delegate Uri UriTemplateExpander(string pattern, object values);

    private static readonly UriTemplateExpander EXPAND_URI_TEMPLATE = (UriTemplateExpander) StringExtensions
        .GetMethod("ExpandUriTemplate", BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(object)], null)!
        .CreateDelegate(typeof(UriTemplateExpander));

    public static Uri ExpandUriTemplate(this string template, object values) => EXPAND_URI_TEMPLATE(template, values);

    extension(ApiUrls) {

        internal static Uri ActionsPermissions(string owner, string repo) =>
            "repos/{0}/{1}/actions/permissions".FormatUri(owner, repo);

        internal static Uri ActionsPermissionsSelectedActions(string owner, string repo) =>
            "repos/{0}/{1}/actions/permissions/selected-actions".FormatUri(owner, repo);

    }

}
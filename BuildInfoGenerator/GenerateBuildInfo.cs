using Microsoft.Build.Framework;
using System.Text;
using System.Text.RegularExpressions;
using MsBuildTask = Microsoft.Build.Utilities.Task;

namespace Unfucked.BuildInfoGenerator;

public class GenerateBuildInfo: MsBuildTask {

    private static readonly Encoding UTF8 = new UTF8Encoding(false, true);

#if DEBUG
    static GenerateBuildInfo() {
        // During development, Workspace Resolution scans for this value to kill the hosting MSBuild process when it needs to install a new version of this package into the local package cache, if MSBuild holds a lock on this library file.
        const string envVarName      = "MSBUILD_TASKS";
        const string selfLibraryName = "Unfucked";
        Environment.SetEnvironmentVariable(envVarName, Environment.GetEnvironmentVariable(envVarName) is {} old ? $"{old};{selfLibraryName}" : selfLibraryName);
    }
#endif

    [Required]
    public string ProjectDir { get; set; } = null!;

    [Required]
    public string OutputFile { get; set; } = null!;

    public override bool Execute() {
        string? gitDirectory = null;
        for (string? parentDirectory = ProjectDir; gitDirectory is null && !string.IsNullOrEmpty(parentDirectory); parentDirectory = Path.GetDirectoryName(parentDirectory)) {
            string gitDir = Path.Combine(parentDirectory, ".git");
            gitDirectory = Directory.Exists(gitDir) ? gitDir : null;
        }

        string? headCommit = null;
        try {
            if (gitDirectory is not null && File.ReadAllLines(Path.Combine(gitDirectory, "HEAD")).FirstOrDefault(line => line.StartsWith("ref: "))?.Substring(5) is {} branchName &&
                !Path.IsPathRooted(branchName) && !branchName.Contains("..")) {

                headCommit = File.ReadAllLines(Path.Combine(gitDirectory, branchName))[0].Trim().ToLowerInvariant();
                if (!Regex.IsMatch(headCommit, @"^[\da-f]{40}$", RegexOptions.IgnoreCase)) {
                    headCommit = null;
                }
            }
        } catch (FileNotFoundException) {
            // leave headCommit null and handle below
        }

        string fileContents =
            $"""[assembly: Unfucked.Versions.BuildInfo(buildDate: "{DateTimeOffset.UtcNow:O}", commitHash: {(headCommit is not null ? $"\"{headCommit}\"" : "null")})]""";

        try {
            using FileStream   fileStream   = File.Open(OutputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using StreamWriter streamWriter = new(fileStream, UTF8);
            streamWriter.WriteLine(fileContents);
        } catch (IOException e) when (e.HResult is unchecked((int) 0x80070020)) {
            // file is in use by another concurrent build (like multitargeting), so we can skip it because it will already be up to date by the other build
        }

        return true;
    }

}
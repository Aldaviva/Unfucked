using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Tests.DI;

[Collection(TestCollections.CwdSensitive.Name)]
public class ConfigurationTest {

    [Fact]
    public void AlsoSearchForJsonFilesInExecutableDirectory() {
        string oldWorkingDirectory = Environment.CurrentDirectory;
        try {
            Environment.CurrentDirectory = Path.GetTempPath();
            IConfigurationBuilder configBuilder   = A.Fake<IConfigurationBuilder>();
            string                installationDir = Path.GetDirectoryName(Environment.ProcessPath)!;
            IList<IConfigurationSource> sources = [
                new JsonConfigurationSource { Path = "abc", ReloadOnChange = true, ReloadDelay = 123, Optional = false }
            ];

            A.CallTo(() => configBuilder.Sources).Returns(sources);

            configBuilder.AlsoSearchForJsonFilesInExecutableDirectory();

            sources.Should().HaveCount(2);
            JsonConfigurationSource actual = (JsonConfigurationSource) sources[0];
            actual.Path.Should().Be("abc");
            actual.ReloadOnChange.Should().BeTrue("ReloadOnChange");
            actual.ReloadDelay.Should().Be(123);
            actual.Optional.Should().BeTrue("Optional");
            ((PhysicalFileProvider) actual.FileProvider!).Root.Should().Be(installationDir + Path.DirectorySeparatorChar);
        } finally {
            Environment.CurrentDirectory = oldWorkingDirectory;
        }
    }

}
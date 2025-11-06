using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Tests.DI;

public class ConfigurationTest {

    [Fact]
    public void AlsoSearchForJsonFilesInExecutableDirectory() {
        IConfigurationBuilder configBuilder   = A.Fake<IConfigurationBuilder>();
        string                installationDir = Path.GetDirectoryName(Environment.ProcessPath)!;
        IList<IConfigurationSource> sources = [
            new JsonConfigurationSource { Path = "abc", ReloadOnChange = true, ReloadDelay = 123 }
        ];

        A.CallTo(() => configBuilder.Sources).Returns(sources);

        configBuilder.AlsoSearchForJsonFilesInExecutableDirectory();

        JsonConfigurationSource actual = (JsonConfigurationSource) sources[0];
        actual.Path.Should().Be("abc");
        actual.ReloadOnChange.Should().BeTrue();
        actual.ReloadDelay.Should().Be(123);
        actual.Optional.Should().BeTrue();
        ((PhysicalFileProvider) actual.FileProvider!).Root.Should().Be(installationDir + Path.DirectorySeparatorChar);
    }

}
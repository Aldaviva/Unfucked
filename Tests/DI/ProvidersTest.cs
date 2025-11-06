using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unfucked.DI;

namespace Tests.DI;

public class ProvidersTest {

    [Fact]
    public async Task Providers() {
        HostApplicationBuilder builder = new();
        builder.Services
            .AddInjectableProviders()
            .AddTransient<MyDependency>()
            .AddSingleton<MyDependent>();

        using IHost app = builder.Build();
        await app.StartAsync();

        try {
            MyDependent dependent = app.Services.GetRequiredService<MyDependent>();
            dependent.Dependency.Should().NotBeNull();
            dependent.Dependency.Get().Should().NotBeNull().And.BeOfType<MyDependency>();
            dependent.PresentOptionalDependency.Get().Should().NotBeNull();
            dependent.MissingOptionalDependency.Should().NotBeNull();
            dependent.MissingOptionalDependency.Get().Should().BeNull();
        } finally {
            await app.StopAsync();
        }
    }

    private class MyDependency;

    private class NotRegistered;

    private class MyDependent(Provider<MyDependency> dependency, OptionalProvider<MyDependency> presentOptionalDependency, OptionalProvider<NotRegistered> missingOptionalDependency) {

        public Provider<MyDependency> Dependency { get; } = dependency;
        public OptionalProvider<MyDependency> PresentOptionalDependency { get; } = presentOptionalDependency;
        public OptionalProvider<NotRegistered> MissingOptionalDependency { get; } = missingOptionalDependency;

    }

}
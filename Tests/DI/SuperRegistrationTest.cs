using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unfucked.DI;

namespace Tests.DI;

public class SuperRegistrationTest {

    [Fact]
    public async Task SuperRegistrations() {
        HostApplicationBuilder builder = new();
        builder.Services
            .AddSingleton<MyClass>(SuperRegistration.INTERFACES | SuperRegistration.SUPERCLASSES)
            .AddHostedService<MyBackgroundService>(SuperRegistration.INTERFACES | SuperRegistration.CONCRETE_CLASS);

        using IHost app = builder.Build();
        await app.StartAsync();

        try {
            IServiceProvider services = app.Services;

            MyClass? myClass = services.GetService<MyClass>();
            myClass.Should().NotBeNull();
            services.GetService<MyInterface>().Should().BeSameAs(myClass);
            services.GetService<MySuperClass>().Should().BeSameAs(myClass);

            MyBackgroundService? myBackgroundService = services.GetService<MyBackgroundService>();
            myBackgroundService.Should().NotBeNull();
            services.GetService<IHostedService>().Should().BeSameAs(myBackgroundService);
            services.GetService<MyHostedService>().Should().BeSameAs(myBackgroundService);
        } finally {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task KeyedSuperRegistrations() {
        const string serviceKey = "key";

        HostApplicationBuilder builder = new();
        builder.Services.AddKeyedTransient<MyClass>(serviceKey, SuperRegistration.INTERFACES | SuperRegistration.SUPERCLASSES);

        using IHost app = builder.Build();
        await app.StartAsync();

        try {
            IServiceProvider services = app.Services;

            services.GetKeyedService<MyClass>(serviceKey).Should().NotBeNull();
            services.GetKeyedService<MyInterface>(serviceKey).Should().BeOfType<MyClass>().And.NotBeNull();
            services.GetKeyedService<MySuperClass>(serviceKey).Should().BeOfType<MyClass>().And.NotBeNull();
        } finally {
            await app.StopAsync();
        }
    }

    private interface MyInterface;

    private abstract class MySuperClass: MyInterface;

    private class MyClass: MySuperClass;

    private interface MyHostedService;

    private class MyBackgroundService: BackgroundService, MyHostedService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    }

}
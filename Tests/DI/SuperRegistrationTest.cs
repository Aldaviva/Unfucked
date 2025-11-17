using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unfucked.DI;

namespace Tests.DI;

public class SuperRegistrationTest: IDisposable {

    public void Dispose() {
        app?.Dispose();
    }

    private IHost? app;

    [Fact]
    public async Task SuperRegistrations() {
        HostApplicationBuilder builder = new();
        builder.Services
            .AddSingleton<MyClass>(SuperRegistration.INTERFACES | SuperRegistration.SUPERCLASSES)
            .AddHostedService<MyBackgroundService>(SuperRegistration.INTERFACES | SuperRegistration.CONCRETE_CLASS);

        app = builder.Build();
        await app.StartAsync();

        IServiceProvider services = app.Services;

        MyClass? myClass = services.GetService<MyClass>();
        myClass.Should().NotBeNull();
        services.GetService<MyInterface>().Should().BeSameAs(myClass);
        services.GetService<MySuperClass>().Should().BeSameAs(myClass);

        MyBackgroundService? myBackgroundService = services.GetService<MyBackgroundService>();
        myBackgroundService.Should().NotBeNull();
        services.GetService<IHostedService>().Should().BeSameAs(myBackgroundService);
        services.GetService<MyHostedService>().Should().BeSameAs(myBackgroundService);
        services.GetServices<IHostedService>().Should().HaveCount(1);
        services.GetServices<MyHostedService>().Should().HaveCount(1);
    }

    [Fact]
    public async Task CyclicalResolution() {
        HostApplicationBuilder builder = new();
        builder.Services
            .AddHostedService<MyBackgroundService>(SuperRegistration.INTERFACES)
            .AddHostedService<MyOtherHostedService>();

        app = builder.Build();

        IServiceProvider services = app.Services;

        TaskCompletionSource<MyHostedService> actualHolder = new();
        Thread thread = new(async void () => {
            await app.StartAsync();
            actualHolder.TrySetResult(services.GetRequiredService<MyHostedService>());
        }, 1024) {
            IsBackground = true
        };

        thread.Start();

        MyHostedService actual = await actualHolder.Task.WaitAsync(TimeSpan.FromSeconds(5));

        actual.Should().NotBeNull();
    }

    [Fact]
    public async Task KeyedSuperRegistrations() {
        const string serviceKey = "key";

        HostApplicationBuilder builder = new();
        builder.Services.AddKeyedTransient<MyClass>(serviceKey, SuperRegistration.INTERFACES | SuperRegistration.SUPERCLASSES);

        app = builder.Build();
        await app.StartAsync();

        IServiceProvider services = app.Services;

        services.GetKeyedService<MyClass>(serviceKey).Should().NotBeNull();
        services.GetKeyedService<MyInterface>(serviceKey).Should().BeOfType<MyClass>().And.NotBeNull();
        services.GetKeyedService<MySuperClass>(serviceKey).Should().BeOfType<MyClass>().And.NotBeNull();
    }

    private interface MyInterface;

    private abstract class MySuperClass: MyInterface;

    private class MyClass: MySuperClass;

    private interface MyHostedService: IHostedService, IDisposable;

    private class MyBackgroundService: BackgroundService, MyHostedService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    }

    private class MyOtherHostedService(MyHostedService dependency): IHostedService {

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }

}
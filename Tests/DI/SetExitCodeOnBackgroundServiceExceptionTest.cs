using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unfucked.DI;

namespace Tests.DI;

public class SetExitCodeOnBackgroundServiceExceptionTest {

    [Fact]
    public async Task SetExitCodeOnBackgroundServiceException() {
        const int expectedExitCode = 8;

        HostApplicationBuilder builder = new();
        builder.Services
            .AddHostedService<MyBackgroundService>(SuperRegistration.ConcreteClass)
            .SetExitCodeOnBackgroundServiceException(expectedExitCode);

        int originalExitCode = Environment.ExitCode;
        originalExitCode.Should().NotBe(expectedExitCode);

        using IHost app = builder.Build();
        await app.StartAsync();

        await app.Services.GetRequiredService<MyBackgroundService>().ExecuteTask!.ContinueWith(_ => Task.CompletedTask);

        try {
            await app.StopAsync();
            Environment.ExitCode.Should().Be(expectedExitCode);
        } finally {
            Environment.ExitCode = originalExitCode;
        }
    }

    private class MyBackgroundService: BackgroundService {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Delay(50, stoppingToken).ConfigureAwait(false);
            throw new ApplicationException("test");
        }

    }

}
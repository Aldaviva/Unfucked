using Microsoft.Extensions.Hosting;
using Unfucked.DI;

namespace Tests.DI;

public class SetExitCodeOnBackgroundServiceExceptionTest {

    [Fact]
    public async Task SetExitCodeOnBackgroundServiceException() {
        const int expectedExitCode = 8;

        HostApplicationBuilder builder = new();
        builder.Services
            .AddHostedService<MyBackgroundService>(SuperRegistration.CONCRETE_CLASS)
            .SetExitCodeOnBackgroundServiceException(expectedExitCode);

        int originalExitCode = Environment.ExitCode;
        originalExitCode.Should().NotBe(expectedExitCode);

        using IHost app = builder.Build();
        await app.StartAsync();

        try {
            await app.StopAsync();
            Environment.ExitCode.Should().Be(expectedExitCode);
        } finally {
            Environment.ExitCode = originalExitCode;
        }
    }

    private class MyBackgroundService: BackgroundService {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Yield();
            throw new ApplicationException("test");
        }

    }

}